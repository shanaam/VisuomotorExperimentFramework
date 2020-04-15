using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public abstract class BaseTask : MonoBehaviour
{
    protected int currentStep;    // Current step of the task
    protected bool finished;      // Are we out of steps
    protected int maxSteps;       // Number of steps this task has

    // References to the home and target GameObjects
    // Use the auto properties to set the value for this
    private GameObject home, target;

    // This task's "home" position
    public virtual GameObject Home 
    {
        get
        {
            if (home == null)
                Debug.LogWarning("Home is not set for this task.");

            return home;
        }
        protected set => home = value;
    }

    // This task's "target" position
    public virtual GameObject Target
    {
        get
        {
            if (home == null)
                Debug.LogWarning("Target is not set for this task.");

            return target;
        }
        protected set => target = value;
    }

    /// <summary>
    /// Increments the current step in this task
    /// </summary>
    public virtual bool IncrementStep()
    {
        currentStep++;

        finished = currentStep == maxSteps;
        return finished;
    }

    public int GetCurrentStep => currentStep;
    public bool Finished => finished;

    /// <summary>
    /// Logic for setting up a specific trial type
    /// </summary>
    protected abstract void Setup();
}
