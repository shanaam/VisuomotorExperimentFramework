using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UXF;

public class PinballTask : BaseTask
{
    // Tilt options
    private const int VISUAL_TILT_NOT_VISIBLE = 0;          // Rotates the entire experiment space
    private const int VISUAL_TILT_VISIBLE = 1;              // Plane is tilted, but background is not
    private const int VISUAL_TILT_VISIBLE_RELEASE = 2;      // Same as VISUAL_TILT_VISIBLE, but only after they release the pinball

    // Task gameobjects
    private GameObject pinballSpace;
    private GameObject pinballCam;
    private GameObject pinball;
    private GameObject directionIndicator;
    private GameObject XRRig;
    private GameObject pinballPlane;
    private GameObject pinballWall;

    private ExperimentController ctrler;

    // Used for pinball aiming
    private float force;
    private Vector3 direction;

    private float timer;

    private float cutoffDistance;

    // Minimum distance to score any points. this is also the cutoff distance
    // for starting the miss timer
    private const float SCORING_DISTANCE = 0.10f;
    private const float TARGET_DISTANCE = 0.55f; // Target distance from home

    private static List<float> targetAngles = new List<float>();

    // True when the participant is holding the trigger down to aim the pinball
    private bool aiming;

    private GameObject currentHand;

    // Used to draw the path of the pinball for feedback mode
    private List<Vector3> pinballPoints = new List<Vector3>();

    // When the pinball is within a
    private float missTimer;
    private Vector3 previousPosition;

    private float distanceToTarget;

    // Pinball Camera Offset
    Vector3 pinballCamOffset = new Vector3(0f, 0.725f, -0.535f);
    private float pinballAngle = 35f;

    private Vector3 lastPositionInTarget, lastLocalPositionInTarget;

    // True when the pinball enters the target circle for the first time
    private bool enteredTarget = false;

    // When true, the indicator will be placed in front of the pinball
    private bool indicatorPosition = true;

    // Distance from pinball in meters the indicator will be shown
    private float indicatorLength = 0.2f;

    public void Init(Trial trial, List<float> angles)
    {
        maxSteps = 3;
        ctrler = ExperimentController.Instance();

        //what is this for?
        if (trial.numberInBlock == 1)
            targetAngles = angles;

        Setup();
    }

    void FixedUpdate()
    {
        // While the pinball is in motion
        if (currentStep == 1)
        {
            // Current distance from pinball to the target
            distanceToTarget = Vector3.Distance(pinball.transform.position, Target.transform.position);

            // Only check when the distance from pinball to target is less than half of the distance
            // between the target and home position and if the pinball is NOT approaching the target
            if (distanceToTarget <= TARGET_DISTANCE / 2f &&
                distanceToTarget > Vector3.Distance(previousPosition, Target.transform.position))
            {
                // The pinball only has 500ms of total time to move away from the target
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
                if (distanceToTarget > previousDistanceToTarget)
                {
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
                lastPositionInTarget = pinball.transform.position;
                IncrementStep();
            }

            if (distanceToTarget < 0.05f)
            {
                // set a temp variable to the pinballs position
                lastPositionInTarget = pinball.transform.position;
                enteredTarget = true;
            }

            previousPosition = pinball.transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //make sure that this is still centered on the exp controller
        switch (currentStep)
        {
            case 0:
                // If non-vr use mouse inputs, otherwise use the controller as input
                if (ctrler.Session.settings.GetString("experiment_mode") == "pinball")
                {
                    if (Input.GetMouseButton(0))
                    {
                        // Draw the indicator if it hasn't been already enabled
                        if (!directionIndicator.activeSelf)
                        {
                            directionIndicator.SetActive(true);
                            ctrler.StartTimer();
                        }

                        // Direction is calculated by projecting the mouse position onto the 
                        // pinball plane and clamping it to a maximum length of 10 centimeters
                        Vector3 mouse = ctrler.CursorController.MouseToPlanePoint(
                            pinballPlane.transform.up * pinball.transform.position.y,
                            pinball.transform.position,
                            pinballCam.GetComponent<Camera>());

                        direction = Vector3.ClampMagnitude(mouse - pinball.transform.position, indicatorLength);

                        // Setup visual feedback for where the participant is aiming

                        // When true, the indicator is in front of the pinball
                        if (indicatorPosition)
                        {
                            directionIndicator.transform.position = pinball.transform.position + direction / 2f;
                        }
                        else
                        {
                            directionIndicator.transform.position = pinball.transform.position - direction / 2f;
                        }

                        directionIndicator.transform.LookAt(pinball.transform.position);
                    }
                    else if (Input.GetMouseButtonUp(0))
                    {
                        FirePinball();
                    }

                }
                else // VR Controls
                {
                    if (ExperimentController.Instance().CursorController.IsTriggerDown() &&
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

                        direction = Vector3.ClampMagnitude(pinball.transform.position - handCoordinates, 0.1f);

                        directionIndicator.transform.localScale = new Vector3(
                            direction.magnitude,
                            directionIndicator.transform.localScale.y,
                            directionIndicator.transform.localScale.z
                        );

                        directionIndicator.transform.position = pinball.transform.position - direction / 2f;
                        directionIndicator.transform.LookAt(pinball.transform.position);
                        directionIndicator.transform.RotateAround(directionIndicator.transform.position, directionIndicator.transform.up,
                            90f);

                        if (ExperimentController.Instance().CursorController.triggerUp)
                        {
                            FirePinball();
                        }
                    }
                }

                break;
            case 1:
                // Track a point every 25 milliseconds
                if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                {
                    pinballPoints.Add(pinball.transform.position);
                }
                break;
            case 2:
                // Pause the screen for 1.5 seconds
                if (timer == 0f)
                {
                    pinballSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["incorrect"];

                    // If the pinball is inside the diameter of the target
                    distanceToTarget = Vector3.Distance(lastPositionInTarget, Target.transform.position);
                    if (distanceToTarget < 0.05f)
                    {
                        if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                        {
                            pinballSpace.GetComponent<LineRenderer>().startColor =
                                pinballSpace.GetComponent<LineRenderer>().endColor =
                                    Target.GetComponent<BaseTarget>().Collided ? Color.green : Color.yellow;

                            Target.transform.GetChild(0).GetComponent<ParticleSystem>().Play();
                        }

                        pinballSpace.GetComponent<AudioSource>().clip = ctrler.AudioClips["correct"];

                        // Freeze pinball
                        pinball.GetComponent<Rigidbody>().isKinematic = true;
                    }

                    // Scoring
                    if (Target.GetComponent<BaseTarget>().Collided)
                    {
                        ctrler.Score += 2;
                    }
                    else if (distanceToTarget < 0.05f)
                    {
                        ctrler.Score += 1;
                    }

                    pinballSpace.GetComponent<AudioSource>().Play();
                    timer += Time.deltaTime;
                }

                if (timer < 1.5f)
                {
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
                    else if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback") && !enteredTarget)
                    {
                        // Add points to show feedback past the target only if they missed
                        // Points along the path are not added if they hit the target
                        pinballPoints.Add(pinball.transform.position);
                    }
                }
                else
                {
                    LogParameters();
                }

                break;
        }

        if (Finished)
        {
            ctrler.EndAndPrepare();
        }
    }

    private void FirePinball()
    {
        ctrler.EndTimer();

        // Face firing direction and set velocity
        pinball.transform.LookAt(pinball.transform.position - direction.normalized);

        if (ctrler.Session.CurrentBlock.settings.GetInt("per_block_visual_tilt") != VISUAL_TILT_VISIBLE)
        {
            SetTilt();
        }

        // Perturbation
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated")
        {
            float angle = ExperimentController.Instance().Session.CurrentTrial.settings
                .GetFloat("per_block_rotation");

            direction = Quaternion.Euler(0f, -angle, 0f) * direction;
        }

        pinball.GetComponent<Rigidbody>().useGravity = true;
        pinball.GetComponent<Rigidbody>().velocity = pinball.transform.forward *
                                                     5f * (direction.magnitude / 0.2f);

        directionIndicator.GetComponent<AudioSource>().Play();

        IncrementStep();
    }

    private void LogParameters()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        // Note: ALL vectors are in world space

        ctrler.Session.CurrentTrial.result["cursor_x"] = directionIndicator.transform.position.x;
        ctrler.Session.CurrentTrial.result["cursor_y"] = directionIndicator.transform.position.y;
        ctrler.Session.CurrentTrial.result["cursor_z"] = directionIndicator.transform.position.z;

        // Note: Vector used is the last updated position of the pinball if it was within 5cm
        // of the target.

        ctrler.Session.CurrentTrial.result["pinball_x"] = lastPositionInTarget.x;
        ctrler.Session.CurrentTrial.result["pinball_y"] = lastPositionInTarget.y;
        ctrler.Session.CurrentTrial.result["pinball_z"] = lastPositionInTarget.z;

        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.position.x;
        ctrler.Session.CurrentTrial.result["target_y"] = Target.transform.position.y;
        ctrler.Session.CurrentTrial.result["target_z"] = Target.transform.position.z;

        ctrler.Session.CurrentTrial.result["error_size"] =
            (Target.transform.position - lastLocalPositionInTarget).magnitude;

        Debug.Log("Distance to target: " + distanceToTarget);

        IncrementStep();
    }

    protected override void Setup()
    {
        pinballSpace = Instantiate(ctrler.GetPrefab("PinballPrefab"));

        pinball = GameObject.Find("Pinball");
        Home = GameObject.Find("PinballHome");
        Target = GameObject.Find("PinballTarget");
        pinballCam = GameObject.Find("PinballCamera");
        pinballPlane = GameObject.Find("PinballPlane");
        directionIndicator = GameObject.Find("PinballSpring");
        directionIndicator.SetActive(false);
        XRRig = GameObject.Find("XR Rig");
        pinballWall = GameObject.Find("PinballWall");

        float targetAngle = targetAngles[0];
        targetAngles.RemoveAt(0);

        Target.transform.position = new Vector3(0f, 0.065f, 0f);
        Target.transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        Target.transform.position += Target.transform.forward.normalized * TARGET_DISTANCE;

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
            pinballCam.SetActive(false);
        }

        cutoffDistance = Vector3.Distance(Target.transform.position, Home.transform.position);

        // Cutoff distance is 15cm more than the distance to the target
        cutoffDistance += 0.15f;

        currentHand = ExperimentController.Instance().CursorController.CurrentHand();

        // Parent to experiment controller
        pinballSpace.transform.SetParent(ExperimentController.Instance().transform);
        pinballSpace.transform.localPosition = Vector3.zero;

        // Setup line renderer for pinball path
        pinballSpace.GetComponent<LineRenderer>().startWidth =
            pinballSpace.GetComponent<LineRenderer>().endWidth = 0.015f;

        // Should the tilt be shown to the participant before they release the pinball?
        if (ctrler.Session.CurrentBlock.settings.GetInt("per_block_visual_tilt") == VISUAL_TILT_VISIBLE)
        {
            SetTilt();
        }
    }

    private void SetTilt()
    {
        // Should the participant be able to see the tilt relative to the environment?
        if (ctrler.Session.CurrentBlock.settings.GetInt("per_block_visual_tilt") == VISUAL_TILT_NOT_VISIBLE)
        {
            // Tilt VR space too
            if (ctrler.Session.settings.GetString("experiment_mode") == "pinball_vr")
            {
                XRRig.transform.RotateAround(pinballSpace.transform.position, pinballSpace.transform.forward,
                    ctrler.Session.CurrentBlock.settings.GetFloat("per_block_tilt"));
            }
        }
        else
        {
            // The participant will be allowed to see the tilt relative to the environment

            // Unparent wall and camera so plane moves independently
            pinballWall.transform.SetParent(null);
            pinballCam.transform.SetParent(null);
        }

        // Set the tilt of the table
        pinballSpace.transform.RotateAround(pinballSpace.transform.position, pinballSpace.transform.forward,
            ctrler.Session.CurrentBlock.settings.GetFloat("per_block_tilt"));

        // Reparent wall and camera
        pinballWall.transform.SetParent(pinballSpace.transform);
        pinballCam.transform.SetParent(pinballSpace.transform);
    }

    protected override void OnDestroy()
    {
        // Realign XR Rig to non-tilted position
        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball_vr")
        {
            XRRig.transform.RotateAround(pinballSpace.transform.position, pinballSpace.transform.forward,
                ctrler.Session.CurrentBlock.settings.GetFloat("per_block_tilt") * -1);
        }

        pinballSpace.SetActive(false);

        Destroy(pinballSpace);

        ctrler.CursorController.SetVRCamera(true);
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
