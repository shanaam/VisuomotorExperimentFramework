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
        ExperimentController.Instance().StepTimer.Add(Time.time);

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


    protected void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            CenterExperiment();
        }
    }

    public void CenterExperiment()
    {
        ExperimentController.Instance().CenterExperiment();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="channel">The channel to receive the impulse.</param>
    /// <param name="amplitude">The normalized (0.0 to 1.0) amplitude value of the haptic impulse to play on the device.</param>
    /// <param name="duration">The duration in seconds that the haptic impulse will play.</param>
    /// <param name="devices">List of InputDevices</param>
    /// <returns>  </returns>
    public bool VibrateController(uint channel, float amplitude, float duration, List<UnityEngine.XR.InputDevice> devices)
    {
        foreach (var device in devices)
        {
            UnityEngine.XR.HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    return device.SendHapticImpulse(channel, amplitude, duration);
                }
            }
        }

        return false;
    }
}
