﻿using System.Collections;
using UnityEngine;
using UXF;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine.InputSystem;
using MovementType = CursorController.MovementType;
using Random = System.Random;

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

    public Session Session { get; private set; }

    private float currentTrialTime;

    public int Score = 0;

    // Pseudorandom Float List
    private Dictionary<string, List<float>> pMap = new Dictionary<string, List<float>>();

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

        //StartCoroutine(StartTrial());
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

                List<int> indices = GenerateListOrder();
                InitializePseudorandomList(trial, "per_block_list_camera_tilt", indices);
                InitializePseudorandomList(trial, "per_block_list_surface_tilt", indices);
                break;
            case "tool":
                CurrentTask = gameObject.AddComponent<ToolTask>();
                break;
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
        //LogParameters();
        EndTimer();
        CurrentTask.LogParameters();

        // Cleanup the current task and destroy it
        BaseTask task = GetComponent<BaseTask>();
        task.Disable();

        if (Session.CurrentTrial.number == Session.LastTrial.number)
            Session.End();
        else
            Session.CurrentTrial.End();

        Destroy(task);
    }

    /// <summary>
    /// Instantiates and sets up a tracker with the specified name.
    /// </summary>
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
    /// <param name="indices">A list of integers that denote the order of which the list is initialized to</param>
    public void InitializePseudorandomList(Trial trial, string key, List<int> indices = null)
    {
        // Only execute if we are starting a new block
        if (trial.numberInBlock != 1) return;

        // If experimenter supplied null in the JSON, return
        string listKey = Session.CurrentBlock.settings.GetString(key, "");
        if (listKey == string.Empty) return;

        // Grab target list
        List<float> tempAngleList = Session.settings.GetFloatList(listKey);

        if (!pMap.ContainsKey(key))
        {
            // Initialize new list if its the first time using this key
            pMap[key] = new List<float>();
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

        // If an index list wasn't specified, make one
        if (indices == null)
        {
            indices = GenerateListOrder();
        }

        // Pseudo-random shuffle
        foreach (int i in indices)
        {
            pMap[key].Add(tempAngleList[i % tempAngleList.Count]);
        }
    }

    /// <summary>
    /// Takes one float from the pseudorandom map while also removing it from its
    /// associated list.
    /// </summary>
    /// <param name="key"></param>
    public float PollPseudorandomList(string key)
    {
        // If the key is null in the JSON, skip
        if (Session.CurrentBlock.settings.GetString(key, "") == string.Empty)
            return 0.0f;

        if (pMap.ContainsKey(key))
        {
            float val = pMap[key][0];
            pMap[key].RemoveAt(0);

            return val;
        }

        if (key != string.Empty)
        {
            Debug.LogError(key +
                             " wasn't initialized yet. Check spelling or have you called InitializePseudorandomList yet?");
            throw new NullReferenceException();
        }

        return 0.0f;
    }

    /// <summary>
    /// Generates a shuffled list of indices used to denote the order of a list used over a block.
    /// The number of indices equals the total number of trials in the current block.
    /// </summary>
    /// <returns></returns>
    private List<int> GenerateListOrder()
    {
        List<int> indices = new List<int>();

        for (int i = 0; i < Session.CurrentBlock.trials.Count; i++)
        {
            indices.Add(i);
        }

        indices.Shuffle();
        return indices;
    }
}
