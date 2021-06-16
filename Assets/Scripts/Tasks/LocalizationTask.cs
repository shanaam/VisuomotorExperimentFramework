using System.Collections.Generic;
using UnityEngine;
using UXF;

public class LocalizationTask : BaseTask
{
    private GameObject[] targets = new GameObject[3];
    private GameObject localizer; // Cursor that indicates where the user's head is gazing

    private Trial trial;

    private GameObject localizationCam;
    private GameObject localizationSurface;
    private GameObject localizationPrefab;

    public void LateUpdate()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        switch (currentStep)
        {
            // When the user holds their hand and they are outside the home, begin the next phase of localization
            case 2 when ctrler.CursorController.PauseTime > 0.5f && 
                        ctrler.CursorController.DistanceFromHome > 0.05f:
                IncrementStep();
                break;
            case 3: // User uses their head to localize their hand
                Plane plane = new Plane(Vector3.down, ctrler.transform.position.y);
                Ray r = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                
                if (plane.Raycast(r, out float hit))
                    localizer.transform.position = r.GetPoint(hit);

                // If the user presses the trigger associated with the hand, we end the trial
                if (ctrler.CursorController.IsTriggerDown(ctrler.CursorController.CurrentTaskHand) || Input.GetKeyDown(KeyCode.N) || Input.GetMouseButtonDown(0))
                    IncrementStep();

                break;
        }

        if (Finished)
            ExperimentController.Instance().EndAndPrepare();
    }

    public override bool IncrementStep()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        switch (currentStep)
        {
            case 0: // Enter dock
                targets[0].SetActive(false);
                Home.SetActive(true);
                break;
            case 1: // Enter home
                Home.SetActive(false);

                ctrler.StartTimer();

                // Create tracker objects
                ctrler.AddTrackedObject("hand_path",
                    ctrler.Session.CurrentTrial.settings.GetString("per_block_hand") == "l"
                        ? ctrler.CursorController.LeftHand
                        : ctrler.CursorController.RightHand);

                ctrler.AddTrackedObject("cursor_path", ctrler.CursorController.gameObject);

                Target.SetActive(true);

                ExperimentController.Instance().CursorController.SetCursorVisibility(false);

                break;
            case 2: // Pause in arc
                localizer.SetActive(true);
                Target.GetComponent<ArcScript>().Expand();

                break;
            case 3: // Select the spot they think their real hand is
                Target.SetActive(false);

                // TODO: Add support for mouse
                // We use the target variable to store the cursor position
                Target.transform.position =
                    ExperimentController.Instance().CursorController.CurrentHand().transform.position;

                break;
        }

        base.IncrementStep();
        return finished;
    }

    public override void LogParameters()
    {
        // Store where they think their hand is
        ExperimentController.Instance().Session.CurrentTrial.result["loc_x"] =
            localizer.transform.localPosition.x;

        ExperimentController.Instance().Session.CurrentTrial.result["loc_y"] =
            localizer.transform.localPosition.y;

        ExperimentController.Instance().Session.CurrentTrial.result["loc_z"] =
            localizer.transform.localPosition.z;
    }

    public override void Setup()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        maxSteps = 4;

        ctrler.CursorController.SetHandVisibility(false);
        Cursor.visible = false;
        ctrler.CursorController.SetCursorVisibility(true);

        localizationPrefab = Instantiate(ctrler.GetPrefab("LocalizationPrefab"));
        localizationPrefab.transform.SetParent(ctrler.transform);
        localizationPrefab.transform.localPosition = Vector3.zero;

        localizationCam = GameObject.Find("LocalizationCamera");
        localizationSurface = GameObject.Find("Surface");

        // Set up the dock position
        targets[0] = GameObject.Find("Dock");
        targets[0].transform.position = ctrler.TargetContainer.transform.position;

        // Set up the home position
        targets[1] = GameObject.Find("Home");
        targets[1].transform.position = ctrler.TargetContainer.transform.position + ctrler.transform.forward * 0.05f;
        targets[1].SetActive(false);
        Home = targets[1];

        // Grab an angle from the list and then remove it
        float targetAngle = ctrler.PollPseudorandomList("per_block_targetListToUse");

        // Set up the arc object
        targets[2] = GameObject.Find("ArcTarget");
        targets[2].transform.rotation = Quaternion.Euler(
            0f,
            -targetAngle + 90f,
            0f);

        targets[2].transform.position = targets[1].transform.position;

        targets[2].GetComponent<ArcScript>().TargetDistance = ctrler.Session.CurrentTrial.settings.GetFloat("per_block_distance") / 100f;
        targets[2].GetComponent<ArcScript>().Angle = targets[2].transform.rotation.eulerAngles.y;
        targets[2].transform.localScale = Vector3.one;
        Target = targets[2];

        // Set up the GameObject that tracks the user's gaze
        localizer = GameObject.Find("Localizer");
        localizer.GetComponent<SphereCollider>().enabled = false;
        localizer.GetComponent<BaseTarget>().enabled = false;
        localizer.SetActive(false);

        localizer.transform.SetParent(ctrler.TargetContainer.transform);
        localizer.name = "Localizer";

        Target.SetActive(false);


        // Use static camera for non-vr version of pinball
        if (ctrler.Session.settings.GetString("experiment_mode") == "target")
        {
            localizationSurface.SetActive(false);
            localizationCam.SetActive(false);
            ctrler.CursorController.UseVR = true;
        }
        else
        {
            ctrler.CursorController.SetVRCamera(false);
        }
    }

    public override void Disable()
    {
        foreach (GameObject g in targets)
            g.SetActive(false);

        localizer.SetActive(false);
    }

    protected override void OnDestroy()
    {
        foreach (GameObject g in targets)
            Destroy(g);

        Destroy(localizer);

        base.OnDestroy();
    }
}
