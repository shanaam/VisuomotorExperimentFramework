using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UXF;
using MovementType = CursorController.MovementType;

public class ToolTask : BaseTask
{

    //TODO: 
    /// <summary>
    /// 
    /// 1_add material changing capabilities 
    /// 
    /// 
    /// 
    /// </summary>

    private MovementType[] reachType;
    private Trial trial;

    private GameObject toolSpace;
    private GameObject tool;
    private GameObject Puckobj; 
    private GameObject toolCamera;
    private GameObject toolSurface;

    private static List<float> targetAngles = new List<float>();

    private ExperimentController ctrler;
    private float  distanceToTarget;
    private List<Vector3> PuckPoints = new List<Vector3>();
    private GameObject oldMainCamera;

    private float height;

    // Allows a delay when the participant initially hits the object
    private float initialDelayTimer;

    private GameObject visualCube;
    private Quaternion cubeRot;
    private Vector3 previousPosition;
    private float Timer;
    private bool enterdTarget = false;


    public void Init(Trial trial, List<float> angles)
    {
        this.trial = trial;
        maxSteps = 4;

        ctrler = ExperimentController.Instance();

        if (trial.numberInBlock == 1)
            targetAngles = angles;

        Setup();
    }

    private void FixedUpdate()
    {

        Debug.Log("current step :" + currentStep);
        
        Vector3 mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, 
            new Vector3(0f, tool.transform.position.y, 0f), toolCamera.GetComponent<Camera>());

        tool.GetComponent<Rigidbody>().velocity = Vector3.zero;

        if (Vector3.Distance(mousePoint, tool.transform.position) > 0.05f && currentStep == 0) return;

        

        switch (currentStep)
        {
            // Return to home position phase
            case 0:

                tool.GetComponent<Rigidbody>().velocity = Vector3.zero;
                if (Vector3.Distance(mousePoint, tool.transform.position) > 0.05f && currentStep == 0)
                {
                    break;
                }
                else
                {
                    IncrementStep();
                }

                break;

            case 1:

                RacketMouseMovment(mousePoint);

                if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                {
                    PuckPoints.Add(Puckobj.transform.position);
                }
                break;

            // After the user hits the object
            // Used to determine if the object hit by the tool is heading away from the target
            // Current distance from pinball to the target`
            case 2:
                RacketMouseMovment(mousePoint);
                float currentDistance = Vector3.Distance(Puckobj.transform.position, Target.transform.position);
                //Debug.Log("this is distance of ball from object: " + currentDistance);


                // Only check when the distance from pinball to target is less than half of the distance
                // between the target and home position and if the pinball is NOT approaching the target
                if (currentDistance <= distanceToTarget / 2f && 
                    currentDistance > Vector3.Distance(previousPosition, Target.transform.position))
                {

                    // The object only has 500ms of total time to move away from the target
                    // After 500ms, the trial ends
                    if (Timer < 0.5f)
                    {
                        Timer += Time.fixedDeltaTime;
                    }
                    else
                    {
                        IncrementStep();
                    }
                }

                if (enterdTarget)
                {
                    // if distance increases from the previous frame, end trial immediately
                    float previousDistanceToTarget = Vector3.Distance(previousPosition, Target.transform.position);

                    // We are now going away from the target, end trial immediately
                    if (distanceToTarget > previousDistanceToTarget)
                    {
                        //lastPositionInTarget = previousPosition;
                        IncrementStep();
                        return;
                    }
                }

                if (distanceToTarget < 0.05f)
                {
                    enterdTarget = true;
                }

              
                previousPosition = Puckobj.transform.position;
                
                break;
            

            // after the either hit the Target or passed by it
            case 3:

                if(Timer == 0)
                {
                    //get Audio Component
                    toolSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["incorrect"];

                    distanceToTarget = Vector3.Distance(previousPosition, Target.transform.position);
                    
                    if(distanceToTarget < 0.05f)
                    {
                        if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                        {
                            toolSpace.GetComponent<LineRenderer>().startColor =
                                toolSpace.GetComponent<LineRenderer>().endColor =
                                    Target.GetComponent<BaseTarget>().Collided ? Color.green : Color.yellow;
                            Target.transform.GetChild(0).GetComponent<ParticleSystem>().Play();

                        }

                        toolSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["correct"];

                        //Freeze puck
                        Puckobj.GetComponent<Rigidbody>().isKinematic = true;

                    }


                }

                if(Timer < 1.5f)
                {
                    Timer += Time.deltaTime;

                    if(Timer > 0.08f)
                    {   
                        //freeze pinball in space
                        Puckobj.GetComponent<Rigidbody>().isKinematic = true;


                        // set pinball trail
                        toolSpace.GetComponent<LineRenderer>().positionCount = PuckPoints.Count;
                        toolSpace.GetComponent<LineRenderer>().SetPositions(PuckPoints.ToArray());

                        if (enterdTarget)
                        {
                            Puckobj.transform.position = previousPosition;
                        }
                        else
                        {
                            Puckobj.transform.position = toolSpace.GetComponent<LineRenderer>().GetPosition(
                                toolSpace.GetComponent<LineRenderer>().positionCount - 1);
                        }

                    }
                    else if(ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback") && !enterdTarget)
                    {

                        // Add points to show feedback past the target only if they missed
                        // Points along the path are not added if they hit the target
                        PuckPoints.Add(Puckobj.transform.position);


                    }


                }
                else
                {
                    LogParameters();
                }
                break;





/*
                // if the user hits the target show the results
                // else give them another try
                if (Target.GetComponent<BaseTarget>().Collided)//(Vector3.Distance(obj.transform.position, Target.transform.position) < 0.025f)
                {

                    Debug.Log("we hit the Target");
                    LogParameters();
                }
                else
                {
                    Debug.Log("we DID NOT hit the Target");
                }

                break;
*/
        }

        if (Finished)
           ctrler.EndAndPrepare();

    }


    public override bool IncrementStep()
    {
        if (currentStep == 0)
        {
            Puckobj.SetActive(true);
            //Home.GetComponent<BaseTarget>().enabled = false;
            //Home.GetComponent<MeshRenderer>().enabled = false;
        }

        return base.IncrementStep();
    }

    protected override void Setup()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        toolSpace = Instantiate(ctrler.GetPrefab("ToolPrefab"));

        tool = GameObject.Find("Tool");
        Puckobj = GameObject.Find("PuckObject");
        Target = GameObject.Find("Target");
        toolCamera = GameObject.Find("ToolCamera");
        toolSurface = GameObject.Find("ToolPlane");

        // Height above the surface. Height is y position of plane
        // plus thickness of surface (0.05) plus the half the width of the tool (0.075)
        height = toolSurface.transform.position.y + 0.08f;

        // Set up home position
        Home = GameObject.Find("HomePosition");


        // Set up target
        float targetAngle = targetAngles[0];
        targetAngles.RemoveAt(0);

        Target.transform.position = new Vector3(0f, 0.08f, 0f);
        Target.transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        Target.transform.position += Target.transform.forward.normalized * 0.55f;

        // Set up camera for non VR and VR modes
        if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
        {
            oldMainCamera = GameObject.Find("Main Camera");
            oldMainCamera.SetActive(false);
        }
        else toolCamera.SetActive(false);

        distanceToTarget = Vector3.Distance(Target.transform.position, Puckobj.transform.position);
        distanceToTarget += 0.15f;


        /*
        // Set up surface friction
        toolSurface.GetComponent<BoxCollider>().material.dynamicFriction =
            ctrler.Session.CurrentTrial.settings.GetFloat("per_block_surface_dynamic_friction");

        toolSurface.GetComponent<BoxCollider>().material.staticFriction =
            ctrler.Session.CurrentTrial.settings.GetFloat("per_block_surface_static_friction");

        // Set up tool friction

        // Set up object
        
        obj.GetComponent<SphereCollider>().material.dynamicFriction =
            ctrler.Session.CurrentTrial.settings.GetFloat("per_block_tool_dynamic_friction");

        obj.GetComponent<SphereCollider>().material.dynamicFriction =
            ctrler.Session.CurrentTrial.settings.GetFloat("per_block_tool_dynamic_friction");
        */

        Puckobj.GetComponent<SphereCollider>().material.bounciness = 0.8f;
        tool.GetComponent<BoxCollider>().material.bounciness = 1f;
        tool.GetComponent<BoxCollider>().enabled = false;

        //visualCube = GameObject.Find("ToolVisualCube");
        //cubeRot = visualCube.transform.rotation;

        /*        // Set up object type
                if (ctrler.Session.CurrentTrial.settings.GetString("per_block_object_type") == "sphere")
                    GameObject.Find("ToolVisualCube").SetActive(false);
                else
                    GameObject.Find("ToolVisualSphere").SetActive(false);*/


        // Disable object for first step
        Puckobj.SetActive(false);
    }

    private void LogParameters()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        ctrler.Session.CurrentTrial.result["Puckobj"] = Puckobj.transform.localPosition.x;
        ctrler.Session.CurrentTrial.result["Puckobj"] = Puckobj.transform.localPosition.y;
        ctrler.Session.CurrentTrial.result["Puckobj"] = Puckobj.transform.localPosition.z;

        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.x;
        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.y;
        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.z;

        IncrementStep();
    }


    private void RacketMouseMovment(Vector3 mousePoint)
    {

        tool.GetComponent<BoxCollider>().enabled = mousePoint.z <= 0.5f;
        Vector3 dir = mousePoint - tool.transform.position;
        dir /= Time.fixedDeltaTime;
        tool.GetComponent<Rigidbody>().velocity = dir;
        tool.GetComponent<BoxCollider>().enabled = mousePoint.z <= 0.05f;

    }

    protected override void OnDestroy()
    {
        Destroy(toolSpace);

        if (ctrler.Session.settings.GetString("experiment_mode") == "tool" && oldMainCamera != null)
            oldMainCamera.SetActive(true);
    }

    void OnDrawGizmos()
    {
        Vector3 mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, new Vector3(
            0f, tool.transform.position.y, 0f), toolCamera.GetComponent<Camera>());

        Gizmos.DrawLine(toolCamera.transform.position, mousePoint);

    }
}
