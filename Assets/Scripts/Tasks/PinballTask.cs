using UnityEngine;
using UXF;

public class PinballTask : BaseTask
{
    private GameObject pinballSpace;
    private GameObject oldMainCamera;
    private GameObject camera;
    private GameObject pinball;
    private GameObject directionIndicator;

    private Trial trial;

    private float force;
    private Vector3 direction;

    private float timer = 0f;

    private float distanceToTarget;

    public void Init(Trial trial)
    {
        maxSteps = 2;
        this.trial = trial;
        Setup();
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentStep)
        {
            case 0:
                if (Input.GetMouseButton(0))
                {
                    if (!directionIndicator.activeSelf)
                    {
                        directionIndicator.SetActive(true);
                        ExperimentController.Instance().StartTimer();
                    }

                    direction = Vector3.ClampMagnitude(pinball.transform.position - MouseToPlanePoint(), 0.1f);

                    directionIndicator.transform.localScale = new Vector3(
                        direction.magnitude,
                        directionIndicator.transform.localScale.y,
                        directionIndicator.transform.localScale.z
                        );

                    directionIndicator.transform.position = pinball.transform.position - direction / 2f;
                    directionIndicator.transform.LookAt(pinball.transform.position);
                    directionIndicator.transform.RotateAround(directionIndicator.transform.position, transform.up, 90f);

                    Debug.Log(direction.magnitude);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    ExperimentController.Instance().EndTimer();
                    direction.y = pinball.transform.position.y;
                    pinball.transform.LookAt(direction);
                    force = direction.magnitude * 45f;
                    //force = direction.magnitude * 100f;
                    //force *= 240f;
                    //pinball.GetComponent<Rigidbody>().AddForce(pinball.transform.forward.normalized * force);

                    pinball.GetComponent<Rigidbody>().velocity = pinball.transform.forward * force;
                    IncrementStep();
                }
                break;
            case 1:
                // ball is in motion. goto next step when ball stops
                // radius is equal to the distance between the target and home
                Debug.Log(pinball.GetComponent<Rigidbody>().velocity.sqrMagnitude);
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
            ExperimentController.Instance().EndAndPrepare();
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
        Vector3 mouseWorldCoords = camera.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);

        return new Vector3(mouseWorldCoords.x, pinball.transform.position.y, mouseWorldCoords.z);
    }

    protected override void Setup()
    {
        pinballSpace = Instantiate(ExperimentController.Instance().GetPrefab("PinballPrefab"));
        pinball = GameObject.Find("Pinball");
        Home = GameObject.Find("PinballHome");
        Target = GameObject.Find("PinballTarget");
        camera = GameObject.Find("PinballCamera");
        directionIndicator = GameObject.Find("PinballSpring");
        directionIndicator.SetActive(false);

        oldMainCamera = GameObject.Find("Main Camera");
        oldMainCamera.SetActive(false);

        distanceToTarget = Vector3.Distance(Target.transform.position, Home.transform.position);

        // Cutoff distance is slightly more than the distance to the target
        distanceToTarget += 0.06f;
    }

    protected override void OnDestroy()
    {
        Destroy(pinballSpace);
        oldMainCamera.SetActive(true);
    }
}
