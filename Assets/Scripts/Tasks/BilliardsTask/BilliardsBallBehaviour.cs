using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// For objects that behave as billiards balls
// This script is attached to the BallObjects GameObject -- the parent of the ball/puck meshes
public class BilliardsBallBehaviour : MonoBehaviour
{
    public void FireBilliardsBall(Vector3 shotDir, float forceMultiplier = 1f)
    {
        // set the velocity of the ball to the shotDir
        GetComponent<Rigidbody>().velocity = shotDir * forceMultiplier;

        // log the velocity of the ball
        Debug.Log("Ball fired! Velocity: " + GetComponent<Rigidbody>().velocity);
    }

    [ContextMenu("FireTest")]
    public void FireTest()
    {
        FireBilliardsBall(new Vector3(0, 0, 5));
    }
}
