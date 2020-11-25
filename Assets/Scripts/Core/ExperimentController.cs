using System.Collections;
using UnityEngine;
using UXF;
using System;
using System.Collections.Generic;
using MovementType = CursorController.MovementType;

/// <summary>
/// Overview of how the application works:
/// - We read in data from the JSON using ExperimentGenerator.cs
/// - The JSON only contains ONE Experiment type
/// 
/// - For each trial, we initialize a BaseTask component, representing the logic
/// for that specific task (aligned, rotated, etc)
/// 
/// - That BaseTask component will run EndAndPrepare when all steps are completed.
/// 
/// - ExperimentController will contain any variables required by the TASKs
/// such as input, session variables, etc
/// </summary>

public class ExperimentController : MonoBehaviour
{
    private static ExperimentController instance;

    public GameObject TargetContainer; // Used as the center point for spawning targets.

    public BaseTask CurrentTask;

    public GameObject[] PrefabList;

    public AudioClip[] SoundEffects;

    public Dictionary<String, GameObject> Prefabs = new Dictionary<string, GameObject>();
    public Dictionary<String, AudioClip> AudioClips = new Dictionary<string, AudioClip>();
    
    public CursorController CursorController { get; private set; }

    public Session Session { get; private set; }

    private float currentTrialTime;

    /// <summary>
    /// Gets the singleton instance of our experiment controller. Use it for
    /// Getting the state of the experiment (input, current trial, etc)
    /// </summary>
    /// <returns></returns>
    public static ExperimentController Instance()
    {
        if (instance == null)
            Debug.LogWarning("Attempted to get ExperimentController that is unitialized.");

        return instance;
    }

    /// <summary>
    /// Initializes all instances of the trials to be run in this experiment
    /// </summary>
    /// <param name="session"></param>
    public void Init(Session session) 
    { 
        Session = session;
        CursorController = GameObject.Find("Cursor").GetComponent<CursorController>();

        foreach (GameObject g in PrefabList)
            Prefabs[g.name] = g;

        foreach (AudioClip c in SoundEffects)
            AudioClips[c.name] = c;

        BeginNextTrial();
    }

    public void BeginNextTrial()
    {
        StartCoroutine(StartTrial());
    }

    /// <summary>
    /// Waits one frame before triggering the next trial event
    /// </summary>
    private IEnumerator StartTrial()
    {
        yield return null;

        if (Session.currentTrialNum == 0)
            Session.FirstTrial.Begin();
        else
            Session.BeginNextTrial();
    }

    /// <summary>
    /// Temporary disables the cursor and re-enables it after 1 second
    /// </summary>
    /// <returns></returns>
    private IEnumerator TempDisableCursor()
    {
        CursorController.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(1f);

        CursorController.gameObject.SetActive(true);
    }

    /// <summary>
    /// We store a list of prefabs a task can spawn. This returns an associated
    /// prefab using the name as it's key
    /// </summary>
    public GameObject GetPrefab(String key)
    {
        if (Prefabs[key] != null) return Prefabs[key];

        Debug.LogWarning(key + " does not exist. Check spelling");
        return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Re-centered Experiment to: " + CursorController.transform.position);
            transform.position = CursorController.RightHand.transform.position;
            StartCoroutine(TempDisableCursor());
        }

        if (Input.GetKeyDown(KeyCode.M))
            EndAndPrepare();

        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }

    /// <summary>
    /// Called at the beginning of each trial. Called by UXF
    /// </summary>
    public void BeginTrialSteps(Trial trial)
    {
        // If the current trial is the first one in the block, we need to generate a pseudo-random
        // list of angles for the trial to pick from.
        List<float> angles = new List<float>();
        if (trial.numberInBlock == 1)
        {
            // Grab target list and shuffle
            List<float> tempAngleList = Session.settings.GetFloatList(
                Session.CurrentBlock.settings.GetString("per_block_targetListToUse"));

            for (int i = 0; i < Session.CurrentBlock.trials.Count; i++)
            {
                angles.Add(tempAngleList[i % tempAngleList.Count]);
            }

            // Pseudo-random shuffle
            angles.Shuffle();
        }

        switch (Session.settings.GetString("experiment_mode")) 
        {
            case "target":
                String per_block_type = trial.settings.GetString("per_block_type");

                switch (per_block_type)
                {
                    case "aligned":
                    case "rotated":
                    case "clamped":
                    case "nocursor":
                        Enum.TryParse(per_block_type, out MovementType reachType);
                        CurrentTask = gameObject.AddComponent<ReachToTargetTask>();

                        ((ReachToTargetTask) CurrentTask).Init(trial,
                            per_block_type == "nocursor" ? MovementType.aligned : reachType,
                            angles);
                        break;
                    case "localization":
                        CurrentTask = gameObject.AddComponent<LocalizationTask>();
                        ((LocalizationTask) CurrentTask).Init(trial, angles);
                        break;
                    default:
                        Debug.LogWarning("Task not implemented: " + per_block_type);
                        trial.End();
                        break;
                }

                break;
            case "pinball_vr":
            case "pinball":
                CurrentTask = gameObject.AddComponent<PinballTask>();
                ((PinballTask)CurrentTask).Init(trial, angles);

                break;
            case "tool":
                CurrentTask = gameObject.AddComponent<ToolTask>();
                ((ToolTask)CurrentTask).Init(trial, angles);
                break;
            default:
                Debug.LogWarning("Experiment Type not implemented: " + 
                                 Session.settings.GetString("experiment_mode"));
                trial.End();
                break;
        }
    }

    /// <summary>
    /// Cleans up the current trial objects and sets up for the next trial
    /// </summary>
    public void PrepareNextTrial(Trial trial)
    {
        BeginNextTrial();
    }

    /// <summary>
    /// Starts time tracking. Called in the task class
    /// </summary>
    public void StartTimer()
    {
        currentTrialTime = Time.fixedTime;
    }

    public void EndTimer()
    {
        Session.CurrentTrial.result["step_time"] = Time.fixedTime - currentTrialTime;
    }

    public void EndAndPrepare()
    {
        LogParameters();

        if (Session.CurrentTrial.number == Session.LastTrial.number)
            Session.End();
        else
            Session.CurrentTrial.End();

        // Cleanup the current task and destroy it
        BaseTask task = GetComponent<BaseTask>();
        task.enabled = false;
        Destroy(task);
    }

    /// <summary>
    /// Saves all of the data points for a particular trial
    /// </summary>
    private void LogParameters()
    {
        // Localization task uses Target as the cursor location
        // For all other tasks, the Target is the actual target
        if (!(CurrentTask is LocalizationTask) && !(CurrentTask is PinballTask))
        {
            Session.CurrentTrial.result["home_x"] = CurrentTask.Home.transform.localPosition.x;
            Session.CurrentTrial.result["home_y"] = CurrentTask.Home.transform.localPosition.y;
            Session.CurrentTrial.result["home_z"] = CurrentTask.Home.transform.localPosition.z;

            Session.CurrentTrial.result["target_x"] = CurrentTask.Target.transform.localPosition.x;
            Session.CurrentTrial.result["target_y"] = CurrentTask.Target.transform.localPosition.y;
            Session.CurrentTrial.result["target_z"] = CurrentTask.Target.transform.localPosition.z;
        }

        EndTimer();
    }

    /// <summary>
    /// Instantiates and sets up a tracker with the specified name.
    /// </summary>
    public GameObject GenerateTracker(String trackerName, Transform parent)
    {
        if (trackerName.Contains(" "))
            Debug.LogError("Tracker has a space in its name. Remove the spaces.");

        GameObject tracker = Instantiate(GetPrefab("TrackerObject"), parent);

        tracker.name = tracker.GetComponent<PositionRotationTracker>().objectName = trackerName;

        return tracker;
    }
}
