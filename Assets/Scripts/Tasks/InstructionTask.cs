﻿using UnityEngine;
using UnityEngine.UI;
using UXF;

public class InstructionTask : BaseTask
{
    /// <summary>
    /// this is an instruction script:
    /// will get the required instructions from the json File
    /// starts the time
    /// </summary>

    private ExperimentController ctrler;

    private string ins;
    private double timeRemaining = 10f;

    private GameObject instructionPanel;
    private GameObject instruction;
    private GameObject timer;
    private GameObject done;

    private Camera tempMainCamera;

    public override void Setup()
    {
        ctrler = ExperimentController.Instance();

        ins = ctrler.Session.CurrentTrial.settings.GetString("per_block_instruction");
        // Temporarily disable VR Camera
        // TODO: This needs to be changed when we implement instruction task for VR
        ctrler.CursorController.SetVRCamera(false);

        //Task GameObjects
        instructionPanel = Instantiate(ctrler.GetPrefab("InstructionPanel"), this.transform);

        instruction = GameObject.Find("Instruction");
        timer = GameObject.Find("Timer");
        done = GameObject.Find("Done");

        instruction.GetComponent<Text>().text = ins;

        //countdown Timer start
        timer.GetComponent<Text>().text = System.Math.Round(timeRemaining, 2).ToString();

        //add event listener to done button
        done.GetComponent<Button>().onClick.AddListener(()=>End() );
    }

    private void Update()
    {
        if(timeRemaining > 0)
        {
            timeRemaining = timeRemaining - Time.deltaTime;
            timer.GetComponent<Text>().text = System.Math.Round(timeRemaining, 2).ToString();
        }
        else
        {
            //Enable Done Button
            done.GetComponent<Button>().interactable = true;
        }
    }

    void End()
    {
        ctrler.EndAndPrepare();
    }

    protected override void OnDestroy()
    {
        instructionPanel.SetActive(false);
        Destroy(instructionPanel);

        // Turn VR Camera back on
        // TODO: See Setup()
        ctrler.CursorController.SetVRCamera(true);
    }
}
