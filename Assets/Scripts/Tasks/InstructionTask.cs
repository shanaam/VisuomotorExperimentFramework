using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    private double timeRemaing = 10f;


    private GameObject InstrucitonPanel;
    private GameObject Instruction;
    private GameObject Timer;
    private GameObject Done;



    public void init(Trial trial, string instruction)
    {
        ctrler = ExperimentController.Instance();
        ins = instruction;

        Setup();
    }


    protected override void Setup()
    {
        //Task GameObjects
        InstrucitonPanel = Instantiate(ctrler.GetPrefab("InstrucitonPanel"), this.transform);

        Instruction = GameObject.Find("Instruction");
        Timer = GameObject.Find("Timer");
        Done = GameObject.Find("Done");

        Instruction.GetComponent<Text>().text = ins;

        //countdown Timer start
        Timer.GetComponent<Text>().text = System.Math.Round(timeRemaing, 2).ToString();

        //add event listener to done button
        Done.GetComponent<Button>().onClick.AddListener(delegate { End(); });

    }

    private void Update()
    {
        
        if(timeRemaing > 0)
        {
            timeRemaing = timeRemaing - Time.deltaTime;
            Timer.GetComponent<Text>().text = System.Math.Round(timeRemaing, 2).ToString();
        }
        else
        {
            //Enable Done Button
            //Done.GetComponent<Button>().interactable = true;

        }
    }


    void End()
    {
        ctrler.EndAndPrepare();
    }




    protected override void OnDestroy()
    {

        InstrucitonPanel.SetActive(false);
        Destroy(InstrucitonPanel);
    }



}
