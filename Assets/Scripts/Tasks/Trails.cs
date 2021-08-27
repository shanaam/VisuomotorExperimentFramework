using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public class Trails : BaseTask
{
    private ExperimentController ctrler;

    /*
     * Step 1:
     * spawn at startpoint gate
     * 3 2 1 go timer
     * 
     * Step 2:
     * let car move
     * 
     * Step 3:
     * hit wall or hit finish line gate
     * log parameters
     * end trial
     * 
     */

    public override void Setup()
    {
        maxSteps = 3;
        ctrler = ExperimentController.Instance();


    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Impact()
    {

    }

    public override bool IncrementStep()
    {
        return base.IncrementStep();
    }


    public override void Disable()
    {
        throw new System.NotImplementedException();
    }

    public override void LogParameters()
    {
        throw new System.NotImplementedException();
    }

}
