﻿using System;
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
    private GameObject[] targets = new GameObject[3];
    private ExperimentController ctrler;
    private Trial trial;

    private GameObject reachPrefab;
    private GameObject reachCam;
    private GameObject reachSurface;
    private GameObject waterBowl;
    private GameObject water;
    private TimerIndicator pinballTimerIndicator;
    private int id;
    LTDescr d;

    private float speed = 1;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
            IncrementStep();

        if (currentStep == 2 &&
            ctrler.CursorController.PauseTime > 0.5f &&
            ctrler.CursorController.DistanceFromHome > 0.05f &&
            trial.settings.GetString("per_block_type") == "nocursor")
            IncrementStep();

        if (Finished)
            ctrler.EndAndPrepare();

        // checks if there is a water animation in the scene
        if (d != null)
        {
            // after animation has comleted and the current step is the home step it sets the home ball to active
            if (!LeanTween.isTweening(id) && currentStep == 1)
            {
                targets[1].SetActive(true);
            }
        }
    }

    public override bool IncrementStep()
    {
        targets[currentStep].SetActive(false);

        switch (currentStep)
        {
            // If the user enters the home, start tracking time
            case 1:
                ctrler.StartTimer();
                ctrler.CursorController.SetMovementType(reachType[2]);

                // Start green timer bar
                pinballTimerIndicator.GetComponent<TimerIndicator>().BeginTimer();

                if (trial.settings.GetString("per_block_type") == "nocursor")
                    ctrler.CursorController.SetCursorVisibility(false);

                // Add trackers: current hand position, cursor position
                ctrler.AddTrackedObject("hand_path",
                    ctrler.Session.CurrentTrial.settings.GetString("per_block_hand") == "l"
                        ? ctrler.CursorController.LeftHand
                        : ctrler.CursorController.RightHand);

                ctrler.AddTrackedObject("cursor_path", ctrler.CursorController.gameObject);

                break;
        }

        base.IncrementStep();

        if (!finished)
            // if current step is home step and there is a water animation in the scene, it sets the home ball to innactive
            if(currentStep == 1 && d != null)
            {
                targets[1].SetActive(false);
            }
            else
            {
                targets[currentStep].SetActive(true);
            }
            

        return finished;
    }

    public override void Setup()
    {
        ctrler = ExperimentController.Instance();

        trial = ctrler.Session.CurrentTrial;

        Cursor.visible = false;

        reachPrefab = Instantiate(ctrler.GetPrefab("ReachPrefab"));
        reachPrefab.transform.SetParent(ctrler.transform);
        reachPrefab.transform.localPosition = Vector3.zero;

        reachCam = GameObject.Find("ReachCamera");
        reachSurface = GameObject.Find("Surface");
        waterBowl = GameObject.Find("Bowl");
        water = GameObject.Find("Water");
        pinballTimerIndicator = GameObject.Find("TimerIndicator").GetComponent<TimerIndicator>();

        pinballTimerIndicator.Timer = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_timerTime");

        Enum.TryParse(ctrler.Session.CurrentTrial.settings.GetString("per_block_type"), 
            out MovementType rType);

        reachType = new MovementType[3];
        reachType[2] = rType;
        maxSteps = 3;

        // Set up hand and cursor
        ctrler.CursorController.SetHandVisibility(false);
        ctrler.CursorController.SetCursorVisibility(true);

        // Set up the dock position
        targets[0] = GameObject.Find("Dock");
        targets[0].transform.position = ctrler.TargetContainer.transform.position;

        // Set up the home position
        targets[1] = GameObject.Find("Home");
        targets[1].transform.position = ctrler.TargetContainer.transform.position + ctrler.transform.forward * 0.05f;
        targets[1].SetActive(false);
        Home = targets[1];

        // Set up the target

        // Takes a target angle from the list and removes it
        float targetAngle = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_targetListToUse"));
        
        targets[2] = GameObject.Find("Target");
        targets[2].transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        targets[2].transform.position = targets[1].transform.position +
                                        targets[2].transform.forward.normalized *
                                        (trial.settings.GetFloat("per_block_distance") / 100f);

        // Disable collision detection for nocursor task
        if (trial.settings.GetString("per_block_type") == "nocursor")
            targets[2].GetComponent<BaseTarget>().enabled = false;

        targets[2].SetActive(false);
        Target = targets[2];

        // Use static camera for non-vr version of pinball
        if (ctrler.Session.settings.GetString("experiment_mode") == "target")
        {
            reachSurface.SetActive(false);
            reachCam.SetActive(false);
            ctrler.CursorController.UseVR = true;
        }
        else
        {
            ctrler.CursorController.SetVRCamera(false);
        }

        // sets up the water in the level
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_waterPresent") == "wp1")
        {
            float waterLevel = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_waterPresent"));
            waterBowl.SetActive(true);
            water.SetActive(true);


            // If previous trial had a water level, animate water level rising/falling from that level
            try
            {
                if (ctrler.Session.PrevTrial.result.ContainsKey("per_block_waterPresent"))
                {
                    water.transform.localPosition =
                        new Vector3(water.transform.localPosition.x,
                        Convert.ToSingle(ctrler.Session.PrevTrial.result["per_block_waterPresent"]) / 10,
                        water.transform.localPosition.z);

                    id = LeanTween.moveLocalY(water, waterLevel / 10, speed).id;
                    d = LeanTween.descr(id);
                }
                else
                {
                    water.transform.localPosition = new Vector3(0, -0.03f, 0);
                    id = LeanTween.moveLocalY(water, waterLevel / 10, speed).id;
                    d = LeanTween.descr(id);
                }
            }
            catch (NoSuchTrialException e)
            {
                water.transform.localPosition = new Vector3(0, -0.03f, 0);
                id = LeanTween.moveLocalY(water, waterLevel / 10, speed).id;
                d = LeanTween.descr(id);
            }
            
        }
        else
        {
            waterBowl.SetActive(false);
            water.SetActive(false);
        }

        
    }

    public override void LogParameters()
    {
        Session session = ctrler.Session;

        session.CurrentTrial.result["home_x"] = Home.transform.localPosition.x;
        session.CurrentTrial.result["home_y"] = Home.transform.localPosition.y;
        session.CurrentTrial.result["home_z"] = Home.transform.localPosition.z;

        session.CurrentTrial.result["target_x"] = Target.transform.localPosition.x;
        session.CurrentTrial.result["target_y"] = Target.transform.localPosition.y;
        session.CurrentTrial.result["target_z"] = Target.transform.localPosition.z;
    }

    public override void Disable()
    {
        reachPrefab.SetActive(false);

        ctrler.CursorController.SetVRCamera(true);
    }

    protected override void OnDestroy()
    {
        // When the trial ends, we need to delete all the objects this task spawned
        Destroy(reachPrefab);

        base.OnDestroy();
    }
}
