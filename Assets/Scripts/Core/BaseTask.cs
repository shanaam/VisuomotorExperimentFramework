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

        // Track the time when a step is incremented
        ExperimentController.Instance().StepTimer.Add(ExperimentController.Instance().GetElapsedTime());

        finished = currentStep == maxSteps;
        return finished;
    }

    private GameObject[] trackers;

    // This task's "home" position
    public GameObject Home { get; protected set; }

    // This task's "target" position
    public GameObject Target { get; protected set; }

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
    public abstract void LogParameters();

    public abstract void Disable();
}
