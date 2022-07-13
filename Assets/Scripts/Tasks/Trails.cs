using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public class Trails : BaseTask
{
    private ExperimentController ctrler;
    private GameObject trailSpace;

    private GameObject trailGate1, trailGate2;

    private BoxCollider startCollider; // Trigger attached to trailgate1 (start gate)
    private BoxCollider midwayCollider; // Standalone trigger used to determine if user is going correct direction
    [SerializeField]
    private List<BaseTarget> innerTrackColliders = new List<BaseTarget>();

    private GameObject railing1, railing2; 

    private GatePlacement gatePlacement; 

    private GameObject roadSegments; 

    private float startPoint, endPoint, midPoint; // Percentages between 0-1 where the start, mid, and end gates will be placed along the track (clockwise)

    private GameObject car;

    [SerializeField]
    private bool carPastMidpoint = false;

    [SerializeField]
    private bool isOnTrack = true;

    // num times car went off track
    [SerializeField]
    private int numImpacts = 0;

    private List<Transform> raycastOrigins = new List<Transform>();

    // Number of triggers spread evenly between start & end point.
    // The user has to contact at least one of these for a lap to count.
    private const int NUM_MID_TRIGGERS = 2;

    private List<BaseTarget> midwayTriggers = new List<BaseTarget>();

    // Whether to use raycasts or use the inner track to dermine whether offtrack
    private bool useRayCasts = false;

    private float inTrackTime, outTrackTime;

    [SerializeField]
    private int score;
    private Scoreboard scoreboard;

    /*
     * Step 0: 
     * 
     * spawn at startpoint gate
     * mouse move to car point
     * 
     * Step 1:
     * let car move
     * 
     * Step 2:
     * hit wall or hit finish line gate
     * log parameters
     * end trial
     * 
     * Step 3:
     * finished
     * 
     */

    public override void Setup()
    {
        maxSteps = 3;
        ctrler = ExperimentController.Instance();

        trailSpace = Instantiate(ctrler.GetPrefab("TrailPrefab"));

        trailGate1 = GameObject.Find("TrailGate1");
        trailGate2 = GameObject.Find("TrailGate2");

        startCollider = trailGate1.transform.GetChild(3).GetComponent<BoxCollider>();
        midwayCollider = GameObject.Find("MidwayCollider").GetComponent<BoxCollider>();

        gatePlacement = GameObject.Find("gatePlacement").GetComponent<GatePlacement>();

        roadSegments = GameObject.Find("generated_by_SplineExtrusion");

        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();

        for (int i = 0; i < roadSegments.transform.childCount; i++)
        { // add road segments to gatePlacement list of meshes
            gatePlacement.mesh.Add(roadSegments.transform.GetChild(i).GetComponent<MeshFilter>().mesh);
        }
        gatePlacement.Setup();

        /* TrailGate children:
        * 0: pole1
        * 2: pole2 
        * 3: checkered line (line renderer component)
        * 4: trigger (collider component)
        */

        startPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_startPoint");
        gatePlacement.SetGatePosition(trailGate1, trailGate1.transform.GetChild(0).gameObject, trailGate1.transform.GetChild(1).gameObject,
            trailGate1.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate1.transform.GetChild(3).GetComponent<BoxCollider>(), startPoint);

        endPoint = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_endPoint");
        gatePlacement.SetGatePosition(trailGate2, trailGate2.transform.GetChild(0).gameObject, trailGate2.transform.GetChild(1).gameObject,
            trailGate2.transform.GetChild(2).GetComponent<LineRenderer>(), trailGate2.transform.GetChild(3).GetComponent<BoxCollider>(), endPoint);

        // Place midway triggers throughout the track
        for (int i = 0; i < NUM_MID_TRIGGERS; i++)
        {
            midwayTriggers.Add(Instantiate(midwayCollider.gameObject).GetComponent<BaseTarget>());

            // Start    Mid1     Mid2     End
            // |--------|--------|--------|
            if (endPoint < startPoint)
            {
                // If the end point comes before the start point

                float distance = 1 - startPoint + endPoint;

                midPoint = ((distance) / (NUM_MID_TRIGGERS + 1)) * (i + 1) + startPoint;

                if (midPoint > 1)
                    midPoint -= 1;
            }
            else
            {
                float distance = endPoint - startPoint;

                midPoint = ((distance) / (NUM_MID_TRIGGERS + 1)) * (i + 1) + startPoint;
            }

            gatePlacement.SetColliderPosition(midwayTriggers[i].GetComponent<BoxCollider>(), midPoint);
        }


        railing1 = GameObject.Find("generated_by_SplineMeshTiling");
        foreach (MeshCollider railing in railing1.transform.GetComponentsInChildren<MeshCollider>())
        {
            railing.tag = "TrailRailing";
            railing.convex = true;
            railing.isTrigger = true;
            railing.gameObject.SetActive(false);
        }

        railing2 = GameObject.Find("generated_by_SplineMeshTiling_1");
        foreach (MeshCollider railing in railing1.transform.GetComponentsInChildren<MeshCollider>())
        {
            railing.tag = "TrailRailing";
            railing.convex = true;
            railing.isTrigger = true;
            railing.gameObject.SetActive(false);
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

        car.transform.position = trailGate1.transform.position;

        raycastOrigins.AddRange(car.GetComponentsInChildren<Transform>());


        innerTrackColliders.AddRange(GameObject.Find("innertrack").transform.GetComponentsInChildren<BaseTarget>());

        if (ctrler.Session.currentTrialNum > 1)
        {
            trailGate1.GetComponentInChildren<ParticleSystem>().transform.position = trailGate1.transform.position;
            trailGate1.GetComponentInChildren<ParticleSystem>().transform.rotation = trailGate1.transform.rotation;
            trailGate1.GetComponentInChildren<ParticleSystem>().Play();
            trailSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["correct"];
            trailSpace.GetComponent<AudioSource>().Play();
        }

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
            case 0:

                break;

            case 1:

                bool onTrack;
                if (useRayCasts)
                {
                    onTrack = true;
                    // Use raycasts to determine if car is on track
                    foreach (Transform t in raycastOrigins)
                    {
                        // if any rays don't hit a collider, then the car is at least partially off the track 
                        if (!Physics.Raycast(t.position, t.TransformDirection(Vector3.down)))
                            onTrack = false;
                    }
                }
                else
                {
                    onTrack = false;
                    // Use inner track to determine if car (must be a cylinder with 0.5 scale) is on the track
                    foreach (BaseTarget innerTrackSegment in innerTrackColliders)
                    {
                        // if the cylinder is at least slightly touching any inner track segment, then the car is still on the main track.
                        if (innerTrackSegment.Collided)
                        {
                            onTrack = true;
                        }
                    }
                }

                if (isOnTrack && !onTrack)
                {
                    isOnTrack = onTrack;
                    numImpacts++;
                    car.GetComponent<MeshRenderer>().material.color = Color.red;
                    score = numImpacts;
                    ctrler.Score = score;
                    trailSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["incorrect"];
                    trailSpace.GetComponent<AudioSource>().Play();
                }
                else if (!isOnTrack && onTrack)
                {
                    isOnTrack = onTrack;
                    car.GetComponent<MeshRenderer>().material.color = Color.white;
                }    

                break;
        }

        
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentStep)
        {
            case 0:

                // mouse is inside gate 1 collider
                if (trailGate1.transform.GetChild(3).GetComponent<BoxCollider>().ClosestPoint
                    (ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main)) ==
                    ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main))
                {
                    IncrementStep();
                }

                // mouse gets near car
                if ((ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main) - car.transform.position).magnitude < 0.15f)
                {
                    //IncrementStep();
                }
                break;

            case 1:

                foreach (BaseTarget t in midwayTriggers)
                {
                    // if the car hits the midway trigger, it is going the correct way
                    if (t.Collided)
                        carPastMidpoint = true;
                }    
                

                // if the car hits the start gate trigger, it is not going the right way 
                if (startCollider.GetComponent<BaseTarget>().Collided)
                    carPastMidpoint = false;

                // car position = mouse position
                car.transform.position = ctrler.CursorController.MouseToPlanePoint(transform.up, car.transform.position, Camera.main);

                if (isOnTrack)
                    inTrackTime += Time.deltaTime;
                else
                    outTrackTime += Time.deltaTime;

                break;
            case 2:
                IncrementStep();
                    break;
        }

        if (Finished) ctrler.EndAndPrepare();
    }

    public override bool IncrementStep()
    {
        switch (currentStep)
        {
            case 0:
                // make the start trigger smaller after the car is picked up
                startCollider.size = new Vector3(startCollider.size.z, startCollider.size.y, 0.1f);

                ctrler.StartTimer();

                ctrler.AddTrackedPosition("car_path", car);

                break;
            case 1:
                if (!carPastMidpoint)
                    return false;

                break;
        }


        return base.IncrementStep();
    }

    public void Impact()
    {
        Debug.Log("Hit!");
        //IncrementStep();
    }

    public override void LogParameters()
    {
        ctrler.LogObjectPosition("car", car.transform.position);

        ctrler.Session.CurrentTrial.result["time_in_track"] = inTrackTime;
        ctrler.Session.CurrentTrial.result["time_out_track"] = outTrackTime;
        ctrler.Session.CurrentTrial.result["percent_in_track"] = inTrackTime / (inTrackTime + outTrackTime);
        ctrler.Session.CurrentTrial.result["lap_time"] = outTrackTime + inTrackTime;
        ctrler.Session.CurrentTrial.result["num_impacts"] = numImpacts;

        ctrler.Score = score;

        ctrler.Session.CurrentTrial.result["start_gate_placement"] = startPoint;
        ctrler.Session.CurrentTrial.result["start_gate_placement"] = endPoint;

    }

    public override void Disable()
    {
        // Realign XR Rig to non-tilted position
        if (ctrler.Session.settings.GetString("experiment_mode") == "trail_vr")
        {
           /* XRRig.transform.RotateAround(trailSpace.transform.position, trailSpace.transform.forward,
                ctrler.Session.CurrentBlock.settings.GetFloat("per_block_tilt") * -1);*/
        }

        trailSpace.SetActive(false);
    }

    protected override void OnDestroy()
    {
        foreach (BaseTarget t in midwayTriggers)
        {
            Destroy(t.gameObject);
        }
        Destroy(trailSpace);
    }
}
