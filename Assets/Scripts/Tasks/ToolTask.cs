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

    protected GameObject toolSpace;
    protected GameObject toolCamera;
    protected GameObject grid;

    private const float TARGET_DISTANCE = 0.55f;

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

    private TimerIndicator timerIndicator;
    private Scoreboard scoreboard;

    private bool trackScore;
    private int score;
    private float tempScore;

    private bool missed;

    private float cameraTilt, surfaceTilt;

    private const int MAX_POINTS = 10; // Maximum points the participant can earn
    private const int BONUS_POINTS = 5; // Bonus points earned if the participant lands a hit

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
        mousePoint = ctrler.CursorController.MouseToPlanePoint(
            toolSpace.transform.up * baseObject.transform.position.y,
            baseObject.transform.position,
            toolCamera.GetComponent<Camera>());

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
                float currentDistance = Vector3.Distance(ballObjects.transform.position, Target.transform.position);

                // Update score if pinball is within 20cm of the target
                if (currentDistance < 0.20f)
                    tempScore = Mathf.Round(-5f * (currentDistance - 0.20f) * MAX_POINTS);

                // Overwrite score only if its greater than the current score
                if (tempScore > score) score = (int)tempScore;
                if (trackScore) scoreboard.ManualScoreText = (ctrler.Score + score).ToString();

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

                float previousDistanceToTarget = Vector3.Distance(previousPosition, Target.transform.position);

                if (enteredTarget)
                {
                    // if distance increases from the previous frame, end trial immediately
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
        Target = GameObject.Find("Target");
        toolCamera = GameObject.Find("ToolCamera");
        grid = GameObject.Find("Grid");
        
        curlingStone = GameObject.Find("curlingStone");
        slingShotBall = GameObject.Find("slingShotBall");

        toolBox = GameObject.Find("ToolBox");
        toolCylinder = GameObject.Find("ToolCylinder");
        toolSphere = GameObject.Find("ToolSphere");

        toolObjects = GameObject.Find("ToolObjects");

        puckobj = GameObject.Find("impactPuck");
        ballobj = GameObject.Find("impactBall");
        baseObject = GameObject.Find("BaseObject");

        ballObjects = GameObject.Find("BallObjects");

        // Set up home position
        Home = GameObject.Find("HomePosition");
        base.Setup();

        timerIndicator = GameObject.Find("TimerIndicator").GetComponent<TimerIndicator>();
        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();
        
        timerIndicator.transform.rotation = Quaternion.LookRotation(
            timerIndicator.transform.position - toolCamera.transform.position);
        timerIndicator.Timer = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_timerTime");

        // Scoreboard is now updated by the tool class
        scoreboard.AllowManualSet = true;

        // Set up target
        float targetAngle = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_targetListToUse"));

        Target.transform.position = new Vector3(0f, 0.08f, 0f);
        Target.transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        Target.transform.position += Target.transform.forward.normalized * TARGET_DISTANCE;

        cameraTilt = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_list_camera_tilt"));
        surfaceTilt = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_list_surface_tilt"));
        cameraTilt -= surfaceTilt; // As surfaceTilt rotates the entire prefab, this line makes creating the json more intuitive 

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
            case "quad":
                toolCylinder.SetActive(false);
                toolSphere.SetActive(false);

                break;
            case "sphere":
                toolCylinder.SetActive(false);
                toolBox.SetActive(false);

                break;
            case "cylinder":
                toolSphere.SetActive(false);
                toolBox.SetActive(false);

                break;
        }

        toolBox.GetComponent<Collider>().enabled = false;
        toolCylinder.GetComponent<Collider>().enabled = false;
        toolSphere.GetComponent<Collider>().enabled = false;

        baseObject.GetComponent<Rigidbody>().useGravity = false;

        // Whether or not this is a practice trial 
        // replaces scoreboard with 'Practice Round', doesn't record score
        trackScore = (ctrler.Session.CurrentBlock.settings.GetBool("per_block_track_score"));

        if (!trackScore)
        {
            scoreboard.ScorePrefix = false;
            scoreboard.ManualScoreText = "Practice Round";
        }

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
