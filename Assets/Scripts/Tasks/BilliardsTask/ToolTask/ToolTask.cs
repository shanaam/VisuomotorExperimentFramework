using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UXF;
using MovementType = CursorController.MovementType;

public class ToolTask : BilliardsTask
{

    protected float InitialDistanceToTarget;
    private Vector3 lastPositionNearTarget;

    protected GameObject toolSpace;
    protected GameObject toolCamera;
    protected GameObject grid;

    private GameObject currentHand;
    private GameObject handL, handR;
    private GameObject XRRig;
    private GameObject XRPosLock;

    // Tools 
    protected GameObject toolBox;
    protected GameObject toolSphere;
    protected GameObject toolCylinder;

    protected GameObject ray;
    protected GameObject barrier;

    protected GameObject toolObjects; //parent object of each tool type

    protected GameObject baseObject; //physics object controlling position of each ball type
    protected GameObject ballObjects; //parent object of each ball type

    // Balls/puck
    protected GameObject ballobj;
    protected GameObject puckobj;
    protected GameObject curlingStone;
    protected GameObject slingShotBall;

    protected LineRenderer elasticR;
    protected LineRenderer elasticL;

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
    protected AudioSource sound;

    private Vector3 previousPosition;

    private int score;
    private float tempScore;
    private const int MAX_POINTS = 10; // Maximum points the participant can earn
    private const int BONUS_POINTS = 5; // Bonus points earned if the participant lands a hit

    private float timer;

    protected GameObject selectedObject;

    // Set to true if the user runs out of time 
    private bool missed;

    // Used to store the current distance between the ball and target
    private float distanceToTarget;

    // Used to store launch angle to log
    protected float launchAngle;

    protected const float FIRE_FORCE = 4f;
    protected Vector3 ctrllerPoint;

    // Ball rolling sfx:
    private float pitchMin = 2;
    private float pitchMax = 3; // Max unity allows is 3 
    private float[] speed = new float[5];
    private int currIndex = 0;

    protected List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();

    protected Vector3 toolOffset = new Vector3();

    // For score
    private GameObject bonusText;

    private void FixedUpdate()
    {
        // Populate speed array
        speed[currIndex] = baseObject.GetComponent<Rigidbody>().velocity.magnitude;
        currIndex++;
        if (currIndex > speed.Length - 1)
            currIndex = 0;

        // Get average speed over the past /speed.Length/ physics updates
        float avgSpeed = 0;
        foreach (float s in speed)
        {
            avgSpeed += s;
        }
        avgSpeed /= speed.Length;

        baseObject.GetComponent<AudioSource>().volume = avgSpeed / 3;
        baseObject.GetComponent<AudioSource>().pitch = Mathf.Lerp(pitchMin, pitchMax, avgSpeed);
    }

    protected virtual void Update()
    {
        UnityEngine.XR.InputDevices.GetDevicesWithRole(UnityEngine.XR.InputDeviceRole.RightHanded, devices);

        //gets the mouse point relative to the surface
        mousePoint = GetMousePoint(baseObject.transform);
        ctrllerPoint = GetControllerPoint(baseObject.transform);
        //sets the rotation on the x and z axis of the tools to be 0 so the tool doesn't rotate, the weird rotation behaviour only happens with imapact tool
        toolObjects.transform.localEulerAngles = new Vector3(0, toolObjects.transform.localEulerAngles.y, 0);

        if (Vector3.Distance(mousePoint, ballObjects.transform.position) > 0.05f && currentStep == 0) return;

        // don't track score if practice
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

                // Every frame, we track the closest position the pinball has ever been to the target
                if (Vector3.Distance(lastPositionNearTarget, Target.transform.position) > distanceToTarget)
                    lastPositionNearTarget = ballObjects.transform.position;

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
                        lastPositionNearTarget = previousPosition;
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
                    // change score colour to white
                    bonusText.GetComponentInChildren<Text>().color = Color.white;

                    // if ball is within the target radius
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

                    bonusText.transform.position = ballObjects.transform.position + toolCamera.transform.up * 0.05f;
                    LeanTween.move(bonusText, bonusText.transform.position + (toolCamera.transform.up * 0.05f), 1.5f);

                    // If the participant fired the pinball within the allowed time & score tracking is enabled in json
                    if (!missed && timerIndicator.GetComponent<TimerIndicator>().Timer >= 0.0f)
                    {
                        toolSpace.GetComponent<AudioSource>().Play();

                        // Scoring. Note that running out of time yields no points
                        if (Target.GetComponent<BaseTarget>().Collided)
                        {
                            if (trackScore) ctrler.Score += MAX_POINTS + BONUS_POINTS;
                            bonusText.GetComponentInChildren<Text>().color = Color.green;

                            // Play bonus animation
                            bonusText.GetComponentInChildren<Text>().text = MAX_POINTS + " + " +
                                                                            BONUS_POINTS + "pts BONUS";
                        }
                        else
                        {
                            if (trackScore) ctrler.Score += score;
                            bonusText.GetComponentInChildren<Text>().text = score + "pts";
                            bonusText.GetComponentInChildren<Text>().color = score == 0 ? Color.red : Color.white;
                        }
                    }
                    else // missed
                    {
                        bonusText.GetComponentInChildren<Text>().text = "0pts";
                        bonusText.GetComponentInChildren<Text>().color = Color.red;
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
        handL = GameObject.Find("handL");
        handR = GameObject.Find("handR");
        XRRig = GameObject.Find("XR Rig");
        XRPosLock = GameObject.Find("XRPosLock");

        bonusText = GameObject.Find("BonusText");

        curlingStone = GameObject.Find("curlingStone");
        slingShotBall = GameObject.Find("slingShotBall");
        puckobj = GameObject.Find("impactPuck");
        ballobj = GameObject.Find("impactBall");
        ray = GameObject.Find("ray");
        barrier = GameObject.Find("barrier");

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
                sound = toolBox.GetComponentInChildren<AudioSource>();
                break;
            case "squeegee":
                Home.transform.position = new Vector3(Home.transform.position.x, Home.transform.position.y, -0.2f);
                toolCylinder.SetActive(false);
                toolBox.SetActive(false);
                selectedObject = toolSphere;
                sound = toolSphere.GetComponentInChildren<AudioSource>();
                break;
            case "slingshot":          
                toolSphere.SetActive(false);
                toolBox.SetActive(false);
                selectedObject = toolCylinder;
                sound = toolCylinder.GetComponentInChildren<AudioSource>();
                // sets the position of the elastics on the barrier
                elasticL = toolCylinder.transform.GetChild(4).gameObject.GetComponent<LineRenderer>();
                elasticR = toolCylinder.transform.GetChild(3).gameObject.GetComponent<LineRenderer>();
                elasticL.SetPosition(0, barrier.transform.GetChild(1).GetChild(0).gameObject.transform.position);
                elasticR.SetPosition(0, barrier.transform.GetChild(0).GetChild(0).gameObject.transform.position);
                break;
        }

        currentHand = ctrler.CursorController.CurrentHand();
        //currentHand.Equals("l") ? UnityEngine.XR.InputDeviceRole.LeftHanded : 
        

        toolBox.GetComponent<Collider>().enabled = false;
        toolCylinder.GetComponent<Collider>().enabled = false;
        toolSphere.GetComponent<Collider>().enabled = false;

        baseObject.GetComponent<Rigidbody>().useGravity = false;

        baseObject.transform.position = Home.transform.position;

        baseObject.GetComponent<Rigidbody>().maxAngularVelocity = 240;

        //initial distance between target and ball
        InitialDistanceToTarget = Vector3.Distance(Target.transform.position, ballObjects.transform.position);
        InitialDistanceToTarget += 0.15f;


        // Set up camera for non VR and VR modes and controller for vr mode
        if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
        {
            ctrler.CursorController.SetVRCamera(false);
        }
        else
        {
            toolCamera.SetActive(false);
            ctrler.CursorController.UseVR = true;
            ctrler.CursorController.SetCursorVisibility(false);

            if (ctrler.Session.CurrentBlock.settings.GetString("per_block_hand") == "l")
            {
                handL.SetActive(true);
            }
            else
            {
                handR.SetActive(true);
            }
        }

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

        // log the error
        // Error is the distance between the pinball and the target (meters)
        Vector2 lastPos = new Vector2(lastPositionNearTarget.x, lastPositionNearTarget.z);
        Vector2 targetPos = new Vector2(Target.transform.position.x, Target.transform.position.z);

        Vector2 dist = lastPos - targetPos; //To Fix: Cast to plane here instead to accound for tilts
        // Debug.Log("error: " + dist.magnitude.ToString("F5"));

        ctrler.Session.CurrentTrial.result["error_size"] = dist.magnitude;

        // Log the angle
        ctrler.Session.CurrentTrial.result["launch_angle"] = launchAngle;

    }

    /// <summary>
    /// Sets the tilt of the experiment
    /// </summary>
    private void SetTilt()
    {
        Vector3 ball_pos = Home.transform.position + Vector3.up * 0.25f;

        SetTilt(toolCamera, ball_pos, toolSpace, cameraTilt);
        //SetTilt(bonusText.transform.parent.gameObject, ball_pos, pinballSpace, cameraTilt);
        //SetTilt(pinballWall, ball_pos, pinballSpace, cameraTilt);

        SetTilt(toolSpace, ball_pos, toolSpace, surfaceTilt); //Tilt surface

        if (ctrler.Session.settings.GetString("experiment_mode") == "tool_vr") //Tilt VR Camera if needed
        {
            SetTilt(XRRig, ball_pos, toolSpace, cameraTilt + surfaceTilt);
            XRRig.transform.position = XRPosLock.transform.position; // lock position of XR Rig
        }
    }

    public override void Disable()
    {
        Vector3 ball_pos = Home.transform.position + Vector3.up * 0.25f;

        if (ctrler.Session.settings.GetString("experiment_mode") == "tool_vr") //Tilt VR Camera if needed
        {
            SetTilt(XRRig, ball_pos, toolSpace, (cameraTilt + surfaceTilt) * -1);
            XRRig.transform.position = XRPosLock.transform.position; // lock position of XR Rig
        }

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



    /// <summary>
    /// Method used to move the tool around based on mouse/vr controller position
    /// </summary>
    /// <param name="objFollower">the tool object that is going to follow the mouse/VR controller position</param>
    /// <param name="offset">TBH filled in</param>
    protected virtual void ObjectFollowMouse(GameObject objFollower, Vector3 offset)
    {
        Vector3 dir = mousePoint - objFollower.transform.position - offset;
        dir /= Time.fixedDeltaTime;
        objFollower.GetComponent<Rigidbody>().velocity = dir;   
    }

    /// <summary>
    /// Moves the ball based on mouse position
    /// </summary>
    /// <param name="objFollower">the ball object that is going to follow the mouse/VR controller position</param>
    /// <param name="offset">TBH filled in</param>
    protected virtual void BallFollowMouse(GameObject objFollower, Vector3 offset)
    {
        objFollower.transform.position = mousePoint - offset;
    }


    /// <summary>
    /// Makes the tool look at the direction of the ball no matter the position
    /// </summary>
    protected virtual void ToolLookAtBall()
    {
        // Rotate the tool: always looking at the ball when close enough 
        if (Vector3.Distance(toolObjects.transform.position, baseObject.transform.position) < 3f)
        {
            toolObjects.transform.LookAt(baseObject.transform, toolSpace.transform.up);
        }
        else
        {
            //toolObjects.transform.rotation = toolSpace.transform.rotation;
        }
    }

    /// <summary>
    /// Rotates the ball if it is stated in the json file
    /// </summary>
    /// <param name="shotDir">the direction of the shot.</param>
    protected virtual Vector3 RotateShot(Vector3 shotDir)
    {
        float angle = ctrler.Session.CurrentTrial.settings
                        .GetFloat("per_block_rotation");
        shotDir = Quaternion.Euler(0f, -angle, 0f) * shotDir;

        return shotDir;
    }


    /*
      void OnDrawGizmos()
      {
          Vector3 mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, new Vector3(
              0f, tool.transform.position.y, 0f), toolCamera.GetComponent<Camera>());

          Gizmos.DrawLine(toolCamera.transform.position, mousePoint);
      }
  */


    //protected virtual void ToolLookAtBall()
    //{
    //    // Rotate the tool: always looking at the ball when close enough 
    //    if (Vector3.Distance(toolObjects.transform.position, baseObject.transform.position) < 0.2f)
    //    {
    //        toolObjects.transform.LookAt(baseObject.transform, toolSpace.transform.up);
    //    }
    //    else
    //    {
    //        toolObjects.transform.rotation = toolSpace.transform.rotation;
    //    }
    //}    


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
