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
    /// DIFFREENT TYPES OF RACKETS ( sphere is not good a better Racket)
    /// 
    /// 3 types of shooting Styles:
    ///     Impact
    ///     Curling --> track the points after the release point
    ///     
    ///     slingShot
    ///         rubber band should just one line fromt he center of the sling shot ball
    ///         make the slingshot ball same size and the impact ball
    ///         
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
    private List<Vector3> slingShotPoints = new List<Vector3>();

    private GameObject oldMainCamera;

    private Vector3 previousPosition;
    private float missTimer;
    private float timer;
    private float delayTimer;
    private bool enteredTarget;

    private string puck_type;

    private TriggerType _triggerType;

    private enum TriggerType
    {
        Impact,
        SlingShot,
        Curling
    }


    private void Update()
    {
        Debug.Log("current step: " + currentStep);

        Vector3 mousePoint = new Vector3();

        if (_triggerType == TriggerType.Impact)
        {
            mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up,
                new Vector3(0f, impactBox.transform.position.y, 0f), toolCamera.GetComponent<Camera>());
            
            if (Vector3.Distance(mousePoint, impactBox.transform.position) > 0.05f && currentStep == 0) return;
        }

        if (_triggerType == TriggerType.Curling)
        {
            mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, 
                new Vector3(0f, curlingStone.transform.position.y, 0f), toolCamera.GetComponent<Camera>());
           
            if (Vector3.Distance(mousePoint, curlingStone.transform.position) > 0.05f && currentStep == 0) return;
        }

        if(_triggerType == TriggerType.SlingShot)
        {
            mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, 
                new Vector3(0f, slingShotBall.transform.position.y, 0f), toolCamera.GetComponent<Camera>());

            if (Vector3.Distance(mousePoint, slingShotBall.transform.position) > 0.05f && currentStep == 0) return;
        }

        switch (currentStep)
        {
            // Return to home position phase
            case 0:

                if (_triggerType == TriggerType.Impact)
                {

                    impactBox.GetComponent<Rigidbody>().velocity = Vector3.zero;

                    if (Vector3.Distance(mousePoint, impactBox.transform.position) <= 0.05f)
                    {
                        IncrementStep();
                    }
                }

                if(_triggerType == TriggerType.Curling)
                {
                    curlingStone.GetComponent<Rigidbody>().velocity = Vector3.zero;

                    if(Vector3.Distance(mousePoint, curlingStone.transform.position) <= 0.05f)
                    {
                        IncrementStep();
                    }
                }

                if(_triggerType == TriggerType.SlingShot)
                {
                    slingShotBall.GetComponent<Rigidbody>().velocity = Vector3.zero;

                    if (Vector3.Distance(mousePoint, slingShotBall.transform.position) <= 0.05f)
                    {
                        IncrementStep();
                    }
                }
                
                break;

            // the user triggers the opbject
            case 1:
               
                if (_triggerType == TriggerType.Impact)
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

                if (_triggerType == TriggerType.Curling)
                {
                    Vector3 dir = mousePoint - curlingStone.transform.position;
                    dir /= Time.fixedDeltaTime;
                    curlingStone.GetComponent<Rigidbody>().velocity = dir;

                    Vector3 startPos = new Vector3();
                    Vector3 shotDir = new Vector3();

                    float time = 0f;



                    if(Vector3.Distance(curlingStone.transform.position , Home.transform.position) > 0.12f)
                    {


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
                
                if (_triggerType == TriggerType.SlingShot)
                {
                    Vector3 dir = mousePoint - slingShotBall.transform.position;
                    dir /= Time.fixedDeltaTime;
                    slingShotBall.GetComponent<Rigidbody>().velocity = dir;

                    float time = 0f;

                    // Lien rendere representing the slingshot band is attached to home GameObject
                    Home.GetComponent<LineRenderer>().positionCount = 2;
                    Home.GetComponent<LineRenderer>().SetPosition(0, Home.transform.position);
                    Home.GetComponent<LineRenderer>().SetPosition(1, mousePoint);
                    

                    if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.12f)
                    {
                        time += Time.fixedDeltaTime;
                    }

                    if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.25f)
                    {
                        Vector3 shotDir = Home.transform.position - mousePoint;
                        shotDir /= time;

                        slingShotBall.GetComponent<Rigidbody>().velocity = shotDir * 0.1f;
                        Home.GetComponent<LineRenderer>().positionCount = 0;

                        IncrementStep();
                    }
                }

                // Track a points for feedback trail 
                if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                {
                    if (_triggerType == TriggerType.Impact)
                        PuckPoints.Add(puck.transform.position);
                }
                
                break;

            // After the user hits the object
            // Used to determine if the object hit by the object is heading away from the target
            case 2:

                // Track a points for feedback trail 
                if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                {
                    if (_triggerType == TriggerType.Curling)
                        CurlingStonePoints.Add(curlingStone.transform.position);


                    if (_triggerType == TriggerType.SlingShot)
                        slingShotPoints.Add(slingShotBall.transform.position);
                }

                if (_triggerType == TriggerType.Impact)
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

                if (_triggerType == TriggerType.Curling)
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

                if (_triggerType == TriggerType.SlingShot)
                {

                    // get the distance btween slingShotBall stone and Target
                    float currentDistance = Vector3.Distance(slingShotBall.transform.position, Target.transform.position);


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
                        if (slingShotBall.GetComponent<Rigidbody>().velocity.magnitude < 0.0001f ||
                            Vector3.Distance(slingShotBall.transform.position, Home.transform.position) >=
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


                    previousPosition = slingShotBall.transform.position;

                    break;


                }

                break;

            // after we either hit the Target or passed by it
            case 3:

                if(_triggerType == TriggerType.Impact)
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

                            // set puck trail
                            toolSpace.GetComponent<LineRenderer>().positionCount = PuckPoints.Count;
                            toolSpace.GetComponent<LineRenderer>().SetPositions(PuckPoints.ToArray());

                            //Freeze puck
                            puck.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            puck.GetComponent<Rigidbody>().isKinematic = true;

                            //freeze impact box 
                            impactBox.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            impactBox.GetComponent<Rigidbody>().isKinematic = true;


                        }
                    }

                    if (timer < 1.5f)
                    {
                        timer += Time.deltaTime;

                        if (timer > 0.08f)
                        {
                            //freeze puck in space
                            puck.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            puck.GetComponent<Rigidbody>().isKinematic = true;

                            //freeze impact box 
                            impactBox.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            impactBox.GetComponent<Rigidbody>().isKinematic = true;

                            // set puck trail
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
                        else if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                        {
                            PuckPoints.Add(puck.transform.position);
                        }
                    }
                    else
                    {
                        LogParameters();
                        IncrementStep();
                    }
                    

                }

                if(_triggerType == TriggerType.Curling)
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
                            //freeze curling stone in space
                            curlingStone.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            curlingStone.GetComponent<Rigidbody>().isKinematic = true;

                            // set curling stone trail
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
                        else if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                        {
                            CurlingStonePoints.Add(curlingStone.transform.position);
                        }


                    }
                    else
                    {
                        LogParameters();
                        IncrementStep();
                    }



                }

                if(_triggerType == TriggerType.SlingShot)
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

                            //set slingshot trail
                            toolSpace.GetComponent<LineRenderer>().positionCount = slingShotPoints.Count;
                            toolSpace.GetComponent<LineRenderer>().SetPositions(slingShotPoints.ToArray());

                            //Freeze slingshot
                            slingShotBall.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            slingShotBall.GetComponent<Rigidbody>().isKinematic = true;

                        }

                    }


                    if (timer < 1.5f)
                    {
                        timer += Time.deltaTime;

                        if (timer > 0.08f)
                        {
                            //freeze slingShot in space
                            slingShotBall.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                            slingShotBall.GetComponent<Rigidbody>().isKinematic = true;

                            // set trail
                            toolSpace.GetComponent<LineRenderer>().positionCount = slingShotPoints.Count;
                            toolSpace.GetComponent<LineRenderer>().SetPositions(slingShotPoints.ToArray());

                            if (enteredTarget)
                            {
                                slingShotBall.transform.position = previousPosition;
                            }
                            else
                            {
                                slingShotBall.transform.position = toolSpace.GetComponent<LineRenderer>().GetPosition(
                                    toolSpace.GetComponent<LineRenderer>().positionCount - 1);
                            }

                        }
                        else if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                        {
                            slingShotPoints.Add(slingShotBall.transform.position);
                        }


                    }
                    else
                    {
                        LogParameters();
                        IncrementStep();
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
            if(_triggerType == TriggerType.Impact)
                puck.SetActive(true);

            if (_triggerType == TriggerType.Curling)
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
        float targetAngle = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_targetListToUse"));
        puck_type = Convert.ToString(ctrler.PollPseudorandomList("per_block_list_puck_type"));

        switch (Convert.ToString(ctrler.PollPseudorandomList("per_block_list_triggerType"))) {

            case "impact":
                _triggerType = TriggerType.Impact;
                break;
            case "slingShot":
                _triggerType = TriggerType.SlingShot;
                break;
            case "curling":
                _triggerType = TriggerType.Curling;
                break;
        }

       


        Debug.Log("puck Type" + puck_type);

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


        switch (_triggerType)
        {
            case TriggerType.Impact:
                //set up tool type
                //tool = impactBox
                impactBox.GetComponent<BoxCollider>().material.bounciness = 1f;
                impactBox.GetComponent<BoxCollider>().enabled = false;

                curlingStone.SetActive(false);
                slingShotBall.SetActive(false);

                // set up puck type 
                if (puck_type == "puck")
                {
                    ballObject.SetActive(false);
                    puck = puckobj;
                }
                else if (puck_type == "ball")
                {
                    puckobj.SetActive(false);
                    puck = ballObject;
                }

                puck.GetComponent<SphereCollider>().material.bounciness = 0.8f;

                InitialDistanceToTarget = Vector3.Distance(Target.transform.position, puck.transform.position);
                InitialDistanceToTarget += 0.15f;

                // Disable object for first step
                puck.SetActive(false);

                break;

            case TriggerType.Curling:
                _triggerType = TriggerType.Curling;

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
                break;

            case TriggerType.SlingShot:
                _triggerType = TriggerType.SlingShot;



                impactBox.SetActive(false);
                puckobj.SetActive(false);
                ballObject.SetActive(false);
                curlingStone.SetActive(false);

                slingShotBall.transform.position = Home.transform.position;

                InitialDistanceToTarget = Vector3.Distance(Target.transform.position, slingShotBall.transform.position);
                InitialDistanceToTarget += 0.15f;
                break;
        }

        


        // set up surface materials for the plane
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_surface_materials") == "fabric")
        {
            grid.SetActive(false);
            base.SetSurfaceMaterial( ctrler.Materials["GrassMaterial"]);

        }
        else if (ctrler.Session.CurrentBlock.settings.GetString("per_block_surface_materials") == "ice")
        {
            grid.SetActive(false);
            base.SetSurfaceMaterial(ctrler.Materials["Ice"]);

        }


    }

    public override void LogParameters()
    {
        GameObject other = null;

        switch (_triggerType)
        {
            case TriggerType.Impact:
                other = puck;
                break;
            case TriggerType.Curling:
                other = curlingStone;
                break;
            case TriggerType.SlingShot:
                other = slingShotBall;
                break; 
            default:
                Debug.LogError("Trigger type not implemented. Tool object will be null");
                break;
        }

        ctrler.LogObjectPosition("tool", other.transform.localPosition);
        ctrler.LogObjectPosition("target", Target.transform.localPosition);
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
    }*/
}
