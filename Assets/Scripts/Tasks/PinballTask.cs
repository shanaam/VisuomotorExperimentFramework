using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UXF;

public class PinballTask : BaseTask
{
    // Task gameobjects
    private GameObject pinballSpace;
    private GameObject oldMainCamera;
    private GameObject pinballCam;
    private GameObject pinball;
    private GameObject directionIndicator;
    private GameObject XRRig;
    private GameObject testCube;

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

    public void Init(Trial trial, List<float> angles)
    {
        maxSteps = 3;
        ctrler = ExperimentController.Instance();

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

            // Trial ends if the pinball crosses the center of the target (5cm) OR
            // The ball stops moving OR
            // The distance between the home position and the pinball exceeds the distance
            // between the pinball and the target
            if (pinball.GetComponent<Rigidbody>().velocity.magnitude <= 0.0001f ||
                Vector3.Distance(pinball.transform.position, Home.transform.position) >= cutoffDistance ||
                Target.GetComponent<BaseTarget>().Collided)
            {
                IncrementStep();
            }

            if (distanceToTarget < 0.05f)
            {
                // set a temp variable to the pinballs position
                lastPositionInTarget = pinball.transform.position;
                lastLocalPositionInTarget = pinball.transform.localPosition;
            }



            previousPosition = pinball.transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //make sure that this is still centred on the exp controller
        //pinballSpace.transform.position = ctrler.transform.position; //this should probably just happen once but doing so on setup doesn't work for the first trial.
        // Debug.Log(Vector3.Distance(pinball.transform.position, Home.transform.position));

        switch (currentStep)
        {
            case 0:
                // If non-vr use mouse inputs, otherwise use the controller as input
                if (ctrler.Session.settings.GetString("experiment_mode") == "pinball")
                {
                    if (Input.GetMouseButton(0))
                    {
                        if (!directionIndicator.activeSelf)
                        {
                            directionIndicator.SetActive(true);
                            ctrler.StartTimer();
                        }

                        direction = Vector3.ClampMagnitude(pinball.transform.position -
                                                           ctrler.CursorController.MouseToPlanePoint(
                                                               pinball.transform.position,
                                                               pinballCam.GetComponent<Camera>()), 0.1f);

                        directionIndicator.transform.localScale = new Vector3(
                            direction.magnitude,
                            directionIndicator.transform.localScale.y,
                            directionIndicator.transform.localScale.z
                        );

                        directionIndicator.transform.position = pinball.transform.position - direction / 2f;
                        directionIndicator.transform.LookAt(pinball.transform.position);
                        directionIndicator.transform.RotateAround(directionIndicator.transform.position, transform.up,
                            90f);
                    }
                    else if (Input.GetMouseButtonUp(0))
                    {
                        FirePinball();
                    }

                }
                else //maybe else if for clarity
                {
                    //if (ExperimentController.Instance().CursorController.IsTriggerDown())
                    //    Debug.Log("Trigger is down");
                    //if (ExperimentController.Instance().CursorController.triggerUp)
                    //    Debug.Log("Trigger Up");

                    if (ExperimentController.Instance().CursorController.IsTriggerDown() &&
                        pinball.GetComponent<Grabbable>().Grabbed)
                    {
                        // If the user presses the trigger while hovering over the pinball, move to next step
                        aiming = true;
                        //Debug.Log("should be grabbed");

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
                        directionIndicator.transform.RotateAround(directionIndicator.transform.position, transform.up,
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
                    }
                    else if (ctrler.Session.CurrentTrial.settings.GetBool("per_block_visual_feedback"))
                    {
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
            Debug.Log("Trial Finished");
            ctrler.EndAndPrepare();
        }
    }

    private void FirePinball()
    {
        Vector3 oldCameraPosition = pinballCam.transform.position;
        Quaternion oldCameraRotation = pinballCam.transform.rotation;
        
        // Tilt perturbation
        pinballSpace.transform.RotateAround(pinballSpace.transform.position, pinballSpace.transform.forward,
            ctrler.Session.CurrentBlock.settings.GetFloat("per_block_tilt"));

        // Should the participant be able to see the tilt relative to the environment?
        if (ctrler.Session.CurrentBlock.settings.GetBool("per_block_visual_tilt"))
        {
            // Tilt VR space too
            if (ctrler.Session.settings.GetString("experiment_mode") == "pinball_vr")
            {
                XRRig.transform.RotateAround(pinballSpace.transform.position, pinballSpace.transform.forward,
                    ctrler.Session.CurrentBlock.settings.GetFloat("per_block_tilt"));
            }

            // Rotate the entire experiment space
            // TODO
        }
        else
        {
            // Put the camera back to where it was if visual tilt is disabled
            pinballCam.transform.SetPositionAndRotation(oldCameraPosition, oldCameraRotation);
        }

        ctrler.EndTimer();
        direction.y = pinball.transform.position.y;

        // Perturbation
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated")
        {
            float angle = ExperimentController.Instance().Session.CurrentTrial.settings
                .GetFloat("per_block_rotation");

            direction = Quaternion.Euler(0f, -angle, 0f) * direction;
        }

        Debug.Log("PB pos before fire: " + pinball.transform.position.ToString("F5"));

        Debug.Log("direction: " + direction.ToString("F5"));

        // Rotates direction by 90 degrees to reflect "forwards" direction
        Quaternion q = Quaternion.AngleAxis(90, directionIndicator.transform.up);

        // have pinball face the direction to be fired
        Vector3 lookAtPosition = pinball.transform.position + 
                                 q * directionIndicator.transform.forward.normalized;

        pinball.transform.LookAt(lookAtPosition);

        // ensure that direction is on horizontal plane for force calc
        direction.y = 0f;

        force = direction.magnitude * 50f;
        //pinball.GetComponent<Rigidbody>().AddForce(pinball.transform.forward.normalized * force);

        pinball.GetComponent<Rigidbody>().useGravity = true;
        pinball.GetComponent<Rigidbody>().velocity = pinball.transform.forward * force * -1;

        Debug.Log("forward for PB: " + pinball.transform.forward.ToString("F5"));

        Debug.Log("force applied: " + force.ToString("F5"));

        IncrementStep();
    }

    private void LogParameters()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        // Convert direction to local space
        direction = ctrler.transform.position - direction;

        ctrler.Session.CurrentTrial.result["cursor_x"] = direction.x;
        ctrler.Session.CurrentTrial.result["cursor_y"] = direction.y;
        ctrler.Session.CurrentTrial.result["cursor_z"] = direction.z;

        // Note: Vector used is the last updated position of the pinball if it was within 5cm
        // of the target.
        ctrler.Session.CurrentTrial.result["pinball_x"] = lastLocalPositionInTarget.x;
        ctrler.Session.CurrentTrial.result["pinball_y"] = lastLocalPositionInTarget.y;
        ctrler.Session.CurrentTrial.result["pinball_z"] = lastLocalPositionInTarget.z;

        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.x;
        ctrler.Session.CurrentTrial.result["target_y"] = Target.transform.localPosition.y;
        ctrler.Session.CurrentTrial.result["target_z"] = Target.transform.localPosition.z;

        Debug.Log("Distance to target: " + distanceToTarget);

        // Cutoff distances
        if (Target.GetComponent<BaseTarget>().Collided)
        {
            ctrler.Session.CurrentTrial.result["score"] = 2;
        }
        else if (distanceToTarget < 0.05f)
        {
            ctrler.Session.CurrentTrial.result["score"] = 1;
        }
        else
        {
            ctrler.Session.CurrentTrial.result["score"] = 0;
        }

        IncrementStep();
    }

    protected override void Setup()
    {
        pinballSpace = Instantiate(ctrler.GetPrefab("PinballPrefab"));

        pinball = GameObject.Find("Pinball");
        Home = GameObject.Find("PinballHome");
        Target = GameObject.Find("PinballTarget");
        pinballCam = GameObject.Find("PinballCamera");
        directionIndicator = GameObject.Find("PinballSpring");
        directionIndicator.SetActive(false);
        XRRig = GameObject.Find("XR Rig");

        // purely for testing
        //testCube = GameObject.Find("TestCube");

        float targetAngle = targetAngles[0];
        targetAngles.RemoveAt(0);

        Target.transform.position = new Vector3(0f, 0.075f, 0f);
        Target.transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        Target.transform.position += Target.transform.forward.normalized * TARGET_DISTANCE;
        Debug.Log("Distance: " + Vector3.Distance(Target.transform.position, Home.transform.position));

        // Use static camera for non-vr version of pinball
        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball")
        {
            // Setup Pinball Camera Offset
            pinballCam.transform.position = pinballCamOffset;
            pinballCam.transform.rotation = Quaternion.Euler(pinballAngle, 0f, 0f);

            oldMainCamera = GameObject.Find("Main Camera");
            oldMainCamera.GetComponent<TrackedPoseDriver>().enabled = false;
            oldMainCamera.transform.localPosition =
                oldMainCamera.transform.InverseTransformPoint(pinballCam.transform.position);
            oldMainCamera.transform.rotation = pinballCam.transform.rotation;
            oldMainCamera.SetActive(false);
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

        //Debug.Log("Exp controller position: " + ExperimentController.Instance().transform.position.ToString("F5"));
        Debug.Log("PB world position: " + pinballSpace.transform.position.ToString("F5"));
    }

    protected override void OnDestroy()
    {
        // Tilt back if required
        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball_vr")
        {
            XRRig.transform.RotateAround(pinballSpace.transform.position, pinballSpace.transform.forward,
                ctrler.Session.CurrentBlock.settings.GetFloat("per_block_tilt") * -1);
        }

        pinballSpace.SetActive(false);

        Destroy(pinballSpace);

        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball" &&
            oldMainCamera != null)
        {
            oldMainCamera.GetComponent<TrackedPoseDriver>().enabled = true;
            oldMainCamera.SetActive(true);
        }
    }

    void OnDrawGizmos()
    {
        if (currentStep >= 1)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lastPositionInTarget, 0.025f);
        } 
    }
}
