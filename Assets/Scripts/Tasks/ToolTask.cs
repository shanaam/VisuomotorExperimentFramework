using System.Collections.Generic;
using UnityEngine;
using UXF;
using MovementType = CursorController.MovementType;

public class ToolTask : BilliardsTask
{

    //TODO: 
    /// <summary>
    /// 
    /// DIFFREENT TYPES OF RACKETS
    /// 
    /// PINBALL STYLE SHOOTING 
    /// 
    /// 
    /// 
    /// 
    /// </summary>

    private MovementType[] reachType;
    private Trial trial;

    // Allows a delay when the participant initially hits the object
    private float initialDelayTimer;
    private GameObject visualCube;
    private Quaternion cubeRot;
    private float InitialDistanceToTarget;

    private GameObject toolSpace;
    private GameObject tool;
    private GameObject puckobj;
    private GameObject ballObject;
    private GameObject toolCamera;
    private GameObject toolSurface;
    private GameObject grid;

    private GameObject chosenObj;

    private const float TARGET_DISTANCE = 0.55f;
    private ExperimentController ctrler;
    
    private List<Vector3> PuckPoints = new List<Vector3>();
    private GameObject oldMainCamera;

    private Vector3 previousPosition;
    private float missTimer;
    private float timer;
    private float delayTimer;
    private bool enteredTarget;

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
                if (Vector3.Distance(mousePoint, tool.transform.position) <= 0.05f)
                {
                    IncrementStep();
                }
                break;
            case 1:
                RacketMouseMovement(mousePoint);

                // Track a point every 25 milliseconds
                if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                {
                    PuckPoints.Add(chosenObj.transform.position);
                }
                break;
            // After the user hits the object
            // Used to determine if the object hit by the tool is heading away from the target
            // Current distance from pinball to the target`
            case 2:
                RacketMouseMovement(mousePoint);
                float currentDistance = Vector3.Distance(chosenObj.transform.position, Target.transform.position);
                //Debug.Log("this is distance of puck from Target: " + currentDistance);

                // Only check when the distance from pinball to target is less than half of the distance
                // between the target and home position and if the pinball is NOT approaching the target
                if (currentDistance <= TARGET_DISTANCE / 2f &&
                    currentDistance > Vector3.Distance(previousPosition, Target.transform.position))
                {

                    // The object only has 500ms of total time to move away from the target
                    // After 500ms, the trial ends
                    if (missTimer < 0.5f)
                    {
                        missTimer += Time.fixedDeltaTime;
                    }
                    else
                    {
                        IncrementStep();
                    }
                }

                if (enteredTarget)
                {
                    // if distance increases from the previous frame, end trial immediately
                    float previousDistanceToTarget = Vector3.Distance(previousPosition, Target.transform.position);

                    // We are now going away from the target, end trial immediately
                    if (currentDistance > previousDistanceToTarget)
                    {
                        //lastPositionInTarget = previousPosition;
                        IncrementStep();
                        return;
                    }
                }

                // Trial ends if the ball stops moving OR
                // The distance between the home position and the pinball exceeds the distance
                // between the pinball and the target

                if (delayTimer > 0.1f)
                {
                    if (chosenObj.GetComponent<Rigidbody>().velocity.magnitude < 0.0001f ||
                        Vector3.Distance(chosenObj.transform.position, Home.transform.position) >=
                        InitialDistanceToTarget)
                    {
                        IncrementStep();
                    }

                }
                else
                {
                    delayTimer += Time.fixedDeltaTime;
                }

                // disbale tool object aft 50ms

                if (currentDistance < 0.05f)
                {
                    enteredTarget = true;
                }

                previousPosition = chosenObj.transform.position;
                break;
            // after we either hit the Target or passed by it
            case 3:
                if (timer == 0)
                {
                    //get Audio Component
                    toolSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["incorrect"];

                    float CurrentDistanceToTarget = Vector3.Distance(previousPosition, Target.transform.position);

                    if (CurrentDistanceToTarget < 0.05f)
                    {
                        if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                        {
                            toolSpace.GetComponent<LineRenderer>().startColor =
                                toolSpace.GetComponent<LineRenderer>().endColor =
                                    Target.GetComponent<BaseTarget>().Collided ? Color.green : Color.yellow;
                            Target.transform.GetChild(0).GetComponent<ParticleSystem>().Play();

                        }

                        toolSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["correct"];

                        // set pinball trail
                        toolSpace.GetComponent<LineRenderer>().positionCount = PuckPoints.Count;
                        toolSpace.GetComponent<LineRenderer>().SetPositions(PuckPoints.ToArray());

                        //Freeze puck
                        chosenObj.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                        chosenObj.GetComponent<Rigidbody>().isKinematic = true;

                    }
                }

                if (timer < 1.5f)
                {
                    timer += Time.deltaTime;

                    if (timer > 0.08f)
                    {
                        //freeze pinball in space
                        chosenObj.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                        chosenObj.GetComponent<Rigidbody>().isKinematic = true;

                        // set pinball trail
                        toolSpace.GetComponent<LineRenderer>().positionCount = PuckPoints.Count;
                        toolSpace.GetComponent<LineRenderer>().SetPositions(PuckPoints.ToArray());

                        if (enteredTarget)
                        {
                            chosenObj.transform.position = previousPosition;
                        }
                        else
                        {
                            chosenObj.transform.position = toolSpace.GetComponent<LineRenderer>().GetPosition(
                                toolSpace.GetComponent<LineRenderer>().positionCount - 1);
                        }
                    }
                    else if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback") &&
                             !enteredTarget)
                    {
                        // Add points to show feedback past the target only if they missed
                        // Points along the path are not added if they hit the target
                        PuckPoints.Add(chosenObj.transform.position);
                    }
                }
                else
                {
                    LogParameters();
                }
                break;
        }

        if (Finished)
            ctrler.EndAndPrepare();
    }

    public override bool IncrementStep()
    {
        if (currentStep == 0)
        {
            chosenObj.SetActive(true);
            //Home.GetComponent<BaseTarget>().enabled = false;
            //Home.GetComponent<MeshRenderer>().enabled = false;
        }

        return base.IncrementStep();
    }

    public override void Setup()
    {
        maxSteps = 4;
        ctrler = ExperimentController.Instance();

        toolSpace = Instantiate(ctrler.GetPrefab("ToolPrefab"));

        puckobj = GameObject.Find("PuckObject");
        ballObject = GameObject.Find("BallObject");
        Target = GameObject.Find("Target");
        toolCamera = GameObject.Find("ToolCamera");
        toolSurface = GameObject.Find("ToolPlane");
        grid = GameObject.Find("Grid");

        GameObject toolBox = GameObject.Find("ToolBox");
        GameObject toolSphere = GameObject.Find("ToolSphere");

        // Set up home position
        Home = GameObject.Find("HomePosition");

        // Set up target
        float targetAngle = ctrler.PollPseudorandomList("per_block_targetListToUse");

        Target.transform.position = new Vector3(0f, 0.08f, 0f);
        Target.transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        Target.transform.position += Target.transform.forward.normalized * TARGET_DISTANCE;

        // Set up camera for non VR and VR modes
        if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
        {
            oldMainCamera = GameObject.Find("Main Camera");
            oldMainCamera.SetActive(false);
        }
        else toolCamera.SetActive(false);

        //set up tool type
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_tool_type") == "Quad")
        {
            tool = toolBox;
            tool.GetComponent<BoxCollider>().material.bounciness = 1f;
            tool.GetComponent<BoxCollider>().enabled = false;
            toolSphere.SetActive(false);
        }
        else if (ctrler.Session.CurrentBlock.settings.GetString("per_block_tool_type") == "Sphere")
        {
            tool = toolSphere;
            tool.GetComponent<SphereCollider>().material.bounciness = 1f;
            tool.GetComponent<SphereCollider>().enabled = false;
            toolBox.SetActive(false);
        }

        // set up puck type 
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_puck_type") == "puck")
        {
            ballObject.SetActive(false);
            chosenObj = puckobj;
        }
        else if (ctrler.Session.CurrentBlock.settings.GetString("per_block_puck_type") == "ball")
        {
            puckobj.SetActive(false);
            chosenObj = ballObject;
        }

        InitialDistanceToTarget = Vector3.Distance(Target.transform.position, chosenObj.transform.position);
        InitialDistanceToTarget += 0.15f;

        // set up surface materials for the plane
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_surface_materials") == "fabric")
        {
            grid.SetActive(false);
            toolSurface.GetComponent<MeshRenderer>().material = ctrler.SurfaceMaterials[0];

        }
        else if (ctrler.Session.CurrentBlock.settings.GetString("per_block_surface_materials") == "ice")
        {
            grid.SetActive(false);
            toolSurface.GetComponent<MeshRenderer>().material = ctrler.SurfaceMaterials[1];
        }

        chosenObj.GetComponent<SphereCollider>().material.bounciness = 0.8f;

        // Disable object for first step
        chosenObj.SetActive(false);
    }

    public override void LogParameters()
    {
        ctrler.Session.CurrentTrial.result["tool_x"] = chosenObj.transform.localPosition.x;
        ctrler.Session.CurrentTrial.result["tool_y"] = chosenObj.transform.localPosition.y;
        ctrler.Session.CurrentTrial.result["tool_z"] = chosenObj.transform.localPosition.z;

        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.x;
        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.y;
        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.z;

        IncrementStep();

        base.LogParameters();
    }

    private void RacketMouseMovement(Vector3 mousePoint)
    {
        Vector3 dir = mousePoint - tool.transform.position;
        dir /= Time.fixedDeltaTime;
        tool.GetComponent<Rigidbody>().velocity = dir;

        tool.transform.LookAt(chosenObj.transform);

        tool.GetComponent<Collider>().enabled = mousePoint.z <= 0.05f;
            //+ ctrler.transform.position.z;
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
