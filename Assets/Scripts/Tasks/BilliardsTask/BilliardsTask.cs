using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;
using UnityEngine.UI;
using System;

public abstract class BilliardsTask : BaseTask
{
    protected ExperimentController ctrler;

    public GameObject Surface;
    protected Scoreboard scoreboard;
    protected TimerIndicator timerIndicator;

    protected bool trackScore;

    protected float cameraTilt, surfaceTilt;

    // Minimum distance to score any points. this is also the cutoff distance
    // for starting the miss timer
    protected const float TARGET_DISTANCE = 0.85f; // Target distance from home

    public override void Setup()
    {
        Surface = GameObject.Find("Surface");
        timerIndicator = GameObject.Find("TimerIndicator").GetComponent<TimerIndicator>();
        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();

        // Scoreboard is now updated by the billiards class
        scoreboard.AllowManualSet = true;

        cameraTilt = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_list_camera_tilt"));
        surfaceTilt = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_list_surface_tilt"));
        //cameraTilt -= surfaceTilt; // As surfaceTilt rotates the entire prefab, this line makes creating the json more intuitive 

        // Whether or not this is a practice trial 
        // replaces scoreboard with 'Practice Round', doesn't record score
        trackScore = (ctrler.Session.CurrentBlock.settings.GetBool("per_block_track_score"));
        if (!trackScore)
        {
            scoreboard.ScorePrefix = false;
            scoreboard.ManualScoreText = "Practice Round";
        }
    }

    /// <summary>
    /// Sets the target's position using targetAngle and TARGET_DISTANCE
    /// </summary>
    /// <param name="targetAngle">Angle to set the target (usually from JSON)</param>
    protected virtual void SetTargetPosition(float targetAngle)
    {
        // initializes the position
        Target.transform.position = new Vector3(0f, 0.065f, 0f);
        //rotates the object
        Target.transform.rotation = Quaternion.Euler(0f, -targetAngle + 90f, 0f);
        //moves object forward towards the direction it is facing
        Target.transform.position += Target.transform.forward.normalized * TARGET_DISTANCE;
    }

    protected void DynamicTilt(float t, GameObject cam, GameObject XRRig, GameObject XRPosLock)
    {
        float tilt = 0;
        float camtilt = 0;

        // set up curve type for tilt to follow
        switch (ctrler.Session.CurrentTrial.settings.GetString("per_block_dynamic_tilt_curve"))
        {
            case "default":
                tilt = t;
                break;
            case "sin": //Starts at 0, goes to surface tilt, goes to -surface tilt, goes to 0
                tilt = Mathf.Sin(t * Mathf.PI * 2);
                break;
            case "cos": //Starts at 0, goes to surface tilt, goes to 0
                tilt = Mathf.Sin(t * Mathf.PI);
                break;
            case "linear":
                tilt = t;
                break;
            case "quad":
                tilt = t * t;
                break;
            case "easeInElastic":
                float c4 = (float)(2f * Math.PI / 3f);
                if (t == 0 || t == 1)
                    tilt = t;
                else
                    tilt = -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin((t * 10f - 10.75f) * c4);
                break;
            case "easeInOutBack":
                float c1 = 1.70158f;
                float c2 = c1 * 1.525f;

                if (t < 0.5f)
                    tilt = Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2) / 2;
                else
                    tilt = Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2 / 2;
                break;
        }

        camtilt = tilt * cameraTilt;
        tilt *= surfaceTilt;


        Vector3 ball_pos = Home.transform.position + Vector3.up * 0.25f;

        ctrler.room.transform.parent = cam.transform.parent;

        SetDynamicTilt(cam.transform.parent.gameObject, camtilt);


        SetDynamicTilt(Surface.transform.parent.gameObject, tilt); //Tilt surface

        //Tilt VR Camera if needed
        if (ctrler.Session.settings.GetString("experiment_mode") == "pinball_vr")
        {
            //XRRig.transform.RotateAround(Home.transform.position + Vector3.up * 0.25f, pinballSpace.transform.forward,
            //   cameraTilt + surfaceTilt);
            SetDynamicTilt(XRRig, camtilt);
            XRRig.transform.position = XRPosLock.transform.position; // lock position of XR Rig
            //XRCamOffset.transform.position = new Vector3(0, -0.8f, -0.2f);
        }

    }


    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, float angle)
    {
        return Quaternion.Euler(new Vector3(0, 0, angle)) * (point - pivot) + pivot;
    }

    /// <summary>
    /// Rotates obj around axis by angle degrees
    /// </summary>
    /// <param name="obj">Object to be rotated</param>
    /// <param name="point">Point to rotate around</param>
    /// <param name="axis">Object to rotate around</param>
    /// <param name="angle">Angle in degrees of rotation</param>
    [Obsolete("Method is obsolete. Use SetDynamicTilt() instead.", false)]
    protected static void SetTilt(GameObject obj, Vector3 point, GameObject axis, float angle)
    {
        // Decouple object from parent
                Transform parent = obj.transform.parent;
                obj.transform.SetParent(null);

                obj.transform.RotateAround(point, axis.transform.forward, angle);

                // Reparent obj
                obj.transform.SetParent(parent);
    }

    // Rather than using RotateAround to rotate objects around a certain position in SetTilt(), 
    // in SetDynamicTilt(), objects are set to be a child of a parent object that is positioned where the child objects should be rotated around.
    // The parent object is then rotated, rotating all child objects with it. 

    /// <summary>
    /// Rotates obj around axis by angle degrees
    /// </summary>
    /// <param name="obj">Object to be rotated</param>
    /// <param name="angle">Angle in degrees of rotation</param>
    protected static void SetDynamicTilt(GameObject obj, float angle)
    {
        obj.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    protected virtual void SetSurfaceMaterial(Material material)
    {
        if (Surface == null)
        {
            Debug.LogError("Surface was not found in the prefab. Please make sure it is added." +
                           " If Surface exists in the prefab, check if you ran base.Setup()");
        }

        if (material == null)
        {
            Debug.LogError("Material was not found. Check spelling.");
        }

        Surface.GetComponent<MeshRenderer>().material = material;
    }

    /// <summary>
    /// Calculates tempScore based on distance to target.
    /// </summary>
    /// <returns>A float between 0-1 calculated linearly using currDistance, multiplied by MAX_POINTS.</returns>
    /// <param name="currDistance">The current distance between the ball object and the target</param>
    /// <param name="maxDistance">The furthest distance from the target that score should be received for (cannot be 0)</param>
    protected virtual float CalculateScore(float currDistance, float maxDistance, float maxPoints)
    {
        float m = 1 / -maxDistance; // Slope of score calculation, so that currDistance = 0 -> max score, currDistance = maxDistance -> no score

        return Mathf.Round(m * (currDistance - maxDistance) * maxPoints);
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void GetFinalScore()
    {
        
    }

    /// <summary>
    /// Projects mouse onto the surface plane.
    /// (Useful for when the surface plane is tilted)
    /// </summary>
    protected virtual Vector3 GetMousePoint(Transform ball)
    {
        //ToFix: can the below two be one function called point to planepoint?
        if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
        {
            return ctrler.CursorController.MouseToPlanePoint(
                            Surface.transform.up * ball.position.y,
                            ball.position,
                            Camera.main);
        }           
        else
        {
            Vector3 ctrl = new Vector3(ctrler.CursorController.GetHandPosition().x, 3, ctrler.CursorController.GetHandPosition().z);
            return ctrler.CursorController.ControllerToPlanePoint(
                                Surface.transform.up * ball.position.y,
                                ball.position,
                                ctrl);
        }

    }

    protected virtual Vector3 GetControllerPoint(Transform ball)
    {
        Vector3 ctrl = new Vector3(ctrler.CursorController.GetHandPosition().x, 3, ctrler.CursorController.GetHandPosition().z);
        return ctrler.CursorController.ControllerToPlanePoint(
                            Surface.transform.up * ball.position.y,
                            ball.position,
                            ctrl);
    }

    // Turns grid materials on surface invisible until the next round
    protected void ToggleGrid()
    {
        Surface.GetComponent<MeshRenderer>().materials[1].color = new Color(0, 0, 0, 0);
    }
}
