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

                        direction = Vector3.ClampMagnitude(pinball.transform.position - MouseToPlanePoint(), 0.1f);

                        directionIndicator.transform.localScale = new Vector3(
                            direction.magnitude,
                            directionIndicator.transform.localScale.y,
                            directionIndicator.transform.localScale.z
                        );

                        directionIndicator.transform.position = pinball.transform.position - direction / 2f;
                        directionIndicator.transform.LookAt(pinball.transform.position);
                        directionIndicator.transform.RotateAround(directionIndicator.transform.position, transform.up,
                            90f);

                        Debug.Log(direction.magnitude);
                    }
                    else if (Input.GetMouseButtonUp(0))
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
                }
                else
                {
                    // TODO
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
            ctrler.EndAndPrepare();
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

    // Converts the mouse screen coordinates to world space along the experiment plane
    private Vector3 MouseToPlanePoint()
    {
        Vector3 mouseWorldCoords = pinballCam.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
        return new Vector3(mouseWorldCoords.x, pinball.transform.position.y, mouseWorldCoords.z);
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
    }

    protected override void OnDestroy()
    {
        Destroy(pinballSpace);

        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball" &&
            oldMainCamera != null)
            oldMainCamera.SetActive(true);
    }
}
