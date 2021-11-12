using System;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using MovementType = CursorController.MovementType;

public class ToolTask : BilliardsTask
{

    protected float InitialDistanceToTarget;

    protected GameObject toolSpace;
    protected GameObject toolCamera;
    protected GameObject grid;

    private GameObject currentHand;

    // Tools 
    protected GameObject toolBox;
    protected GameObject toolSphere;
    protected GameObject toolCylinder;

    protected GameObject toolObjects; //parent object of each tool type

    protected GameObject baseObject; //physics object controlling position of each ball type
    protected GameObject ballObjects; //parent object of each ball type

    // Balls/puck
    protected GameObject ballobj;
    protected GameObject puckobj;
    protected GameObject curlingStone;
    protected GameObject slingShotBall;

    // lists for feedback points
    //protected List<Vector3> PuckPoints = new List<Vector3>();
    //private List<Vector3> CurlingStonePoints = new List<Vector3>();
    //private List<Vector3> slingShotPoints = new List<Vector3>();
    protected List<Vector3> ballPoints = new List<Vector3>();

    private GameObject oldMainCamera;

    private float missTimer;
    private float delayTimer;
    private bool enteredTarget;

    protected Vector3 mousePoint;

    private Vector3 previousPosition;

    private int score;
    private float tempScore;
    private const int MAX_POINTS = 10; // Maximum points the participant can earn
    private const int BONUS_POINTS = 5; // Bonus points earned if the participant lands a hit

    private float timer;

    private GameObject selectedObject;

    // Set to true if the user runs out of time 
    private bool missed;

    // Used to store the current distance between the ball and target
    private float distanceToTarget;

    protected const float FIRE_FORCE = 3f;

    protected virtual void Update()
    {
        mousePoint = GetMousePoint(baseObject.transform);

        if (Vector3.Distance(mousePoint, ballObjects.transform.position) > 0.05f && currentStep == 0) return;

        if (!trackScore) scoreboard.ManualScoreText = "Practice Round";

        switch (currentStep)
        {
            // initlize the scene 
            case 0:
                
                break;

            // the user triggers the object 
            case 1:
                // If the user runs out of time to fire the pinball, play audio cue
                if (!missed && timerIndicator.GetComponent<TimerIndicator>().Timer <= 0.0f)
                {
                    missed = true;
                    toolSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["incorrect"];
                    toolSpace.GetComponent<AudioSource>().Play();
                }

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
                distanceToTarget = Vector3.Distance(ballObjects.transform.position, Target.transform.position);

                // Update score if pinball is within 20cm of the target
                if (distanceToTarget < 0.20f)
                    tempScore = CalculateScore(distanceToTarget, 0.2f, MAX_POINTS);

                // Overwrite score only if its greater than the current score
                if (!missed & tempScore > score) score = (int)tempScore;
                if (trackScore) scoreboard.ManualScoreText = (ctrler.Score + score).ToString();

                // Only check when the distance from curling stone to target is less than half of the distance
                // between the target and home position and if the curlingStone is NOT approaching the target
                if (distanceToTarget <= TARGET_DISTANCE / 2f &&
                    distanceToTarget > Vector3.Distance(previousPosition, Target.transform.position))
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

                float previousDistanceToTarget = Vector3.Distance(previousPosition, Target.transform.position);

                if (enteredTarget)
                {
                    // if distance increases from the previous frame, end trial immediately
                    // We are now going away from the target, end trial immediately
                    if (distanceToTarget > previousDistanceToTarget)
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
                if (distanceToTarget < 0.05f)
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

                            if (!missed) Target.transform.GetChild(0).GetComponent<ParticleSystem>().Play();
                        }

                        toolSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["correct"];

                        // set pinball trail
                        toolSpace.GetComponent<LineRenderer>().positionCount = ballPoints.Count;
                        toolSpace.GetComponent<LineRenderer>().SetPositions(ballPoints.ToArray());

                        //Freeze puck
                        baseObject.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                        baseObject.GetComponent<Rigidbody>().isKinematic = true;

                    }

                    // If the participant fired the pinball within the allowed time & score tracking is enabled in json
                    if (!missed && timerIndicator.GetComponent<TimerIndicator>().Timer >= 0.0f)
                    {
                        toolSpace.GetComponent<AudioSource>().Play();

                        // Scoring. Note that running out of time yields no points
                        if (Target.GetComponent<BaseTarget>().Collided)
                        {
                            if (trackScore) ctrler.Score += MAX_POINTS + BONUS_POINTS;
                        }
                        else
                        {
                            if (trackScore) ctrler.Score += score;
                        }
                    }

                    if (trackScore)
                        scoreboard.ManualScoreText = ctrler.Score.ToString();

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
        switch (currentStep)
        {
            // 
            case 0:
                timerIndicator.BeginTimer();

                break;
            // User launched the ball/puck
            case 1:
                if (ctrler.Session.CurrentBlock.settings.GetBool("per_block_tilt_after_fire"))
                    SetTilt();

                timerIndicator.Cancel();

                baseObject.GetComponent<Rigidbody>().useGravity = true;

                // Add ball to tracked objects
                ctrler.AddTrackedObject("ball_path", baseObject);

                break;
        }

                Debug.Log("current step: " + currentStep);
        return base.IncrementStep();
    }

    public override void Setup()
    {
        maxSteps = 4;

        ctrler = ExperimentController.Instance();

        toolSpace = Instantiate(ctrler.GetPrefab("ToolPrefab"));

        base.Setup();

        Target = GameObject.Find("Target");
        toolCamera = GameObject.Find("ToolCamera");
        grid = GameObject.Find("Grid");
        
        curlingStone = GameObject.Find("curlingStone");
        slingShotBall = GameObject.Find("slingShotBall");
        puckobj = GameObject.Find("impactPuck");
        ballobj = GameObject.Find("impactBall");

        toolBox = GameObject.Find("paddle");
        toolCylinder = GameObject.Find("slingshot");
        toolSphere = GameObject.Find("squeegee");

        toolObjects = GameObject.Find("ToolObjects");
   
        baseObject = GameObject.Find("BaseObject");

        ballObjects = GameObject.Find("BallObjects");

        // Set up home position
        Home = GameObject.Find("HomePosition");
        
        timerIndicator.transform.rotation = Quaternion.LookRotation(
            timerIndicator.transform.position - toolCamera.transform.position);
        timerIndicator.Timer = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_timerTime");

        // Set up target
        float targetAngle = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_targetListToUse"));
        SetTargetPosition(targetAngle);

        // Should the tilt be shown to the participant before they release the pinball?
        if (!ctrler.Session.CurrentBlock.settings.GetBool("per_block_tilt_after_fire"))
            SetTilt();

        // Disable all balls/puck (to be enabled in child classes)
        puckobj.SetActive(false);
        curlingStone.SetActive(false);
        slingShotBall.SetActive(false);
        ballobj.SetActive(false);

        switch (ctrler.PollPseudorandomList("per_block_list_tool_type"))
        {
            case "paddle":
                toolCylinder.SetActive(false);
                toolSphere.SetActive(false);
                selectedObject = toolBox;

                break;
            case "squeegee":
                toolCylinder.SetActive(false);
                toolBox.SetActive(false);
                selectedObject = toolSphere;

                break;
            case "slingshot":
                toolSphere.SetActive(false);
                toolBox.SetActive(false);
                selectedObject = toolCylinder;

                break;
        }

        currentHand = ctrler.CursorController.CurrentHand();

        toolBox.GetComponent<Collider>().enabled = false;
        toolCylinder.GetComponent<Collider>().enabled = false;
        toolSphere.GetComponent<Collider>().enabled = false;

        baseObject.GetComponent<Rigidbody>().useGravity = false;

        baseObject.transform.position = Home.transform.position;

        baseObject.GetComponent<Rigidbody>().maxAngularVelocity = 240;

        //initial distance between target and ball
        InitialDistanceToTarget = Vector3.Distance(Target.transform.position, ballObjects.transform.position);
        InitialDistanceToTarget += 0.15f;


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
        // Note: ALL vectors are in world space

        ctrler.LogObjectPosition("tool", ballObjects.transform.position);
        ctrler.LogObjectPosition("target", Target.transform.position);

    }

    private void SetTilt()
    {
        SetTilt(toolCamera, toolSpace, cameraTilt);

        SetTilt(toolSpace, toolSpace, surfaceTilt);
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

    // method used to move the tool around based on mouse position
    protected virtual void ObjectFollowMouse(GameObject objFollower)
    {
        Vector3 dir = mousePoint - objFollower.transform.position;
        dir /= Time.fixedDeltaTime;
        objFollower.GetComponent<Rigidbody>().velocity = dir;


        switch (currentStep)
        {
            case 0:
                //objFollower.transform.position = mousePoint;
                //dir /= Time.fixedDeltaTime;
                //objFollower.GetComponent<Rigidbody>().velocity = dir;
                break;

            case 1:
                Debug.Log("here");
                switch (selectedObject.name)
                {
                    // the slingshot is placed at the home position and it rotates based on the direction the player is aiming at
                    case "slingshot":
                        objFollower.transform.position = Home.transform.position;
                        Vector3 direc = new Vector3(Home.transform.position.x - mousePoint.x, 0, Home.transform.position.z - mousePoint.z);
                        objFollower.transform.localRotation = Quaternion.LookRotation(direc);
                        break;

                    // squeegee rotates based on the direction that the player is moving towards
                    case "squeegee":
                        objFollower.transform.position = mousePoint;
                        dir = new Vector3(dir.x, 0, dir.z);
                        dir.Normalize();
                        objFollower.transform.localRotation = Quaternion.Slerp(objFollower.transform.localRotation, Quaternion.LookRotation(dir), Time.deltaTime * 10);
                        break;
                }     
                break;
            case 2:
                objFollower.transform.position = mousePoint;
                break;
            case 3:
                objFollower.transform.position = mousePoint;
                break;
        }

    }

    // moves the ball based on mouse position
    protected virtual void BallFollowMouse(GameObject objFollower)
    {
        objFollower.transform.position = mousePoint;
    }

    protected virtual void ToolLookAtBall()
    {
        // Rotate the tool: always looking at the ball when close enough 
        if (Vector3.Distance(toolObjects.transform.position, baseObject.transform.position) < 0.2f)
        {
            toolObjects.transform.LookAt(baseObject.transform, toolSpace.transform.up);
        }
        else
        {
            //toolObjects.transform.rotation = toolSpace.transform.rotation;
        }
    }    


    /*
      void OnDrawGizmos()
      {
          Vector3 mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, new Vector3(
              0f, tool.transform.position.y, 0f), toolCamera.GetComponent<Camera>());

          Gizmos.DrawLine(toolCamera.transform.position, mousePoint);
      }
  */

    void OnDrawGizmos()
    {
        /*
        if (currentStep >= 1)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lastPositionInTarget, 0.025f);
        }

        Vector3 dir = Vector3.zero;
        if (currentStep < 1)
        {
            Quaternion rot = Quaternion.AngleAxis(
                ctrler.Session.CurrentBlock.settings.GetFloat("per_block_tilt"), pinballSpace.transform.forward);

            mousePoint = ctrler.CursorController.MouseToPlanePoint(pinballPlane.transform.up * pinball.transform.position.y,
                pinball.transform.position,
                pinballCam.GetComponent<Camera>());

            dir = mousePoint - pinball.transform.position;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(pinball.transform.position, pinball.transform.position + dir * 5f);
        }

        // Represents the vector of where the mouse is pointing at in world space
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(pinballCam.transform.position, mousePoint);

        // Represents the direction of where the pinball is hit towards
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pinball.transform.position, pinball.transform.position + dir.normalized * 0.1f);

        // Represents the forward direction of the pinball
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pinball.transform.position, pinball.transform.position + pinball.transform.forward * 2f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(
            direction, 0.02f
            );
        */
        // Positions that are saved for data collection



        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(mousePoint, 0.02f);
    }


}
