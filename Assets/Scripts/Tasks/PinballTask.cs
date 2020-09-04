using System.Collections.Generic;
using UnityEngine;
using UXF;

public class PinballTask : BaseTask
{
    private GameObject pinballSpace;
    private GameObject oldMainCamera;
    private GameObject pinballCam;
    private GameObject pinball;
    private GameObject directionIndicator;

    private Trial trial;
    private ExperimentController ctrler;

    private float force;
    private Vector3 direction;

    private float timer = 0f;

    private float distanceToTarget;

    private static List<float> targetAngles = new List<float>();

    // True when the participant is holding the trigger down to aim the pinball
    private bool aiming;

    private GameObject currentHand;

    public void Init(Trial trial, List<float> angles)
    {
        maxSteps = 2;
        this.trial = trial;
        ctrler = ExperimentController.Instance();

        if (trial.numberInBlock == 1)
            targetAngles = angles;

        Setup();
    }


    // Update is called once per frame
    void Update()
    {
        //make sure that this is still centred on the exp controller
        //pinballSpace.transform.position = ctrler.transform.position; //this should probably just happen once but doing so on setup doesn't work for the first trial.
        Debug.Log(Vector3.Distance(pinball.transform.position, Home.transform.position));
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
                                                           ctrler.CursorController.MouseToPlanePoint(pinball.transform.position, 
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
                        FirePinball();
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
                        Debug.Log("should be grabbed");
                        
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
                            FirePinball();
                    }
                }
                break;
            case 1:
                // Trial ends if the pinball crosses the center of the target (~2.5cm)
                if (Vector3.Distance(pinball.transform.position, new Vector3(
                        Target.transform.position.x, 
                        pinball.transform.position.y,
                        Target.transform.position.z)) < 0.025f)
                {
                    LogParameters();
                }

                // ball is in motion. goto next step when ball stops
                // radius is equal to the distance between the target and home
                if (pinball.GetComponent<Rigidbody>().velocity.magnitude <= 0.0001f)
                {
                    if (timer <= 0.5f)
                        timer += Time.deltaTime;
                    else
                        LogParameters();
                }
                else if (Vector3.Distance(pinball.transform.position, Home.transform.position) >= distanceToTarget)
                    LogParameters();
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
        // Tilt perturbation
        pinballSpace.transform.RotateAround(pinballSpace.transform.position, pinballSpace.transform.forward,
            ctrler.Session.CurrentBlock.settings.GetFloat("per_block_tilt"));

        ctrler.EndTimer();
        direction.y = pinball.transform.position.y;

        // Perturbation
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated")
        {
            float angle = ExperimentController.Instance().Session.CurrentTrial.settings
                .GetFloat("per_block_rotation");

            direction = Quaternion.Euler(0f, -angle, 0f) * direction;
        }

        pinball.transform.LookAt(direction);
        force = direction.magnitude * 40f;
        //force = direction.magnitude * 100f;
        //force *= 240f;
        //pinball.GetComponent<Rigidbody>().AddForce(pinball.transform.forward.normalized * force);

        pinball.GetComponent<Rigidbody>().useGravity = true;
        pinball.GetComponent<Rigidbody>().velocity = pinball.transform.forward * force;
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

        ctrler.Session.CurrentTrial.result["pinball_x"] = pinball.transform.localPosition.x;
        ctrler.Session.CurrentTrial.result["pinball_y"] = pinball.transform.localPosition.y;
        ctrler.Session.CurrentTrial.result["pinball_z"] = pinball.transform.localPosition.z;

        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.x;
        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.y;
        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.z;

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

        float targetAngle = targetAngles[0];
        targetAngles.RemoveAt(0);

        Target.transform.position = new Vector3(0f, 0.003f, 0f);
        Target.transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        Target.transform.position += Target.transform.forward.normalized * 0.55f;

        // Use static camera for non-vr version of pinball
        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball")
        {
            oldMainCamera = GameObject.Find("Main Camera");
            oldMainCamera.SetActive(false);
        }
        else
        {
            pinballCam.SetActive(false);
        }

        distanceToTarget = Vector3.Distance(Target.transform.position, Home.transform.position);

        // Cutoff distance is 15cm more than the distance to the target
        distanceToTarget += 0.15f;

        currentHand = ExperimentController.Instance().CursorController.CurrentHand();

        // Parent to experiment controller
        pinballSpace.transform.SetParent(ExperimentController.Instance().transform);
    }

    protected override void OnDestroy()
    {
        Destroy(pinballSpace);

        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball" &&
            oldMainCamera != null)
            oldMainCamera.SetActive(true);
    }
}
