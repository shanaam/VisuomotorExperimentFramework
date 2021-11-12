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
    protected const float TARGET_DISTANCE = 1.0f; // Target distance from home

    public override void Setup()
    {
        Surface = GameObject.Find("Surface");
        timerIndicator = GameObject.Find("TimerIndicator").GetComponent<TimerIndicator>();
        scoreboard = GameObject.Find("Scoreboard").GetComponent<Scoreboard>();

        // Scoreboard is now updated by the billiards class
        scoreboard.AllowManualSet = true;

        cameraTilt = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_list_camera_tilt"));
        surfaceTilt = Convert.ToSingle(ctrler.PollPseudorandomList("per_block_list_surface_tilt"));
        cameraTilt -= surfaceTilt; // As surfaceTilt rotates the entire prefab, this line makes creating the json more intuitive 

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


    /// <summary>
    /// Rotates obj around axis by angle degrees
    /// </summary>
    /// <param name="obj">Object to be rotated</param>
    /// <param name="point">Point to rotate around</param>
    /// <param name="axis">Object to rotate around</param>
    /// <param name="angle">Angle in degrees of rotation</param>
    protected static void SetTilt(GameObject obj, Vector3 point, GameObject axis, float angle)
    {
        // Decouple object from parent
        Transform parent = obj.transform.parent;
        obj.transform.SetParent(null);

        obj.transform.RotateAround(point, axis.transform.forward, angle);
        
        // Reparent obj
        obj.transform.SetParent(parent);
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
        return ctrler.CursorController.MouseToPlanePoint(
                            Surface.transform.up * ball.position.y,
                            ball.position,
                            Camera.main);
    }
}
