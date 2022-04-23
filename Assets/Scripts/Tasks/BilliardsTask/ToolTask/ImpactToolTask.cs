using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactToolTask : ToolTask
{

    Vector3 pos = new Vector3();
    bool hasHit = false;
    private Vector3 shotDir;
    private Vector3 lastForward_toolDir;
    private Vector3 toolDir;

    private const float MAX_MAGNITUDE = 6f;

    public override void Setup()
    {
        base.Setup();

        toolObjects.GetComponentInChildren<Collider>().material.bounciness = 1f;
        toolObjects.GetComponentInChildren<Collider>().enabled = false;

        string puck_type = Convert.ToString(ctrler.PollPseudorandomList("per_block_list_puck_type"));

        // set up puck type 
        if (puck_type == "puck")
        {
            ballObjects.GetComponent<ToolFollower>().RotateWithObject = false; // So puck doesn't roll like a ball
            puckobj.SetActive(true);
        }
        else if (puck_type == "ball")
        {
            ballobj.SetActive(true);
        }

        baseObject.GetComponent<SphereCollider>().material.bounciness = 0.8f;

        // Disable object(puck) for first step
        baseObject.SetActive(false);
    }

    public override bool IncrementStep()
    {
        switch (currentStep)
        {
            case 0:
                baseObject.SetActive(true);
                Cursor.visible = false;
                break;

            case 1:
                // set shotDir to the velocity of the tool
                shotDir = lastForward_toolDir * (FIRE_FORCE * 0.5f);

                // if magnitude of shotDir > #, then cap it at max Magnitude     
                if (shotDir.magnitude > MAX_MAGNITUDE)
                {
                    //normalizing the vector and then multiplying by the max_magnitude
                    shotDir.Normalize();
                    shotDir = shotDir * MAX_MAGNITUDE;
                }

                // apply rotation if necessary
                if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated")
                {
                    shotDir = RotateShot(shotDir);
                }

                // record and apply shotDir
                launchAngle = Vector2.SignedAngle(new Vector2(1f, 0f), new Vector2(shotDir.x, shotDir.z));
                // run the FireBilliardsBall function from the BilliardsBallBehaviour script
                baseObject.GetComponent<BilliardsBallBehaviour>().FireBilliardsBall(shotDir);


                toolObjects.transform.rotation = toolSpace.transform.rotation;

                toolObjects.GetComponentInChildren<Collider>().enabled = false;
                break;
        }

        return base.IncrementStep();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        switch (currentStep)
        {
            // initlize the scene 
            case 0:
                toolObjects.GetComponent<Rigidbody>().velocity = Vector3.zero;

                // when close to the tool the controller vibrates
                if (Vector3.Distance(mousePoint, toolObjects.transform.position) <= 0.07f)
                {
                    // To Fix: the above positions are casted to the plane, y doesn't matter
                    VibrateController(0, 0.2f, Time.deltaTime, devices);
                }

                // grab object
                if (Vector3.Distance(mousePoint, toolObjects.transform.position) <= 0.07f && (Input.GetMouseButton(0) || ctrler.CursorController.IsTriggerDown()))
                {
                    VibrateController(0, 0.34f, Time.deltaTime, devices);
                    toolOffset = mousePoint - toolObjects.transform.position;
                    IncrementStep();
                }

                break;

            // the user triggers the object 
            case 1:
                // Tool follows mouse
                ObjectFollowMouse(toolObjects, toolOffset);

                // Vibrate controller scaled to velocity
                if (toolObjects.GetComponent<Rigidbody>().velocity.magnitude > 0.5f)
                    VibrateController(0, Mathf.Lerp(0.1f, 0.2f, toolObjects.GetComponent<Rigidbody>().velocity.magnitude / 10f), Time.deltaTime, devices);

                ToolLookAtBall();

                toolDir = toolObjects.GetComponent<Rigidbody>().velocity;

                // also get distance
                float ball_tool_distance = Vector3.Magnitude(toolObjects.transform.position - ballObjects.transform.position);

                // only update this if moving forward
                if (toolDir.z > 0.1f && Vector3.Magnitude(toolDir) > Vector3.Magnitude(lastForward_toolDir) && ball_tool_distance < 0.10f)
                {
                    lastForward_toolDir = toolDir;
                }

                // non vr and vr turning on the collider on the tool
                // CHECK IF THIS IS STILL NECESSARY
                toolObjects.GetComponentInChildren<Collider>().enabled = mousePoint.z <= 0.05f;

                pos = toolObjects.transform.position;
                break;

            // After the user hits the object
            // Used to determine if the triggerd object is heading away from the target or not
            case 2:

                if (!hasHit)
                {
                    sound.Play();
                    hasHit = true;
                }
                toolObjects.transform.position = pos;
                break;

            // after we either hit the Target or passed by it
            case 3:
                //freeze impact box 
                toolObjects.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                toolObjects.GetComponent<Rigidbody>().isKinematic = true;

                break;
        }
    }
}
