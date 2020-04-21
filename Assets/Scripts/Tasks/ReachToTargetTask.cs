using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using MovementType = CursorController.MovementType;

public class ReachToTargetTask : BaseTask
{
    // The current steps are as follows:
    // 1. User goes to DOCK position (the starting position)
    // 2. User moves FORWARD to HOME position (aligned)
    // 3. User moves to TARGET with reachType[1]

    MovementType[] reachType;  // Reach type for current step
    Trial trial;
    private GameObject[] targets = new GameObject[3];

    private List<float> targetAngles;

    /// <summary>
    /// Initializes a task where you move from a starting position to
    /// a target in space
    /// </summary>
    /// <param name="reachType">Reach type from HOME to TARGET.</param>
    public void Init(Trial trial, MovementType reachType)
    {
        this.reachType = new MovementType[3];
        this.reachType[2] = reachType;
        this.trial = trial;
        maxSteps = 3;

        Setup();
    }

    public void Update()
    {
        if (Input.GetKeyDown("n"))
            IncrementStep();

        if (Finished)
            ExperimentController.Instance().EndAndPrepare();
    }

    public override bool IncrementStep()
    {
        targets[currentStep].SetActive(false);

        // If the user enters the home, start tracking time
        if (currentStep == 1)
        {
            ExperimentController.Instance().OnEnterHome();
            ExperimentController.Instance().CursorController.SetMovementType(reachType[2]);
        }

        base.IncrementStep();

        if (!finished)
            targets[currentStep].SetActive(true);

        return finished;
    }

    protected override void Setup()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        // Set up hand and cursor
        ctrler.CursorController.SetHandVisibility(false);

        // Set up the dock position
        targets[0] = Instantiate(ctrler.TargetPrefab);
        targets[0].transform.position = ctrler.TargetContainer.transform.position;
        targets[0].name = "Dock";

        // Set up the home position
        targets[1] = Instantiate(ctrler.TargetPrefab);
        targets[1].transform.position = ctrler.TargetContainer.transform.position + ctrler.transform.forward * 0.05f;
        targets[1].SetActive(false);
        targets[1].name = "Home";
        Home = targets[1];

        // Set up the target

        // Get target angles from list
        targetAngles = ctrler.Session.settings.GetFloatList(
            trial.settings.GetString("per_block_targetListToUse")
        );

        // Select a random angle from the list and use it as the target angle
        // Uses psuedo-random
        
        targets[2] = Instantiate(ctrler.TargetPrefab);
        targets[2].transform.rotation = Quaternion.Euler(
            0f, 
            -targetAngles[Random.Range(0, targetAngles.Count - 1)] + 90f, 
            0f);

        targets[2].transform.position = targets[1].transform.position + 
            targets[2].transform.forward.normalized * (trial.settings.GetFloat("per_block_distance") / 100f);
        
        targets[2].SetActive(false);
        targets[2].name = "Target";
        Target = targets[2];

        // Parents everything to the target container
        foreach (GameObject g in targets)
            g.transform.SetParent(ctrler.TargetContainer.transform);
    }

    void OnDestroy()
    {
        // When the trial ends, we need to delete all the objects this task spawned
        foreach (GameObject g in targets)
            Destroy(g);
    }
}
