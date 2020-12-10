﻿using System.Collections;
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

    private static List<float> targetAngles = new List<float>();

    /// <summary>
    /// Initializes a task where you move from a starting position to
    /// a target in space
    /// </summary>
    /// <param name="reachType">Reach type from HOME to TARGET.</param>
    public void Init(Trial trial, MovementType reachType, List<float> angles)
    {
        this.reachType = new MovementType[3];
        this.reachType[2] = reachType;
        this.trial = trial;
        maxSteps = 3;

        if (trial.numberInBlock == 1)
            targetAngles = angles;

        Setup();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
            IncrementStep();

        if (currentStep == 2 &&
            ExperimentController.Instance().CursorController.PauseTime > 0.5f &&
            ExperimentController.Instance().CursorController.DistanceFromHome > 0.05f &&
            trial.settings.GetString("per_block_type") == "nocursor")
            IncrementStep();

        if (Finished)
            ExperimentController.Instance().EndAndPrepare();
    }

    public override bool IncrementStep()
    {
        targets[currentStep].SetActive(false);

        switch (currentStep)
        {
            // If the user enters the home, start tracking time
            case 1:
                ExperimentController.Instance().StartTimer();
                ExperimentController.Instance().CursorController.SetMovementType(reachType[2]);

                if (trial.settings.GetString("per_block_type") == "nocursor")
                    ExperimentController.Instance().CursorController.SetCursorVisibility(false);

                foreach (GameObject g in Trackers)
                    g.GetComponent<PositionRotationTracker>().StartRecording();

                break;
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
        ctrler.CursorController.SetCursorVisibility(true);

        // Set up the dock position
        targets[0] = Instantiate(ctrler.GetPrefab("Target"));
        targets[0].transform.position = ctrler.TargetContainer.transform.position;
        targets[0].name = "Dock";

        // Set up the home position
        targets[1] = Instantiate(ctrler.GetPrefab("Target"));
        targets[1].transform.position = ctrler.TargetContainer.transform.position + ctrler.transform.forward * 0.05f;
        targets[1].SetActive(false);
        targets[1].name = "Home";
        Home = targets[1];

        // Set up the target

        // Takes a target angle from the list and removes it
        float targetAngle = targetAngles[0];
        targetAngles.RemoveAt(0);
        
        targets[2] = Instantiate(ctrler.GetPrefab("Target"));
        targets[2].transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        targets[2].transform.position = targets[1].transform.position +
                                        targets[2].transform.forward.normalized *
                                        (trial.settings.GetFloat("per_block_distance") / 100f);

        // Disable collision detection for nocursor task
        if (trial.settings.GetString("per_block_type") == "nocursor")
            targets[2].GetComponent<BaseTarget>().enabled = false;

        targets[2].SetActive(false);
        targets[2].name = "Target";
        Target = targets[2];

        // Parents everything to the target container
        foreach (GameObject g in targets)
            g.transform.SetParent(ctrler.TargetContainer.transform);

        // Create tracker objects
        Trackers = new GameObject[2];

        Trackers[0] = ctrler.GenerateTracker("handtracker",
            ctrler.Session.CurrentTrial.settings.GetString("per_block_hand") == "l"
                ? ctrler.CursorController.LeftHand.transform
                : ctrler.CursorController.RightHand.transform);

        Trackers[1] = ctrler.GenerateTracker("cursortracker", ctrler.CursorController.transform);

        foreach (GameObject g in Trackers)
            ctrler.Session.trackedObjects.Add(g.GetComponent<PositionRotationTracker>());
    }

    protected override void OnDestroy()
    {
        // When the trial ends, we need to delete all the objects this task spawned
        foreach (GameObject g in targets)
            Destroy(g);

        base.OnDestroy();
    }
}
