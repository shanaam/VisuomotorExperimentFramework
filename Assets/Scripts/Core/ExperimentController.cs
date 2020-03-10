using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using System;

public class ExperimentController : MonoBehaviour
{
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

    private static ExperimentController instance;

    public Session Session { get; private set; }

    /// <summary>
    /// Gets the singleton instance of our experiment controller. Use it for
    /// Getting the state of the experiment (input, current trial, etc)
    /// </summary>
    /// <returns></returns>
    public static ExperimentController Instance()
    {
        if (instance == null)
        {
            Debug.LogWarning("Attempted to get ExperimentController that is unitialized.");
        }

        return instance;
    }

    /// <summary>
    /// Initializes all instances of the trials to be run in this experiment
    /// </summary>
    /// <param name="session"></param>
    public void Init(Session session) 
    { 
        Session = session;
        Session.FirstTrial.Begin();
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BeginTrial()
    {
        Session.BeginNextTrial();
    }

    public void BeginTrialSteps(Trial trial)
    {
        switch (Session.settings.GetString("experiment_mode")) 
        {
            case "target":
                String per_block_type = trial.settings.GetString("per_block_type");
                Debug.Log("Beginning: " + trial.number);
                Debug.Log("Trial Type: " + per_block_type);

                switch (per_block_type)
                {
                    case "aligned":
                    case "rotated":
                    case "nocursor":
                        Enum.TryParse(per_block_type, out ReachExperiment.ReachType reachType);
                        gameObject.AddComponent<ReachToTargetTask>();
                        gameObject.GetComponent<ReachToTargetTask>().Init(trial, reachType);
                        break;
                    default:
                        Debug.LogWarning("Task not implemented: " + per_block_type);
                        break;
                }
                break;
        }
    }

    public void EndAndPrepare()
    {
        Debug.Log("Ending: " + Session.CurrentTrial.number);
        BaseTask task = GetComponent<BaseTask>();
        task.enabled = false;
        Destroy(task);
       
        if (Session.CurrentTrial.number == Session.LastTrial.number)
        {
            Session.End();
        }
        else
        {
            Session.CurrentTrial.End();
        }
    }
}
