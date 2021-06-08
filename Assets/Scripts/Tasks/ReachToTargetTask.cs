using System;
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
    private GameObject[] targets = new GameObject[3];
    private ExperimentController ctrler;
    private Trial trial;

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

    public override void Setup()
    {
        ctrler = ExperimentController.Instance();
        trial = ctrler.Session.CurrentTrial;

        Enum.TryParse(ctrler.Session.CurrentTrial.settings.GetString("per_block_type"), 
            out MovementType rType);

        reachType = new MovementType[3];
        reachType[2] = rType;
        maxSteps = 3;

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
        float targetAngle = ctrler.PollPseudorandomList("per_block_targetListToUse");
        
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

    public override void LogParameters()
    {
        Session session = ExperimentController.Instance().Session;

        session.CurrentTrial.result["home_x"] = Home.transform.localPosition.x;
        session.CurrentTrial.result["home_y"] = Home.transform.localPosition.y;
        session.CurrentTrial.result["home_z"] = Home.transform.localPosition.z;

        session.CurrentTrial.result["target_x"] = Target.transform.localPosition.x;
        session.CurrentTrial.result["target_y"] = Target.transform.localPosition.y;
        session.CurrentTrial.result["target_z"] = Target.transform.localPosition.z;

        base.LogParameters();
    }

    public override void Disable()
    {
        foreach (GameObject g in targets)
            g.SetActive(false);
    }

    protected override void OnDestroy()
    {
        // When the trial ends, we need to delete all the objects this task spawned
        foreach (GameObject g in targets)
            Destroy(g);

        base.OnDestroy();
    }
}
