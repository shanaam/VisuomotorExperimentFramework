using System.Collections.Generic;
using UnityEngine;
using UXF;
using MovementType = CursorController.MovementType;

public class ToolTask : BilliardsTask
{

    //TODO: 
    /// <summary>
    /// 
    /// DIFFREENT TYPES OF RACKETS ( sphere is not gpood a better Racket)
    /// 
    /// 3 types of shooting Styles:
    ///     Impact
    ///     Curling
    ///     slingShot
    /// 
    /// 
    /// 
    /// Tool sperates after shoot
    /// 
    /// 
    /// 
    /// </summary>

    private float InitialDistanceToTarget;
    private ExperimentController ctrler;

    private GameObject toolSpace;
    private GameObject tool;
    private GameObject toolCamera;
    private GameObject grid;

    private const float TARGET_DISTANCE = 0.55f;



    private GameObject impactBox;
    private GameObject puck;

    private GameObject curlingStone;
    private GameObject slingShotBall;

    private List<Vector3> PuckPoints = new List<Vector3>();
    private List<Vector3> CurlingStonePoints = new List<Vector3>();
    private GameObject oldMainCamera;

    private Vector3 previousPosition;
    private float missTimer;
    private float timer;
    private float delayTimer;
    private bool enteredTarget;

    //private bool CurlingShot = false;
    //private Vector3 

    private triggerType _triggerType;

    private enum triggerType
    {
        Impact,
        SlingShot,
        Curling
    }


    private void Update()
    {
        Debug.Log("current step :" + currentStep);


        Vector3 mousePoint = new Vector3();

        if (_triggerType == triggerType.Impact)
        {
            mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up,
                new Vector3(0f, impactBox.transform.position.y, 0f), toolCamera.GetComponent<Camera>());
            
            if (Vector3.Distance(mousePoint, impactBox.transform.position) > 0.05f && currentStep == 0) return;
        }

        if (_triggerType == triggerType.Curling)
        {
            mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, 
                new Vector3(0f, curlingStone.transform.position.y, 0f), toolCamera.GetComponent<Camera>());
           
            if (Vector3.Distance(mousePoint, curlingStone.transform.position) > 0.05f && currentStep == 0) return;
        }


        if(_triggerType == triggerType.SlingShot)
        {
            mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, 
                new Vector3(0f, slingShotBall.transform.position.y, 0f), toolCamera.GetComponent<Camera>());

            if (Vector3.Distance(mousePoint, curlingStone.transform.position) > 0.05f && currentStep == 0) return;
        }

        switch (currentStep)
        {
            // Return to home position phase
            case 0:

                if (_triggerType == triggerType.Impact)
                {

                    impactBox.GetComponent<Rigidbody>().velocity = Vector3.zero;

                    if (Vector3.Distance(mousePoint, impactBox.transform.position) <= 0.05f)
                    {
                        IncrementStep();
                    }
                }

                if(_triggerType == triggerType.Curling)
                {
                    curlingStone.GetComponent<Rigidbody>().velocity = Vector3.zero;

                    if(Vector3.Distance(mousePoint, curlingStone.transform.position) <= 0.05f)
                    {
                        IncrementStep();
                    }
                }

                if(_triggerType == triggerType.SlingShot)
                {
                    slingShotBall.GetComponent<Rigidbody>().velocity = Vector3.zero;


                    //Debug.Log("Distance in step 0: " + Vector3.Distance(mousePoint, slingShotBall.transform.position));

                    if (Vector3.Distance(mousePoint, slingShotBall.transform.position) <= 0.05f)
                    {
                        IncrementStep();
                    }
                }
                break;
            
            case 1:
               

                if (_triggerType == triggerType.Impact)
                {
                    Vector3 dir = mousePoint - impactBox.transform.position;
                    dir /= Time.fixedDeltaTime;
                    impactBox.GetComponent<Rigidbody>().velocity = dir;


                    // Rotate the impact: always looking at the puck when close enough 
                    if (Vector3.Distance(impactBox.transform.position, puck.transform.position) < 0.2f)
                    {
                        impactBox.transform.LookAt(puck.transform);
                    }
                    else
                    {
                        impactBox.transform.rotation = Quaternion.identity;
                    }

                    impactBox.GetComponent<Collider>().enabled = mousePoint.z <= 0.05f;
                }


                if (_triggerType == triggerType.Curling)
                {
                    Vector3 dir = mousePoint - curlingStone.transform.position;
                    dir /= Time.fixedDeltaTime;
                    curlingStone.GetComponent<Rigidbody>().velocity = dir;

                    Vector3 startPos = new Vector3();
                    Vector3 shotDir = new Vector3();

                    float time = 0f;



                    if(Vector3.Distance(curlingStone.transform.position , Home.transform.position) > 0.12f)
                    {
                        //Debug.Log("im here");

                        time += Time.fixedDeltaTime;
                        startPos = mousePoint;

                    }

                    if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.2f)
                    {
                        shotDir = startPos - mousePoint;
                        shotDir /= time;
                        curlingStone.GetComponent<Rigidbody>().AddForce(-shotDir);

                        IncrementStep();

                    }
                }

/*                if (_triggerType == triggerType.SlingShot)
                {
                    Vector3 dir = mousePoint - slingShotBall.transform.position;
                    dir /= Time.fixedDeltaTime;
                    slingShotBall.GetComponent<Rigidbody>().velocity = dir;

                    Vector3 startPos = new Vector3();
                    Vector3 shotDir = new Vector3();

                    float time = 0f;



                    if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.12f)
                    {
                        //Debug.Log("im here");

                        time += Time.fixedDeltaTime;
                        startPos = mousePoint;
                       
                    }

                    if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.2f)
                    {
                        shotDir = startPos - mousePoint;
                        shotDir /= time;
                        curlingStone.GetComponent<Rigidbody>().AddForce(-shotDir);

                        IncrementStep();

                    }
                }*/







                // Track a point every 25 milliseconds
                if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                {
                    if (_triggerType == triggerType.Impact)
                        PuckPoints.Add(puck.transform.position);


                    if (_triggerType == triggerType.Curling)
                        CurlingStonePoints.Add(curlingStone.transform.position);

                }
                break;
            // After the user hits the object
            // Used to determine if the object hit by the tool is heading away from the target
            // Current distance from pinball to the target`
            case 2:



                if (_triggerType == triggerType.Impact)
                {

                    //RacketMouseMovement(mousePoint);
                    Vector3 dir = mousePoint - impactBox.transform.position;
                    dir /= Time.fixedDeltaTime;
                    impactBox.GetComponent<Rigidbody>().velocity = dir;

                    // Rotate the impact: always looking at the puck when close enough 
                    if (Vector3.Distance(impactBox.transform.position, puck.transform.position) < 0.2f)
                    {
                        impactBox.transform.LookAt(puck.transform);
                    }
                    else
                    {
                        impactBox.transform.rotation = Quaternion.identity;
                    }

                    impactBox.GetComponent<Collider>().enabled = mousePoint.z <= 0.05f;



                    float currentDistance = Vector3.Distance(puck.transform.position, Target.transform.position);


                    //Debug.Log("this is distance of puck from Target: " + currentDistance);

                    // Only check when the distance from puck to target is less than half of the distance
                    // between the target and home position and if the puck is NOT approaching the target
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
                        if (puck.GetComponent<Rigidbody>().velocity.magnitude < 0.0001f ||
                            Vector3.Distance(puck.transform.position, Home.transform.position) >=
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

                    previousPosition = puck.transform.position;
                    break;
                }



                if (_triggerType == triggerType.Curling)
                {

                    // get the distance btween curling stone and Target
                    float currentDistance = Vector3.Distance(curlingStone.transform.position, Target.transform.position);


                    // Only check when the distance from curling stone to target is less than half of the distance
                    // between the target and home position and if the curlingStone is NOT approaching the target
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
                        if (curlingStone.GetComponent<Rigidbody>().velocity.magnitude < 0.0001f ||
                            Vector3.Distance(curlingStone.transform.position, Home.transform.position) >=
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


                    previousPosition = curlingStone.transform.position;
                    break;

                }




                break;


            // after we either hit the Target or passed by it
            case 3:

                if (_triggerType == triggerType.Impact)
                {

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
                            puck.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            puck.GetComponent<Rigidbody>().isKinematic = true;

                        }
                    }

                    if (timer < 1.5f)
                    {
                        timer += Time.deltaTime;

                        if (timer > 0.08f)
                        {
                            //freeze pinball in space
                            puck.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            puck.GetComponent<Rigidbody>().isKinematic = true;

                            // set pinball trail
                            toolSpace.GetComponent<LineRenderer>().positionCount = PuckPoints.Count;
                            toolSpace.GetComponent<LineRenderer>().SetPositions(PuckPoints.ToArray());

                            if (enteredTarget)
                            {
                                puck.transform.position = previousPosition;
                            }
                            else
                            {
                                puck.transform.position = toolSpace.GetComponent<LineRenderer>().GetPosition(
                                    toolSpace.GetComponent<LineRenderer>().positionCount - 1);
                            }
                        }
                        else if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback") &&
                                 !enteredTarget)
                        {
                            // Add points to show feedback past the target only if they missed
                            // Points along the path are not added if they hit the target
                            PuckPoints.Add(puck.transform.position);
                        }
                    }
                    else
                    {
                        LogParameters();
                    }
                    break;

                }

                if(_triggerType == triggerType.Curling)
                {


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
                            toolSpace.GetComponent<LineRenderer>().positionCount = CurlingStonePoints.Count;
                            toolSpace.GetComponent<LineRenderer>().SetPositions(CurlingStonePoints.ToArray());

                            //Freeze puck
                            curlingStone.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            curlingStone.GetComponent<Rigidbody>().isKinematic = true;

                        }

                    }




                    if (timer < 1.5f)
                    {
                        timer += Time.deltaTime;

                        if (timer > 0.08f)
                        {
                            //freeze pinball in space
                            curlingStone.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            curlingStone.GetComponent<Rigidbody>().isKinematic = true;

                            // set pinball trail
                            toolSpace.GetComponent<LineRenderer>().positionCount = CurlingStonePoints.Count;
                            toolSpace.GetComponent<LineRenderer>().SetPositions(CurlingStonePoints.ToArray());

                            if (enteredTarget)
                            {
                                curlingStone.transform.position = previousPosition;
                            }
                            else
                            {
                                curlingStone.transform.position = toolSpace.GetComponent<LineRenderer>().GetPosition(
                                    toolSpace.GetComponent<LineRenderer>().positionCount - 1);
                            }

                        }
                        else if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback") &&
                                   !enteredTarget)
                        {
                            // Add points to show feedback past the target only if they missed
                            // Points along the path are not added if they hit the target
                            CurlingStonePoints.Add(curlingStone.transform.position);
                        }


                    }
                    else
                    {
                        //LogParameters();
                    }



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
            if(_triggerType == triggerType.Impact)
                puck.SetActive(true);

            if (_triggerType == triggerType.Curling)
                curlingStone.SetActive(true);
        }

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

        impactBox = GameObject.Find("ToolBox");
        curlingStone = GameObject.Find("curlingStone");
        slingShotBall = GameObject.Find("slingShotBall");


        GameObject puckobj = GameObject.Find("PuckObject");
        GameObject ballObject = GameObject.Find("BallObject");

        // Set up home position
        Home = GameObject.Find("HomePosition");
        base.Setup();

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



        // setup for each Trigger type
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_triggerType") == "impact")
        {
            _triggerType = triggerType.Impact;

            //set up tool type
            //tool = impactBox
            impactBox.GetComponent<BoxCollider>().material.bounciness = 1f;
            impactBox.GetComponent<BoxCollider>().enabled = false;

            curlingStone.SetActive(false);
            slingShotBall.SetActive(false);

            // set up puck type 
            if (ctrler.Session.CurrentBlock.settings.GetString("per_block_puck_type") == "puck")
            {
                ballObject.SetActive(false);
                puck = puckobj;
            }
            else if (ctrler.Session.CurrentBlock.settings.GetString("per_block_puck_type") == "ball")
            {
                puckobj.SetActive(false);
                puck = ballObject;
            }

            puck.GetComponent<SphereCollider>().material.bounciness = 0.8f;

            InitialDistanceToTarget = Vector3.Distance(Target.transform.position, puck.transform.position);
            InitialDistanceToTarget += 0.15f;

            // Disable object for first step
            puck.SetActive(false);

        }
        
        else if (ctrler.Session.CurrentBlock.settings.GetString("per_block_triggerType") == "curling")
        {
            _triggerType = triggerType.Curling;

            //set up tool type
            //tool = curlingStone
            curlingStone.GetComponent<SphereCollider>().material.bounciness = 1f;
            curlingStone.GetComponent<SphereCollider>().enabled = false;

            curlingStone.transform.position = Home.transform.position;



            impactBox.SetActive(false);
            puckobj.SetActive(false);
            ballObject.SetActive(false);
            slingShotBall.SetActive(false);

            InitialDistanceToTarget = Vector3.Distance(Target.transform.position, curlingStone.transform.position);
            InitialDistanceToTarget += 0.15f;
        }
        
        else if (ctrler.Session.CurrentBlock.settings.GetString("per_block_triggerType") == "slingshot")
        {
            _triggerType = triggerType.SlingShot;



            impactBox.SetActive(false);
            puckobj.SetActive(false);
            ballObject.SetActive(false);
            curlingStone.SetActive(false);

            slingShotBall.transform.position = Home.transform.position;

            InitialDistanceToTarget = Vector3.Distance(Target.transform.position, slingShotBall.transform.position);
            InitialDistanceToTarget += 0.15f;


        }


        // set up surface materials for the plane
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_surface_materials") == "fabric")
        {
            grid.SetActive(false);
            base.SetSurfaceMaterial( ctrler.Materials["Felt"]);

        }
        else if (ctrler.Session.CurrentBlock.settings.GetString("per_block_surface_materials") == "ice")
        {
            grid.SetActive(false);
            base.SetSurfaceMaterial(ctrler.Materials["Ice"]);

        }


    }

    public override void LogParameters()
    {
        ctrler.Session.CurrentTrial.result["tool_x"] = puck.transform.localPosition.x;
        ctrler.Session.CurrentTrial.result["tool_y"] = puck.transform.localPosition.y;
        ctrler.Session.CurrentTrial.result["tool_z"] = puck.transform.localPosition.z;

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
        if (Vector3.Distance(tool.transform.position, puck.transform.position) < 0.2f)
        {
            tool.transform.LookAt(puck.transform);
        }
        else
        {
            tool.transform.rotation = Quaternion.identity;
        }
        tool.GetComponent<Collider>().enabled = mousePoint.z <= 0.05f;

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
    }*/
}
