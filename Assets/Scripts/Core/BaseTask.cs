using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTask : MonoBehaviour
{
    protected int currentStep = 0;        // Current step of the task
    protected bool finished = false;      // Are we out of steps
    protected int maxSteps;               // Number of steps this task has

    public bool IncrementStep()
    {
        currentStep++;

        finished = currentStep == maxSteps;
        return finished;
    }

    public int GetCurrentStep() { return currentStep; }
    public bool Finished() { return finished; }
    public abstract void Setup();
}
