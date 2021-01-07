using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UXF;
using MovementType = CursorController.MovementType;

public class ToolTask : BaseTask
{

    private MovementType[] reachType;
    private Trial trial;

    private GameObject toolSpace;
    private GameObject tool;
    private GameObject obj;
    private GameObject toolCamera;
    private GameObject toolSurface;

    private static List<float> targetAngles = new List<float>();

    private ExperimentController ctrler;

    private float timer, distanceToTarget;

    private GameObject oldMainCamera;

    private float height;

    // Allows a delay when the participant initially hits the object
    private float initialDelayTimer;

    private GameObject visualCube;
    private Quaternion cubeRot;
    private Vector3 previousPosition;
    private float missTimer;

    public void Init(Trial trial, List<float> angles)
    {
        this.trial = trial;
        maxSteps = 4;

        ctrler = ExperimentController.Instance();

        if (trial.numberInBlock == 1)
            targetAngles = angles;

        Setup();
    }

    void FixedUpdate()
    {
        Vector3 mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, new Vector3(
            0f, tool.transform.position.y, 0f), toolCamera.GetComponent<Camera>());

        tool.GetComponent<Rigidbody>().velocity = Vector3.zero;

        if (Vector3.Distance(mousePoint, tool.transform.position) > 0.05f && currentStep == 0) return;

        switch (currentStep)
        {
            case 0: // Return to home position phase
            case 1: // User hits the object phase
                // Position is tied to either mouse position or the hand
                if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
                {
                    tool.GetComponent<BoxCollider>().enabled = mousePoint.z <= 0.5f;

                    Vector3 dir = mousePoint - tool.transform.position;
                    dir /= Time.fixedDeltaTime;

                    tool.GetComponent<Rigidbody>().velocity = dir;
                    tool.GetComponent<BoxCollider>().enabled = mousePoint.z <= 0.05f;

                    //mousePoint.Set(mousePoint.x, mousePoint.y,
                    //    Mathf.Clamp(mousePoint.z, toolSurface.transform.position.z - 1f,
                    //        toolSurface.transform.position.z + 0.05f));

                    //tool.GetComponent<Rigidbody>().MovePosition(mousePoint);
                    //tool.transform.position = mousePoint;
                }
                else
                {
                    tool.GetComponent<Rigidbody>().MovePosition(ctrler.CursorController.CurrentHand().transform.position);
                }

                break;
            case 2: // After the user hits the object
                // Used to determine if the object hit by the tool is heading away from the target
                // Current distance from pinball to the target
                float currentDistance = Vector3.Distance(obj.transform.position, Target.transform.position);

                // Only check when the distance from pinball to target is less than half of the distance
                // between the target and home position and if the pinball is NOT approaching the target
                if (currentDistance <= distanceToTarget / 2f &&
                    currentDistance > Vector3.Distance(previousPosition, Target.transform.position))
                {
                    // The object only has 500ms of total time to move away from the target
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

                previousPosition = obj.transform.position;
                break;
            case 3: // 
                if (Vector3.Distance(obj.transform.position, new Vector3(
                    Target.transform.position.x,
                    obj.transform.position.y,
                    Target.transform.position.z)) < 0.025f)
                {
                    LogParameters();
                }

                if (initialDelayTimer <= 1.0f)
                    initialDelayTimer += Time.deltaTime;
                else
                {
                    if (obj.GetComponent<Rigidbody>().velocity.magnitude <= 0.0001f)
                    {
                        if (timer <= 0.5f)
                            timer += Time.deltaTime;
                        else
                            LogParameters();
                    }
                    else if (Vector3.Distance(obj.transform.position, Home.transform.position) >= distanceToTarget)
                        LogParameters();
                }
                break;
        }

        if (Finished)
            ctrler.EndAndPrepare();
    }

    public override bool IncrementStep()
    {
        if (currentStep == 0)
        {
            obj.SetActive(true);
            Home.GetComponent<BaseTarget>().enabled = false;
            Home.GetComponent<MeshRenderer>().enabled = false;
        }

        return base.IncrementStep();
    }

    void LateUpdate()
    {
        // Lock rotation axis of cube. This is only a visual effect
        if (visualCube != null)
            visualCube.transform.rotation = Quaternion.Inverse(obj.transform.rotation);
    }

    protected override void Setup()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        toolSpace = Instantiate(ctrler.GetPrefab("ToolPrefab"));
        tool = GameObject.Find("Tool");
        obj = GameObject.Find("ToolObject");
        Target = GameObject.Find("ToolTarget");
        toolCamera = GameObject.Find("ToolCamera");
        toolSurface = GameObject.Find("ToolPlane");

        // Height above the surface. Height is y position of plane
        // plus thickness of surface (0.05) plus the half the width of the tool (0.075)
        height = toolSurface.transform.position.y + 0.08f;

        // Set up home position
        Home = Instantiate(ctrler.GetPrefab("Target"));
        Home.transform.position = tool.transform.position;
        Home.name = "Home";
        Home.transform.SetParent(toolSpace.transform);

        // Set up target
        float targetAngle = targetAngles[0];
        targetAngles.RemoveAt(0);

        Target.transform.position = new Vector3(0f, 0.05f, 0f);
        Target.transform.rotation = Quaternion.Euler(
            0f, -targetAngle + 90f, 0f);

        Target.transform.position += Target.transform.forward.normalized * 0.55f;

        // Set up camera for non VR and VR modes
        if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
        {
            oldMainCamera = GameObject.Find("Main Camera");
            oldMainCamera.SetActive(false);
        }
        else toolCamera.SetActive(false);

        distanceToTarget = Vector3.Distance(Target.transform.position, obj.transform.position);
        distanceToTarget += 0.15f;


        /*
        // Set up surface friction
        toolSurface.GetComponent<BoxCollider>().material.dynamicFriction =
            ctrler.Session.CurrentTrial.settings.GetFloat("per_block_surface_dynamic_friction");

        toolSurface.GetComponent<BoxCollider>().material.staticFriction =
            ctrler.Session.CurrentTrial.settings.GetFloat("per_block_surface_static_friction");

        // Set up tool friction

        // Set up object
        
        obj.GetComponent<SphereCollider>().material.dynamicFriction =
            ctrler.Session.CurrentTrial.settings.GetFloat("per_block_tool_dynamic_friction");

        obj.GetComponent<SphereCollider>().material.dynamicFriction =
            ctrler.Session.CurrentTrial.settings.GetFloat("per_block_tool_dynamic_friction");
        */
        
        obj.GetComponent<SphereCollider>().material.bounciness = 0.8f;
        tool.GetComponent<BoxCollider>().material.bounciness = 1f;
        tool.GetComponent<BoxCollider>().enabled = false;

        visualCube = GameObject.Find("ToolVisualCube");
        cubeRot = visualCube.transform.rotation;

        // Set up object type
        if (ctrler.Session.CurrentTrial.settings.GetString("per_block_object_type") == "sphere")
            GameObject.Find("ToolVisualCube").SetActive(false);
        else
            GameObject.Find("ToolVisualSphere").SetActive(false);

        

        // Disable object for first step
        obj.SetActive(false);
    }

    private void LogParameters()
    {
        ExperimentController ctrler = ExperimentController.Instance();

        ctrler.Session.CurrentTrial.result["obj"] = obj.transform.localPosition.x;
        ctrler.Session.CurrentTrial.result["obj"] = obj.transform.localPosition.y;
        ctrler.Session.CurrentTrial.result["obj"] = obj.transform.localPosition.z;

        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.x;
        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.y;
        ctrler.Session.CurrentTrial.result["target_x"] = Target.transform.localPosition.z;

        IncrementStep();
    }

    protected override void OnDestroy()
    {
        Destroy(toolSpace);

        if (ctrler.Session.settings.GetString("experiment_mode") == "tool" && oldMainCamera != null)
            oldMainCamera.SetActive(true);
    }

    void OnDrawGizmos()
    {
        Vector3 mousePoint = ctrler.CursorController.MouseToPlanePoint(Vector3.up, new Vector3(
            0f, tool.transform.position.y, 0f), toolCamera.GetComponent<Camera>());

        Gizmos.DrawLine(toolCamera.transform.position, mousePoint);

    }
}
