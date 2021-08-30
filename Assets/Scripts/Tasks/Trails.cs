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

    [SerializeField]
    private float carFollowMouseSpeed = 25;

    [SerializeField]
    private bool carSpeedDependantOnMouseDistance = false;

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

        gatePlacement.Setup();

        startPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_startPoint");
        
        gatePlacement.SetGatePosition(trailGate1.transform.GetChild(0).gameObject, trailGate1.transform.GetChild(1).gameObject,
            trailGate1.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate1.transform.GetChild(3).GetComponent<BoxCollider>(), startPoint);

        endPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_endPoint");

        gatePlacement.SetGatePosition(trailGate2.transform.GetChild(0).gameObject, trailGate2.transform.GetChild(1).gameObject,
            trailGate2.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate2.transform.GetChild(3).GetComponent<BoxCollider>(), endPoint);

        railing1 = GameObject.Find("generated_by_SplineMeshTiling");
        foreach (Transform railing in railing1.transform.GetComponentsInChildren<Transform>())
        {
            railing.tag = "TrailRailing";
        }

        railing2 = GameObject.Find("generated_by_SplineMeshTiling_1");
        foreach (Transform railing in railing2.transform.GetComponentsInChildren<Transform>())
        {
            railing.tag = "TrailRailing";
        }

        for(int i = 0; i < railing1.transform.GetChild(0).transform.childCount; i++)
        {
            railing1.transform.GetChild(i).gameObject.AddComponent<BaseTarget>();
        }

        for (int i = 0; i < railing2.transform.childCount; i++)
        {
            railing2.transform.GetChild(i).gameObject.AddComponent<BaseTarget>();
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
        switch (currentStep)
        {
            case 2:

                Vector3 dir = ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main) - car.transform.position;

                if (dir.magnitude > 1 && !carSpeedDependantOnMouseDistance)
                    dir.Normalize();

                dir *= carFollowMouseSpeed;

                car.GetComponent<Rigidbody>().velocity = dir;

                break;
        }

        
    }

    // Update is called once per frame
    void Update()
    {


        if (Finished) ctrler.EndAndPrepare();
    }

    public void Impact()
    {
        Debug.Log("Hit!");
        //IncrementStep();
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
