using System;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using MovementType = CursorController.MovementType;

public class ToolTask : BilliardsTask
{

    //TODO: 
    /// <summary>
    /// 
    ///  sphere is not good a better Racket)
    /// 
    /// 3 types of shooting Styles:
    ///     Impact:
    ///         DIFFREENT TYPES OF RACKETS
    //         
    ///     
    ///     slingShot
    ///         
    ///              
    ///
    /// 
    /// </summary>

    protected float InitialDistanceToTarget;
    protected ExperimentController ctrler;

    private GameObject toolSpace;
    private GameObject tool;
    private GameObject toolCamera;
    private GameObject grid;

    private const float TARGET_DISTANCE = 0.55f;

    protected GameObject impactBox;

    //protected GameObject puck;

    protected GameObject baseObject; //physics object controlling position of each ball type
    protected GameObject ballObjects; //parent object of each ball type

    protected GameObject puckobj;
    protected GameObject curlingStone;
    protected GameObject slingShotBall;

    // lists for feedback points
    //protected List<Vector3> PuckPoints = new List<Vector3>();
    //private List<Vector3> CurlingStonePoints = new List<Vector3>();
    //private List<Vector3> slingShotPoints = new List<Vector3>();
    protected List<Vector3> ballPoints = new List<Vector3>();

    private GameObject oldMainCamera;

    private Vector3 previousPosition;
    private float missTimer;
    private float timer;
    private float delayTimer;
    private bool enteredTarget;

    protected Vector3 mousePoint;


    protected virtual void Update()
    {
        

        mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up,
                new Vector3(0f, ballObjects.transform.position.y, 0f), toolCamera.GetComponent<Camera>());

        if (Vector3.Distance(mousePoint, ballObjects.transform.position) > 0.05f && currentStep == 0) return;

        switch (currentStep)
        {
            // initlize the scene 
            case 0:
                
                break;

            // the user triggers the object 
            case 1:
                //occurs in child classes

                break;

            // After the user hits the object
            // Used to determine if the triggerd object is heading away from the target or not
            case 2:

                // Track a points for feedback trail 
                if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                {
                    ballPoints.Add(ballObjects.transform.position);
                }

                // get the distance btween ball/puck and Target
                float currentDistance = Vector3.Distance(ballObjects.transform.position, Target.transform.position);

                // Only check when the distance from curling stone to target is less than half of the distance
                // between the target and home position and if the curlingStone is NOT approaching the target
                if (currentDistance <= TARGET_DISTANCE / 2f &&
                    currentDistance > Vector3.Distance(previousPosition, Target.transform.position))
                {

                    // The object only has 500ms of total time to move away from the target
                    // After 500ms, the trial ends
                    if (missTimer < 0.5f)
                    {
                        missTimer += Time.deltaTime;
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
                    if (baseObject.GetComponent<Rigidbody>().velocity.magnitude < 0.0001f ||
                        Vector3.Distance(ballObjects.transform.position, Home.transform.position) >=
                        InitialDistanceToTarget)
                    {
                        IncrementStep();
                    }

                }
                else
                {
                    delayTimer += Time.deltaTime;
                }

                // disbale tool object aft 50ms

                if (currentDistance < 0.05f)
                {
                    enteredTarget = true;
                }
                previousPosition = ballObjects.transform.position;

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
                        toolSpace.GetComponent<LineRenderer>().positionCount = ballPoints.Count;
                        toolSpace.GetComponent<LineRenderer>().SetPositions(ballPoints.ToArray());

                        //Freeze puck
                        baseObject.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                        baseObject.GetComponent<Rigidbody>().isKinematic = true;

                    }

                }

                if (timer < 1.5f)
                {
                    timer += Time.deltaTime;

                    if (timer > 0.08f)
                    {
                        //freeze curling stone in space
                        baseObject.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                        baseObject.GetComponent<Rigidbody>().isKinematic = true;

                        // set curling stone trail
                        toolSpace.GetComponent<LineRenderer>().positionCount = ballPoints.Count;
                        toolSpace.GetComponent<LineRenderer>().SetPositions(ballPoints.ToArray());

                        if (enteredTarget)
                        {
                            ballObjects.transform.position = previousPosition;
                        }
                        else
                        {
                            ballObjects.transform.position = toolSpace.GetComponent<LineRenderer>().GetPosition(
                                toolSpace.GetComponent<LineRenderer>().positionCount - 1);
                        }

                    }
                    else if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                    {
                        ballPoints.Add(ballObjects.transform.position);
                    }


                }
                else
                {
                    LogParameters();
                    IncrementStep();
                }

                break;
        }


        if (Finished)
            ctrler.EndAndPrepare();
    }

    public override bool IncrementStep()
    {
        Debug.Log("current step: " + currentStep);
        return base.IncrementStep();
    }

    public override void Setup()
    {
        maxSteps = 4;
        ctrler = ExperimentController.Instance();
        toolSpace = Instantiate(ctrler.GetPrefab("ToolPrefab"));
        Target = GameObject.Find("Target");
        toolCamera = GameObject.Find("ToolCamera");
        grid = GameObject.Find("Grid");
        
        curlingStone = GameObject.Find("curlingStone");
        slingShotBall = GameObject.Find("slingShotBall");

        impactBox = GameObject.Find("ToolBox");

        puckobj = GameObject.Find("PuckObject");
        baseObject = GameObject.Find("BaseObject");

        ballObjects = GameObject.Find("BallObjects");

        // Set up home position
        Home = GameObject.Find("HomePosition");
        base.Setup();

        // Set up target
        float targetAngle = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_targetListToUse"));
        


        //// cursur represnation type
        //string tool_type = Convert.ToString(ctrler.PollPseudorandomList("per_block_list_tool_type"));

        Target.transform.position = new Vector3(0f, 0.08f, 0f);
        Target.transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        Target.transform.position += Target.transform.forward.normalized * TARGET_DISTANCE;

        // Set up camera for non VR and VR modes
        // VR Mode needs to be added
        if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
        {
            ctrler.CursorController.SetVRCamera(false);
        }
        else toolCamera.SetActive(false);

        


        // set up surface materials for the plane
        switch (ctrler.Session.CurrentBlock.settings.GetString("per_block_surface_materials"))
        {
            case "fabric":
                grid.SetActive(false);
                base.SetSurfaceMaterial(ctrler.Materials["GrassMaterial"]);
                break;

            case "ice":
                grid.SetActive(false);
                base.SetSurfaceMaterial(ctrler.Materials["Ice"]);
                break;
        }
    }

    public override void LogParameters()
    {
        ctrler.LogObjectPosition("tool", ballObjects.transform.localPosition);
        ctrler.LogObjectPosition("target", Target.transform.localPosition);
    }

 /*   private void RacketMouseMovement(Vector3 mousePoint)
    {
        Vector3 dir = mousePoint - tool.transform.position;
        dir /= Time.fixedDeltaTime;

        tool.GetComponent<Rigidbody>().velocity = dir;
        if (Vector3.Distance(tool.transform.position, puck.transform.position) < 0.2f)
        {
            tool.transform.LookAt(puck.transform);
        }
        else
        {
            tool.transform.rotation = Quaternion.identity;
        }
        tool.GetComponent<Collider>().enabled = mousePoint.z <= 0.05f;

    }*/

    public override void Disable()
    {
        toolSpace.SetActive(false);

        // Enabling this will cause screen flicker. Only use if task involves both Non-VR and VR in the same experiment
        //ctrler.CursorController.SetVRCamera(true);
    }

    protected override void OnDestroy()
    {
        Destroy(toolSpace);

        if (ctrler.Session.settings.GetString("experiment_mode") == "tool" && oldMainCamera != null)
            oldMainCamera.SetActive(true);
    }
    
    
    
  /*
    void OnDrawGizmos()
    {
        Vector3 mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, new Vector3(
            0f, tool.transform.position.y, 0f), toolCamera.GetComponent<Camera>());

        Gizmos.DrawLine(toolCamera.transform.position, mousePoint);
    }
*/



}
