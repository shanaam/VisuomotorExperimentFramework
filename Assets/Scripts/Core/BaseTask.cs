using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public abstract class BaseTask : MonoBehaviour
{
    protected int currentStep;    // Current step of the task
    protected bool finished;      // Are we out of steps
    protected int maxSteps;       // Number of steps this task has

    /// <summary>
    /// Increments the current step in this task
    /// </summary>
    public virtual bool IncrementStep()
    {
        currentStep++;

        finished = currentStep == maxSteps;
        return finished;
    }

    // References to the home and target GameObjects
    // Use the auto properties to set the value for this
    private GameObject home, target;

    private GameObject[] trackers;

    // This task's "home" position
    public GameObject Home
    {
        get => home;
        protected set => home = value;
    }

    // This task's "target" position
    public GameObject Target
    {
        get => target;
        protected set => target = value;
    }

    protected GameObject[] Trackers
    {
        get
        {
            if (trackers == null)
                Debug.LogWarning("Trackers have not been initialized for this task.");

            return trackers;
        }
        set => trackers = value;
    }

    protected virtual void OnDestroy()
    {
        // Delete trackers
        foreach (GameObject g in Trackers)
        {
            ExperimentController.Instance().Session.trackedObjects
                .Remove(g.GetComponent<PositionRotationTracker>());
            Destroy(g);
        }
    }

    public int GetCurrentStep => currentStep;
    public bool Finished => finished;

    /// <summary>
    /// Logic for setting up a specific trial type
    /// </summary>
    public abstract void Setup();

    /// <summary>
    /// This is called in ExperimentController when the trial ends. Do not call this method
    /// anywhere else.
    /// </summary>
    public virtual void LogParameters()
    {
        ExperimentController ctrler = ExperimentController.Instance();
        Session session = ctrler.Session;

        // Track score if score tracking is enabled in the JSON
        // Defaults to disabled if property does not exist in JSON
        if (session.settings.GetBool("track_score", false))
        {
            session.CurrentTrial.result["score"] = ctrler.Score;
        }
    }

    public abstract void Disable();
}
