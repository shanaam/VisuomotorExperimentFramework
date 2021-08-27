using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public class Trails : BaseTask
{
    private ExperimentController ctrler;
    private GameObject trailSpace;

    private GameObject trailGate1, trailGate2;

    private GameObject railing1, railing2;

    private GatePlacement gatePlacement;

    private GameObject roadSegments;

    private float startPoint, endPoint;

    private GameObject car;

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

        trailSpace = Instantiate(ctrler.GetPrefab("TrailPrefab"));

        trailGate1 = GameObject.Find("TrailGate1");
        trailGate2 = GameObject.Find("TrailGate2");

        gatePlacement = GameObject.Find("gatePlacement").GetComponent<GatePlacement>();

        roadSegments = GameObject.Find("generated_by_SplineExtrusion");
        
        for (int i = 0; i < roadSegments.transform.childCount; i++)
        {
            gatePlacement.mesh.Add(roadSegments.transform.GetChild(i).GetComponent<MeshFilter>().mesh);
        }

        //gatePlacement.mesh.AddRange(roadSegments.GetComponentsInChildren<Mesh>());

        gatePlacement.Setup();

        startPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_startPoint");
        endPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_endPoint");

        gatePlacement.SetGatePosition(trailGate1.transform.GetChild(0).gameObject, trailGate1.transform.GetChild(1).gameObject,
            trailGate1.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate1.transform.GetChild(3).GetComponent<BoxCollider>(), startPoint);

        gatePlacement.SetGatePosition(trailGate2.transform.GetChild(0).gameObject, trailGate2.transform.GetChild(1).gameObject,
            trailGate2.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate2.transform.GetChild(3).GetComponent<BoxCollider>(), endPoint);

        railing1 = GameObject.Find("generated_by_SplineMeshTiling");
        railing2 = GameObject.Find("generated_by_SplineMeshTiling_1");

        foreach (Transform railing in railing1.transform.GetComponentsInChildren<Transform>())
        {
            railing.tag = "TrailRailing";
        }

        foreach (Transform railing in railing2.transform.GetComponentsInChildren<Transform>())
        {
            railing.tag = "TrailRailing";
        }

        car = GameObject.Find("Car");

        // Use static camera for non-vr version of pinball
        if (ctrler.Session.settings.GetString("experiment_mode") == "trail")
        {
            ctrler.CursorController.SetVRCamera(false);
        }
        else
        {

        }

    }

    private void FixedUpdate()
    {
        Vector3 dir = ctrler.CursorController.MouseToPlanePoint(transform.up, Vector3.zero, Camera.main) - car.transform.position;
        //dir /= Time.deltaTime;
        car.GetComponent<Rigidbody>().velocity = dir;
    }

    // Update is called once per frame
    void Update()
    {
       


        if (Finished) ctrler.EndAndPrepare();
    }

    public void Impact()
    {
        IncrementStep();
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
