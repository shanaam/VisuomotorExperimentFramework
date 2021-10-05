using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CommonUsages = UnityEngine.XR.CommonUsages;

public class PinballTask : BilliardsTask
{
    // Task GameObjects
    private GameObject pinballSpace;
    private GameObject pinballCam;
    private GameObject pinball;
    private GameObject directionIndicator;
    private ArcScript arcIndicator;
    private GameObject XRRig;
    private GameObject pinballWall;
    private GameObject pinballSurface;
    private GameObject XRPosLock;
    private GameObject XRCamOffset;

    private GameObject obstacle;

    private GameObject handL, handR;

    // Used for pinball aiming
    private Vector3 direction;

    private float cutoffDistance;

    // True when the participant is holding the trigger down to aim the pinball
    private bool aiming;

    private GameObject currentHand;

    // Used to draw the path of the pinball for feedback mode
    private List<Vector3> pinballPoints = new List<Vector3>();

    // Used to determine if the ball moved away from the target for too long
    private float missTimer;

    private Vector3 pinballStartPosition;

    // Pinball Camera Offset
    Vector3 pinballCamOffset = new Vector3(0f, 0.725f, -0.535f);
    private float pinballAngle = 35f;

    private Vector3 lastPositionInTarget;

    // True when the pinball enters the target circle for the first time
    private bool enteredTarget;

    // When true, the indicator will be placed in front of the pinball
    private bool indicatorPosition = true;

    // Distance from pinball in meters the indicator will be shown
    private float indicatorLength = 0.2f;

    private float trialTimer;
    private const float MAX_TRIAL_TIME = 2.0f;

    private const float PINBALL_FIRE_FORCE = 15f;

    // Plane that is parallel to the environment plane
    private Plane pPlane;

    // A position above the visual target that is aligned with the height of the pinball regardless
    // of plane tilt
    private Vector3 pinballAlignedTargetPosition;

    private GameObject bonusText;

    private Vector3 previousPosition;

    private int score;
    private float tempScore;
    private const int MAX_POINTS = 10; // Maximum points the participant can earn
    private const int BONUS_POINTS = 5; // Bonus points earned if the participant lands a hit

    private float timer;

    // Set to true if the user runs out of time 
    private bool missed;

    // Used to store the current distance t
    private float distanceToTarget;

    // if in flick launch mode, the time the user starts the flick
    private float flickStartTime, flickEndTime;
    private Vector3 flickStartPos;
    private bool flickStarted = false;
    // The max distance before a VR flick automatically ends
    private float flickCutoff = 0.12f;
    private const float VR_FLICK_FIRE_FORCE = 2;

    private Vector3 initialVelocity;

    private float timeBallTrackingStarts, timeHandTrackingStarts;


    void FixedUpdate()
    {
        // While the pinball is in motion
        if (currentStep == 1)
        {
            // Current distance from pinball to the target
            distanceToTarget = Vector3.Distance(pinball.transform.position, pinballAlignedTargetPosition);

            // Every frame, we track the closest position the pinball has ever been to the target
            if (Vector3.Distance(lastPositionInTarget, pinballAlignedTargetPosition) > distanceToTarget)
                lastPositionInTarget = pinball.transform.position;

            // Update score if pinball is within 20cm of the target
            if (distanceToTarget < 0.20f)
                tempScore = CalculateScore(distanceToTarget, .2f, MAX_POINTS);

            // Overwrite score only if its greater than the current score
            if (!missed && tempScore > score) score = (int)tempScore;
            if (trackScore) scoreboard.ManualScoreText = (ctrler.Score + score).ToString();

            // Only check when the distance from pinball to target is less than half of the distance
            // between the target and home position and if the pinball is NOT approaching the target
            if (distanceToTarget <= TARGET_DISTANCE / 2f &&
                distanceToTarget > Vector3.Distance(previousPosition, pinballAlignedTargetPosition))
            {
                // The pinball only has 500ms of total time to move away from the target
                // After 500ms, the trial ends
                if (missTimer < 0.5f)
                {
                    missTimer += Time.fixedDeltaTime;
                }
                else
                {
                    Debug.Log("Trial Ended: Ball traveled away from target for too long");
                    IncrementStep();
                    return;
                }
            }

            // Timer such that the pinball has a max travel time before forcing the trial to end
            if (trialTimer <= MAX_TRIAL_TIME)
            {
                trialTimer += Time.fixedDeltaTime;
            }
            else
            {
                Debug.Log("Trial Ended: Max trial time exceeded");
                IncrementStep();
                return;
            }

            if (enteredTarget)
            {
                // if distance increases from the previous frame, end trial immediately
                float previousDistanceToTarget = Vector3.Distance(previousPosition, pinballAlignedTargetPosition);

                // We are now going away from the target, end trial immediately
                if (distanceToTarget > previousDistanceToTarget)
                {
                    Debug.Log("Trial Ended: Ball is exiting the target radius");
                    lastPositionInTarget = previousPosition;
                    IncrementStep();
                    return;
                }
            }

            // Trial ends if the ball stops moving OR
            // The distance between the home position and the pinball exceeds the distance
            // between the pinball and the target
            if (pinball.GetComponent<Rigidbody>().velocity.magnitude <= 0.0001f ||
                Vector3.Distance(pinball.transform.position, Home.transform.position) >= cutoffDistance)
            {
                Debug.Log("Trial Ended: Ball has stopped moving or ball has exceeded the cutoff distance");
                //lastPositionInTarget = pinball.transform.position;
                IncrementStep();
                return;
            }

            if (distanceToTarget < 0.05f)
            {
                // Set a temp variable to the pinball's position
                lastPositionInTarget = pinball.transform.position;
                enteredTarget = true;
            }

            previousPosition = pinball.transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();

        //make sure that this is still centered on the exp controller
        switch (currentStep)
        {
            case 0:
                if (!trackScore) scoreboard.ManualScoreText = "Practice Round";

                // If non-vr use mouse inputs, otherwise use the controller as input
                if (ctrler.Session.settings.GetString("experiment_mode") == "pinball")
                {
                    if (Input.GetMouseButton(0))
                    {
                        if (ctrler.Session.CurrentTrial.settings.GetString("per_block_fire_mode") == "flick")
                        {
                            Vector3 mouse = GetMouseScreenPercentage();

                            if (!flickStarted)
                            {
                                flickStartTime = Time.time;
                                flickStartPos = mouse;
                                flickStarted = true;
                                ctrler.StartTimer();
                            }    
                            //pinballCam.GetComponent<Camera>().WorldToScreenPoint(pinball.transform.position),
                            //pinballCam.GetComponent<Camera>().WorldToScreenPoint(pinball.transform.position + new Vector3(indicatorLength, 0, 0))))
                            else if (Vector3.Distance(mouse, flickStartPos) > .1)
                            { // end flick if reaches max distance (10% of the screen)
                                FlickPinball();
                            }
                        }
                        else
                        {
                            Cursor.visible = false;

                            // Draw the indicator if it hasn't been already enabled
                            if (!directionIndicator.activeSelf)
                            {
                                directionIndicator.SetActive(true);
                                ctrler.StartTimer();
                            }

                            // Direction is calculated by projecting the mouse position onto the 
                            // pinball plane and clamping it to a maximum length of 10 centimeters
                            Vector3 mouse = GetMousePoint(pinball.transform);

                            direction = Vector3.ClampMagnitude(mouse - pinball.transform.position, indicatorLength);

                            if (ctrler.Session.CurrentBlock.settings.GetString("per_block_indicator_type") == "arc")
                            {
                                if (direction.z < 0)
                                {
                                    arcIndicator.gameObject.SetActive(true);
                                    arcIndicator.TargetDistance = Mathf.Lerp(-.337f, -.141f, direction.magnitude / indicatorLength);
                                    arcIndicator.GenerateArc();
                                }
                                else
                                {
                                    arcIndicator.gameObject.SetActive(false);
                                }
                            }

                            // Setup visual feedback for where the participant is aiming

                            // When true, the indicator is in front of the pinball
                            if (indicatorPosition)
                            {
                                directionIndicator.transform.position = pinball.transform.position + direction;
                            }
                            else
                            {
                                directionIndicator.transform.position = pinball.transform.position - direction;
                            }

                            directionIndicator.transform.LookAt(pinball.transform.position);
                        }
                    }
                    else if (Input.GetMouseButtonUp(0))
                    {
                        if (ctrler.Session.CurrentTrial.settings.GetString("per_block_fire_mode") == "spring")
                            FirePinball();
                        else
                            FlickPinball();
                    }
                }
                else // VR Controls
                {
                    if (ctrler.Session.CurrentTrial.settings.GetString("per_block_fire_mode") == "flick")
                    {
                        if (ctrler.CursorController.IsTriggerDown())
                        {
                            Vector3 hand = ctrler.CursorController.GetHandPosition();

                            Debug.Log("trigger is down");

                            if (!flickStarted)
                            {
                                flickStartTime = Time.time;
                                flickStartPos = hand;
                                flickStarted = true;
                                ctrler.StartTimer();
                            }
                            else if (Vector3.Distance(hand, flickStartPos) > flickCutoff)
                            { // end flick if reaches max distance 
                                FlickPinball();
                            }
                        }
                        else if (!ctrler.CursorController.IsTriggerDown() && flickStarted)
                        { // user lifts trigger
                            FlickPinball();
                        }
                    }
                    else
                    {
                        if (ctrler.CursorController.IsTriggerDown() &&
                            pinball.GetComponent<Grabbable>().Grabbed)
                        {
                            // If the user presses the trigger while hovering over the pinball, move to next step
                            aiming = true;

                            directionIndicator.SetActive(true);
                            ctrler.StartTimer();
                        }
                        else if (aiming)
                        {
                            Vector3 handCoordinates = new Vector3(
                                currentHand.transform.position.x,
                                pinball.transform.position.y,
                                currentHand.transform.position.z);

                            direction = -Vector3.ClampMagnitude(pinball.transform.position - handCoordinates, indicatorLength);

                            directionIndicator.transform.localScale = new Vector3(
                                direction.magnitude,
                                directionIndicator.transform.localScale.y,
                                directionIndicator.transform.localScale.z
                            );

                            directionIndicator.transform.position = pinball.transform.position - direction / 2f;
                            directionIndicator.transform.LookAt(pinball.transform.position);
                            directionIndicator.transform.RotateAround(directionIndicator.transform.position,
                                directionIndicator.transform.up,
                                90f);

                            if (ctrler.CursorController.triggerUp)
                                FirePinball();
                        }
                    }
                }

                // If the user runs out of time to fire the pinball, play audio cue
                if (!missed && timerIndicator.GetComponent<TimerIndicator>().Timer <= 0.0f)
                {
                    missed = true;
                    pinballSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["incorrect"];
                    pinballSpace.GetComponent<AudioSource>().Play();
                }

                break;
            case 1:
                // Track a point every 25 milliseconds
                if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_show_path"))
                    pinballPoints.Add(pinball.transform.position);

                break;
            case 2:
                // Pause the screen for 1.5 seconds
                if (timer == 0f)
                {
                    pinballSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["incorrect"];
                    bonusText.GetComponentInChildren<Text>().color = Color.white;

                    // If the pinball is inside the diameter of the target
                    distanceToTarget = Vector3.Distance(lastPositionInTarget, pinballAlignedTargetPosition);
                    if (distanceToTarget < 0.05f)
                    {
                        if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_show_path"))
                        {
                            pinballSpace.GetComponent<LineRenderer>().startColor =
                                pinballSpace.GetComponent<LineRenderer>().endColor =
                                    Target.GetComponent<BaseTarget>().Collided ? Color.green : Color.yellow;

                            if (!missed)
                            {
                                Target.transform.GetChild(0).GetComponent<ParticleSystem>().Play();
                            }
                        }

                        pinballSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["correct"];

                        // Freeze pinball
                        pinball.GetComponent<Rigidbody>().isKinematic = true;
                    }

                    bonusText.transform.position = pinball.transform.position + pinballCam.transform.up * 0.05f;
                    LeanTween.move(bonusText, bonusText.transform.position + (pinballCam.transform.up * 0.05f), 1.5f);

                    // If the participant fired the pinball within the allowed time & score tracking is enabled in json
                    if (!missed && timerIndicator.GetComponent<TimerIndicator>().Timer >= 0.0f)
                    {
                        pinballSpace.GetComponent<AudioSource>().Play();

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

                    timer += Time.deltaTime;
                }

                if (timer < 1.5f)
                {
                    ctrler.IsTracking = false;

                    timer += Time.deltaTime;

                    if (timer > 0.08f)
                    {
                        // Freezes pinball in place
                        pinball.GetComponent<Rigidbody>().isKinematic = true;

                        // Set pinball trail
                        pinballSpace.GetComponent<LineRenderer>().positionCount = pinballPoints.Count;
                        pinballSpace.GetComponent<LineRenderer>().SetPositions(pinballPoints.ToArray());

                        // Set transform
                        if (enteredTarget)
                        {
                            pinball.transform.position = lastPositionInTarget;
                        }
                        else
                        {
                            pinball.transform.position = pinballSpace.GetComponent<LineRenderer>().GetPosition(
                                pinballSpace.GetComponent<LineRenderer>().positionCount - 1);
                        }
                    }
                    else if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_show_path") &&
                             !enteredTarget)
                    {
                        // Add points to show feedback past the target only if they missed
                        // Points along the path are not added if they hit the target
                        pinballPoints.Add(pinball.transform.position);
                    }
                }
                else
                {
                    IncrementStep();
                }

                break;
        }

        if (Finished) ctrler.EndAndPrepare();
    }

    private void FlickPinball()
    {
        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball")
        {
            float flickTime = Time.time - flickStartTime;
            Vector3 mouse = GetMouseScreenPercentage();

            Vector3 tempDir = flickStartPos - mouse;
            tempDir.Normalize();

            if (ctrler.Session.settings.GetString("experiment_mode") == "pinball")
                tempDir = Quaternion.Euler(90, 0, 0) * tempDir;
            tempDir = Quaternion.Euler(0, 0, surfaceTilt) * tempDir;

            direction = Vector3.ClampMagnitude(tempDir / (flickTime * 50), indicatorLength);
        }
        else // VR flick
        {
            initialVelocity = ctrler.CursorController.GetVelocity();
            direction = -initialVelocity;
            direction.y = 0;

            // Perturbation
            if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated")
            {
                float angle = ctrler.Session.CurrentTrial.settings
                    .GetFloat("per_block_rotation");

                direction = Quaternion.Euler(0f, -angle, 0f) * direction;
            }

            direction = Quaternion.Euler(0, 0, surfaceTilt) * direction;
        }

        flickEndTime = Time.time;

        FirePinball();        
    }

    private void FirePinball()
    {
        ctrler.EndTimer();

        // Face firing direction and set velocity
        pinball.transform.LookAt(pinball.transform.position - direction.normalized);

        if (ctrler.Session.CurrentBlock.settings.GetBool("per_block_tilt_after_fire"))
            SetTilt();

        // Perturbation
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated" && !(ctrler.Session.CurrentBlock.settings.GetString("per_block_fire_mode") == "flick"))
        {
            float angle = ctrler.Session.CurrentTrial.settings
                .GetFloat("per_block_rotation");

            direction = Quaternion.Euler(0f, -angle, 0f) * direction;
        }

        pinball.GetComponent<Rigidbody>().useGravity = true;

        pinball.GetComponent<Rigidbody>().maxAngularVelocity = 240;

        Vector3 force = pinball.transform.forward * direction.magnitude;
        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball_vr" && (ctrler.Session.CurrentBlock.settings.GetString("per_block_fire_mode") == "flick"))
        {
            force *= VR_FLICK_FIRE_FORCE;
        }
        else
        {
            force *= PINBALL_FIRE_FORCE;
        }

        pinball.GetComponent<Rigidbody>().velocity = force;

        directionIndicator.GetComponent<AudioSource>().Play();

        timerIndicator.GetComponent<TimerIndicator>().Cancel();

        // Creates a plane parallel to the main surface
        pPlane = new Plane(Surface.transform.up, Surface.transform.position);

        // Gets the point along the surface of the plane by adding the thickness of the plane
        Vector3 targetLocation = pPlane.ClosestPointOnPlane(Target.transform.position) +
                                 (Surface.transform.localScale.y / 2f) * pPlane.normal;

        // Adds the radius of the pinball such that the resulting point above the target is
        // parallel to the pinball
        pinballAlignedTargetPosition = targetLocation + (pinball.transform.localScale.x / 2f) * pPlane.normal;

        // Add Pinball to tracked objects
        ctrler.AddTrackedObject("pinball_path", pinball);
        timeBallTrackingStarts = Time.time;

        IncrementStep();
    }

    public override void LogParameters()
    {
        // Note: ALL vectors are in world space
        ctrler.LogObjectPosition("cursor", directionIndicator.transform.position);

        // Note: Vector used is the last updated position of the pinball if it was within 5cm
        // of the target.
        ctrler.LogObjectPosition("pinball", lastPositionInTarget);

        // Log home and target positions
        ctrler.LogObjectPosition("home", pinballStartPosition);
        ctrler.LogObjectPosition("target", pinballAlignedTargetPosition);

        // Error is the distance between the pinball and the target (meters)
        Vector3 dist = lastPositionInTarget - pinballAlignedTargetPosition;
        ctrler.Session.CurrentTrial.result["error_size"] = dist.magnitude;

        // Converts indicator angle such that 0 degrees represents the right side of the pinball
        float angle = 270.0f - directionIndicator.transform.localRotation.eulerAngles.y;

        // Accounts for angles in the bottom right quadrant (270-360 degrees)
        if (angle < 0.0f) angle += 360.0f;

        ctrler.Session.CurrentTrial.result["indicator_angle"] = angle;

        // Magnitude is the distance (meters) on how much the participant pulled the spring back
        ctrler.Session.CurrentTrial.result["magnitude"] = 
            (directionIndicator.transform.position - pinballStartPosition).magnitude;

        ctrler.Session.CurrentTrial.result["show_path"] =
            ctrler.Session.CurrentTrial.settings.GetBool("per_block_show_path");

        ctrler.Session.CurrentTrial.result["tilt_after_fire"] =
            ctrler.Session.CurrentTrial.settings.GetBool("per_block_tilt_after_fire");

        ctrler.LogObjectPosition("flick_velocity", initialVelocity);
        ctrler.LogObjectPosition("flick_direction", direction);
        ctrler.Session.CurrentTrial.result["flick_multiplier"] = VR_FLICK_FIRE_FORCE;

        ctrler.Session.CurrentTrial.result["flick_start_time"] = flickStartTime;
        ctrler.Session.CurrentTrial.result["flick_end_time"] = flickEndTime;
        ctrler.Session.CurrentTrial.result["tracking_start_time"] = timeBallTrackingStarts;
        ctrler.Session.CurrentTrial.result["tracking_start_time"] = timeHandTrackingStarts;
    }

    public override void Setup()
    {
        maxSteps = 3;

        ctrler = ExperimentController.Instance();

        pinballSpace = Instantiate(ctrler.GetPrefab("PinballPrefab"));

        base.Setup();

        pinball = GameObject.Find("Pinball"); 
        Home = GameObject.Find("PinballHome");
        Target = GameObject.Find("PinballTarget");
        pinballCam = GameObject.Find("PinballCamera");
        directionIndicator = GameObject.Find("PinballSpring");
        directionIndicator.SetActive(false);
        arcIndicator = GameObject.Find("ArcTarget").GetComponent<ArcScript>();
        arcIndicator.gameObject.SetActive(false);
        XRRig = GameObject.Find("XR Rig");
        pinballWall = GameObject.Find("PinballWall");
        XRPosLock = GameObject.Find("XRPosLock");
        XRCamOffset = GameObject.Find("Dummy Camera");

        bonusText = GameObject.Find("BonusText");
        obstacle = GameObject.Find("Obstacle");
        pinballSurface = GameObject.Find("Surface");
        handL = GameObject.Find("handL");
        handR = GameObject.Find("handR");
        handL.SetActive(false);
        handR.SetActive(false);

        float targetAngle = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_targetListToUse"));

        SetTargetPosition(targetAngle);

        // checks if the current trial uses the obstacle and activates it if it does
        if (ctrler.Session.CurrentBlock.settings.GetBool("per_block_obstacle"))
        {       
            obstacle.SetActive(true);
            // initializes the position
            obstacle.transform.position = new Vector3(0f, 0.065f, 0f);
            //rotates the object
            obstacle.transform.rotation = Quaternion.Euler(0f, -targetAngle + 90f, 0f);
            //moves object forward towards the direction it is facing
            obstacle.transform.position += Target.transform.forward.normalized * (TARGET_DISTANCE / 2);
        }
        else
        {
            obstacle.SetActive(false);
        }

        // Use static camera for non-vr version of pinball
        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball")
        {
            // Setup Pinball Camera Offset
            pinballCam.transform.position = pinballCamOffset;
            pinballCam.transform.rotation = Quaternion.Euler(pinballAngle, 0f, 0f);

            ctrler.CursorController.SetVRCamera(false);
        }
        else
        {
            ctrler.CursorController.UseVR = true;
            pinballCam.SetActive(false);
            ctrler.CursorController.SetCursorVisibility(false);

            timerIndicator.transform.position = Home.transform.position;
            scoreboard.transform.position += Vector3.up * 0.33f;

            if (ctrler.Session.CurrentBlock.settings.GetString("per_block_hand") == "l")
            {
                handL.SetActive(true);
            }
            else
            {
                handR.SetActive(true);
            }
        }

        // Cutoff distance is 15cm more than the distance to the target
        cutoffDistance = 0.15f + TARGET_DISTANCE;

        currentHand = ctrler.CursorController.CurrentHand();

        // Parent to experiment controller
        pinballSpace.transform.SetParent(ctrler.transform);
        pinballSpace.transform.localPosition = Vector3.zero;

        // Setup line renderer for pinball path
        pinballSpace.GetComponent<LineRenderer>().startWidth =
            pinballSpace.GetComponent<LineRenderer>().endWidth = 0.015f;

        timerIndicator.transform.rotation = Quaternion.LookRotation(
            timerIndicator.transform.position - pinballCam.transform.position);

        // Should the tilt be shown to the participant before they release the pinball?
        if (!ctrler.Session.CurrentBlock.settings.GetBool("per_block_tilt_after_fire"))
            SetTilt();

        pinballStartPosition = pinball.transform.position;

        timerIndicator.Timer = ctrler.Session.CurrentBlock.settings.GetFloat("per_block_timerTime");

        timerIndicator.GetComponent<TimerIndicator>().BeginTimer();

        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_indicator_type") == "arc")
        {
            directionIndicator.GetComponent<MeshRenderer>().enabled = false;
            
        }

        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_fire_mode") == "flick")
        {
            Cursor.visible = false;
        }

        // Start tracking hand pos
        ctrler.AddTrackedObject("hand", ctrler.CursorController.CurrentHand());
        timeHandTrackingStarts = Time.time;

        // set up surface materials for the plane
        switch (Convert.ToString(ctrler.PollPseudorandomList("per_block_surface_materials")))
        {
            case "default":
                // Default material in prefab
                break;

            case "brick":
                base.SetSurfaceMaterial(ctrler.Materials["GrassMaterial"]);
                pinballWall.GetComponent<MeshRenderer>().material = ctrler.Materials["BrickMat"];
                break;
        }
    }

    private void SetTilt()
    {
        Vector3 ball_pos = Home.transform.position + Vector3.up * 0.25f;
        //Vector3 rot_axis = pinballSpace.transform.forward;

        SetTilt(pinballCam, ball_pos, pinballSpace, cameraTilt);
        SetTilt(bonusText.transform.parent.gameObject, ball_pos, pinballSpace, cameraTilt);
        SetTilt(pinballWall, ball_pos, pinballSpace, cameraTilt);

        SetTilt(pinballSpace, ball_pos, pinballSpace, surfaceTilt); //Tilt surface

        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball_vr") //Tilt VR Camera if needed
        {
            //XRRig.transform.RotateAround(Home.transform.position + Vector3.up * 0.25f, pinballSpace.transform.forward,
            //   cameraTilt + surfaceTilt);
            SetTilt(XRRig, ball_pos, pinballSpace, cameraTilt + surfaceTilt);
            XRRig.transform.position = XRPosLock.transform.position; // lock position of XR Rig
            //XRCamOffset.transform.position = new Vector3(0, -0.8f, -0.2f);
        }

    }

    public override void Disable()
    {
        Vector3 ball_pos = Home.transform.position + Vector3.up * 0.25f;
        // Realign XR Rig to non-tilted position
        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball_vr")
        {
            //XRRig.transform.RotateAround(Home.transform.position + Vector3.up * 0.25f, pinballSpace.transform.forward,
            //    (cameraTilt + surfaceTilt) * -1);
            SetTilt(XRRig, ball_pos, pinballSpace, (cameraTilt + surfaceTilt) * -1);
        }

        pinballSpace.SetActive(false);

        // Enabling this will cause screen flicker. Only use if task involves both Non-VR and VR in the same experiment
        //ctrler.CursorController.SetVRCamera(true);
    }

    protected override void OnDestroy()
    {
        Destroy(pinballSpace);
    }

    // returns how far across the screen the mouse is
    // ex. middle of the screen returns (0.5, 0.5, 0)
    public Vector3 GetMouseScreenPercentage()
    {
        return new Vector3(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height, 0);
    }

    private Vector3 mousePoint;
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
        
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(
            lastPositionInTarget - ctrler.transform.position, 0.02f
            );

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(
            Target.transform.position - ctrler.transform.position, 0.02f
            );

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(directionIndicator.transform.position - ctrler.transform.position, 0.02f);
    }
}
