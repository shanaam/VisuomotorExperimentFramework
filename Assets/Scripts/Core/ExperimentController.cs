using System.Collections;
using UnityEngine;
using UXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    public Material[] SurfaceMaterials;

    public Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
    public Dictionary<string, AudioClip> AudioClips = new Dictionary<string, AudioClip>();
    public Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    public CursorController CursorController { get; private set; }

    // Reference to the UXF Session object
    public Session Session { get; private set; }

    // Stores the trial's start and end times
    private float trialStartTime, trialEndTime;

    // Global score variable
    public int Score = 0;

    // Pseudorandom Float List
    private Dictionary<string, List<object>> pMap = new Dictionary<string, List<object>>();

    // Used for object tracking
    public bool IsTracking = true;
    private Dictionary<string, GameObject> trackedObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, List<Vector3>> trackedObjectPath = new Dictionary<string, List<Vector3>>();
    private List<float> trackingTimestamps = new List<float>();

    // Used to track when a step has been incremented
    public List<float> StepTimer = new List<float>();

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

        foreach (Material m in SurfaceMaterials)
            Materials[m.name] = m;

        BeginNextTrial();
    }

    public void BeginNextTrial()
    {
        if (Session.currentTrialNum == 0)
            Session.FirstTrial.Begin();
        else
            Session.BeginNextTrialSafe();
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
    public GameObject GetPrefab(string key)
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

    void FixedUpdate()
    {
        if (IsTracking)
        {
            foreach (string key in trackedObjectPath.Keys)
            {
                trackedObjectPath[key].Add(trackedObjects[key].transform.localPosition);
            }

            if (trackedObjectPath.Count > 0)
            {
                trackingTimestamps.Add(Time.time);
            }
        }
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
        InitializePseudorandomList(trial, "per_block_targetListToUse");
        
        string per_block_type = trial.settings.GetString("per_block_type");
        if (per_block_type == "instruction")
        {
            string per_block_instruction = trial.settings.GetString("per_block_instruction");

            if (per_block_instruction != null)
            {
                CurrentTask = gameObject.AddComponent<InstructionTask>();
                CurrentTask.Setup();
            }

            return;
        }

        switch (Session.settings.GetString("experiment_mode"))
        {
            case "target2d":
                InitializePseudorandomList(trial, "per_block_waterPresent");
                switch (per_block_type)
                {
                    case "aligned":
                    case "rotated":
                    case "clamped":
                    case "nocursor":
                        CurrentTask = gameObject.AddComponent<ReachToTargetTask>();
                        break;
                    case "localization":
                        CurrentTask = gameObject.AddComponent<LocalizationTask>();
                        break;
                    default:
                        Debug.LogWarning("Task not implemented: " + per_block_type);
                        trial.End();
                        break;
                }
                break;
                
            case "target":
                switch (per_block_type)
                {
                    case "aligned":
                    case "rotated":
                    case "clamped":
                    case "nocursor":
                        CurrentTask = gameObject.AddComponent<ReachToTargetTask>();
                        break;
                    case "localization":
                        CurrentTask = gameObject.AddComponent<LocalizationTask>();
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

                List<int> indices = InitializePseudorandomList(trial, "per_block_list_camera_tilt");
                InitializePseudorandomList(trial, "per_block_list_surface_tilt", indices);
                break;
            case "tool":
                CurrentTask = gameObject.AddComponent<ToolTask>();

                // Triger type option list shuffled
                List<int> index = InitializePseudorandomList(trial, "per_block_list_triggerType");
               
                // puck type option list shuffled
                InitializePseudorandomList(trial, "per_block_list_puck_type", index);

                // tool type option list shuffled
                InitializePseudorandomList(trial, "per_block_list_tool_type", index);

                break;
            //case "target2d":
            //    InitializePseudorandomList(trial, "per_block_waterPresent");
            //    break;
            default:
                Debug.LogWarning("Experiment Type not implemented: " +
                                    Session.settings.GetString("experiment_mode"));
                trial.End();
                return;
        }

        CurrentTask.Setup();
    }

    /// <summary>
    /// Cleans up the current trial objects and sets up for the next trial
    /// </summary>
    public void PrepareNextTrial(Trial trial)
    {
        BeginNextTrial();
    }

    /// <summary>
    /// Grabs the current application time, storing it as a starting point
    /// </summary>
    public void StartTimer()
    {
        trialStartTime = Time.time;
    }

    /// <summary>
    /// Grabs the current application time, storing it as an endpoint
    /// </summary>
    public void EndTimer()
    {
        trialEndTime = Time.time;
    }

    /// <summary>
    /// Returns the time elapsed in the current trial
    /// </summary>
    public float GetElapsedTime()
    {
        return Time.time - trialStartTime;
    }

    /// <summary>
    /// Performs all data collection, cleanup, and calls event to start next trial
    /// </summary>
    public void EndAndPrepare()
    {
        CurrentTask.LogParameters();

        Session.CurrentTrial.result["type"] = Session.CurrentTrial.settings.GetString("per_block_type");
        Session.CurrentTrial.result["hand"] = Session.CurrentTrial.settings.GetString("per_block_hand");

        // Track score if score tracking is enabled in the JSON
        // Defaults to disabled if property does not exist in JSON
        if (Session.settings.GetBool("track_score", false))
        {
            Session.CurrentTrial.result["score"] = Score;
        }

        CursorController.UseVR = false;

        // Tracked Object logging
        foreach (string key in trackedObjects.Keys)
        {
            if (trackedObjectPath[key].Count == 0) continue;

            // Add each vector and its components separated by commas
            var list = trackedObjectPath[key];

            // For each element (Select), remove scientific notation and round to 6 decimal places.
            // Then join all these numbers separated by a comma
            Session.CurrentTrial.result[key + "_x"] =
                string.Join(",", list.Select(i => string.Format($"{i.x:F6}")));
            
            Session.CurrentTrial.result[key + "_y"] =
                string.Join(",", list.Select(i => string.Format($"{i.y:F6}")));    
            
            Session.CurrentTrial.result[key + "_z"] =
                string.Join(",", list.Select(i => string.Format($"{i.z:F6}")));
        }

        // Timestamps for tracked objects
        Session.CurrentTrial.result["tracking_timestamp"] =
            string.Join(",", trackingTimestamps.Select(i => string.Format($"{i:F6}")));

        // Timestamps for when a step is incremented
        Session.CurrentTrial.result["step_timestamp"] =
            string.Join(",", StepTimer.Select(i => string.Format($"{i:F6}")));
        StepTimer.Clear();

        ClearTrackedObjects();

        // Cleanup the current task and destroy it
        BaseTask task = GetComponent<BaseTask>();
        task.Disable();

        // Make the cursor visible again, for the tasks that make it not visible
        Cursor.visible = true;

        // Re-enables tracking for next trial, in case prev trial disables it
        IsTracking = true;

        if (Session.CurrentTrial.number == Session.LastTrial.number)
            Session.End();
        else
            Session.CurrentTrial.End();

        Destroy(task);
    }

    /// <summary>
    /// Instantiates and sets up a tracker with the specified name.
    /// </summary>
    [Obsolete("Use AddTrackedObject Instead.")]
    public GameObject GenerateTracker(string trackerName, Transform parent)
    {
        if (trackerName.Contains(" "))
            Debug.LogError("Tracker has a space in its name. Remove the spaces.");

        GameObject tracker = Instantiate(GetPrefab("TrackerObject"), parent);

        tracker.name = tracker.GetComponent<PositionRotationTracker>().objectName = trackerName;

        return tracker;
    }

    /// <summary>
    /// If an experiment requires a pseudorandom list of floats this method will initialize a list
    /// and save it using the key represented in the JSON. The method does not do anything if the function
    /// is called in a trial that is not at the start at the block.
    /// </summary>
    /// <param name="trial"></param>
    /// <param name="key">string used to access this list. Must be the same as the value in the JSON</param>
    /// <param name="indices">A list of integers that denote the order of which the list is initialized to. If none is specified,
    ///  one will be created in the method.</param>
    /// <returns> The indices list. </returns>
    public List<int> InitializePseudorandomList(Trial trial, string key, List<int> indices = null)
    {
        // Only execute if we are starting a new block
        if (trial.numberInBlock != 1) return indices;

        // If experimenter supplied null in the JSON, return
        string listKey = Session.CurrentBlock.settings.GetString(key, "");
        if (listKey == string.Empty) return indices;

        // Grab target list
        List<object> tempAngleList = Session.settings.GetObjectList(listKey);

        if (!pMap.ContainsKey(key))
        {
            // Initialize new list if its the first time using this key
            pMap[key] = new List<object>();
        }
        else
        {
            // Since a list is polled (See PollPseudorandomList()), list keys
            // can not be reused. Experimenter must use separate keys
            if (pMap[key].Count == Session.CurrentBlock.trials.Count)
            {
                Debug.LogWarning(key + ": Keys can't be reused. Use a separate key for this list.");
            }

            pMap[key].Clear();
        }

        if (trial.block.trials.Count < tempAngleList.Count)
        {
            Debug.LogWarning("Number of trials: " + trial.block.trials.Count + " is less than the list" +
                             " of elements in the target list: " + tempAngleList.Count + ". Indices list will be " +
                             "clamped to " + trial.block.trials.Count);

            indices = GenerateListOrder(trial.block.trials.Count);
        }
        else if (trial.block.trials.Count % tempAngleList.Count != 0)
        {
            Debug.LogError("Trial count: " + trial.block.trials.Count + " is not divisible by the number of" +
                           " elements in the list of floats: " + tempAngleList.Count);
        }

        // If an index list wasn't specified, make one
        if (indices == null)
        {
            indices = GenerateListOrder(tempAngleList.Count);
        }

        // Pseudo-random shuffle
        foreach (int i in indices)
        {
            if (i >= tempAngleList.Count)
            {
                Debug.LogError("Index: " + i + " out of range for list containing " + tempAngleList.Count + " elements." +
                               "Are you reusing the indices list for another list? Check if the list is the correct size.");
            }

            pMap[key].Add(tempAngleList[i]);
        }

        return indices;
    }

    /// <summary>
    /// Takes one float from the pseudorandom map while also removing it from its
    /// associated list.
    /// </summary>
    /// <param name="key"></param>
    public object PollPseudorandomList(string key)
    {
        // If the key is null in the JSON, skip
        if (Session.CurrentBlock.settings.GetString(key, "") == string.Empty)
            return 0.0f;

        if (pMap.ContainsKey(key))
        {
            // Pop value from list
            object val = pMap[key][0];
            pMap[key].RemoveAt(0);

            // Log value that was polled
            Session.CurrentTrial.result[key] = val;

            return val;
        }

        if (key != string.Empty)
        {
            Debug.LogError(key +
                             " wasn't initialized yet. Check spelling or have you called InitializePseudorandomList yet?");
            throw new NullReferenceException();
        }

        return null;
    }


    /// <summary>
    /// Using the number of floats specified, generates a temporary list from 0 .. numFloats.
    /// This list is then shuffled and concatenated to a result list. This result list contains elements
    /// equal to the number of trials in the current block. It is important the number of trials is divisible by the
    /// number of floats specified. If it isn't, the problem can be fixed in the JSON.
    /// </summary>
    /// <returns> A list of integers where the values ranges from [0, numFloats) and the number of elements
    /// equals the number of trials in the current block</returns>
    /// <param name="numElements"> The number of elements in the list. Must be a multiple of the number of trials.</param>
    private List<int> GenerateListOrder(int numElements)
    {
        List<int> indices = new List<int>();

        int count = Session.CurrentBlock.trials.Count / numElements;
        for (int i = 0; i < count; i++)
        {
            // Create a temporary list ranging from [0, numFloats) and shuffles it
            List<int> temp = new List<int>(Enumerable.Range(0, numElements));
            temp.Shuffle();

            // Concats the list to the result
            indices.AddRange(temp);
        }

        return indices;
    }

    /// <summary>
    /// Adds an object such that its local position is tracked every FixedUpdate
    /// </summary>
    /// <param name="key">The key representing the location the column in the CSV</param>
    /// <param name="obj">The object to be tracked</param>
    public void AddTrackedObject(string key, GameObject obj)
    {
        if (trackedObjects.ContainsKey(key))
        {
            Debug.LogWarning("You are trying to add a tracker that has already been added");
        }
        else
        {
            trackedObjects[key] = obj;
            trackedObjectPath[key] = new List<Vector3>();
        }
    }

    /// <summary>
    /// Clears all of the tracked objects
    /// </summary>
    private void ClearTrackedObjects()
    {
        string[] keys = new string[trackedObjects.Keys.Count];
        trackedObjects.Keys.CopyTo(keys, 0);

        foreach (string key in keys)
        {
            trackedObjects.Remove(key);
            trackedObjectPath.Remove(key);
        }

        trackingTimestamps.Clear();
    }

    /// <summary>
    /// Logs the position as 3 separate X,Y,Z values in the current trial.
    /// </summary>
    /// <param name="key">Prefix string that will show up in the CSV.</param>
    public void LogObjectPosition(string key, Vector3 position)
    {
        Session.CurrentTrial.result[key + "_x"] = position.x;
        Session.CurrentTrial.result[key + "_y"] = position.y;
        Session.CurrentTrial.result[key + "_z"] = position.z;
    }
}
