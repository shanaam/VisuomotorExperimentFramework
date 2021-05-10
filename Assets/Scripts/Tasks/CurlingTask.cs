using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public class CurlingTask : BaseTask
{
    private Trial trial;
    private ExperimentController ctrler;
    private static List<float> targetAngles = new List<float>();


    //Task Varibles

    private GameObject Camera;
    private GameObject Plane;
    private GameObject Target;
    private GameObject BallObject;
    private GameObject HomePosition;
    private GameObject CurlingSpace;



    public void Init(Trial trial, List<float> angles)
    {
        this.trial = trial;
        maxSteps = 4;

        ctrler = ExperimentController.Instance();

        if (trial.numberInBlock == 1)
            targetAngles = angles;

        Setup();
    }

    protected override void Setup()
    {
        // throw new System.NotImplementedException();

        ExperimentController ctrler = ExperimentController.Instance();

        CurlingSpace = GameObject.Find("CurlingPrefab");

        Camera = GameObject.Find("CurlingCamera");
        Plane = GameObject.Find("CurlingPlane");
        Target = GameObject.Find("CurlingTarget");
        BallObject = GameObject.Find("CurlingBall");
        HomePosition = GameObject.Find("CurlingHomePosition");


    }

    // Update is called once per frame
    void FixedUpdate()
    {


        // have the ball move based on mouser position



/*        Vector3 mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up,
            new Vector3(0f, BallObject.transform.position.y, 0f), Camera.GetComponent<Camera>());

        Vector3 dir = mousePoint / Time.deltaTime;
        BallObject.GetComponent<Rigidbody>().velocity = dir;*/


    }
}
