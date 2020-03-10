using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public class ReachToTargetTask : BaseTask
{
    // The current steps are as follows:
    // 1. User goes to DOCK position (the starting position)
    // 2. User moves FORWARD to HOME position (aligned)
    // 3. User moves to TARGET with reachType[1]
    // 4. 

    ReachExperiment.ReachType[] reachType;  // Reach type for current step
    Trial trial;

    /// <summary>
    /// Initializes a task where you move from a starting position to
    /// a target in space
    /// </summary>
    /// <param name="reachType">Reach type from HOME to TARGET.</param>
    public void Init(Trial trial, ReachExperiment.ReachType reachType)
    {
        this.reachType = new ReachExperiment.ReachType[3];
        this.reachType[2] = reachType;
        this.trial = trial;
        maxSteps = 3;
    }

    public void Update()
    {
        if (Input.GetKeyDown("n"))
        {
            IncrementStep();
        }

        if (Finished())
        {
            ExperimentController.Instance().EndAndPrepare();
        }
    }

    public override void Setup()
    {

    }
}
