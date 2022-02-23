using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactToolTask : ToolTask
{

    Vector3 pos = new Vector3();
    bool hasHit = false;
    private Vector3 shotDir;

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
                //shotDir = toolObjects.GetComponent<Rigidbody>().velocity;
                shotDir = Vector3.ClampMagnitude(toolObjects.GetComponent<Rigidbody>().velocity, FIRE_FORCE);

                // apply rotation if necessary
                if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated")
                {
                    float angle = ctrler.Session.CurrentTrial.settings
                        .GetFloat("per_block_rotation");

                    shotDir = Quaternion.Euler(0f, -angle, 0f) * shotDir;
                }

                // apply shotDir
                baseObject.GetComponent<Rigidbody>().velocity = shotDir;

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

                if (Vector3.Distance(mousePoint, toolObjects.transform.position) <= 0.05f && Input.GetMouseButton(0))
                {
                    toolOffset = mousePoint - toolObjects.transform.position;
                    IncrementStep();
                }
                if (Vector3.Distance(ctrllerPoint, toolObjects.transform.position) <= 0.1f && ctrler.CursorController.IsTriggerDown())
                {
                    VibrateController(0, 0.34f, Time.deltaTime, devices);
                    toolOffset = ctrllerPoint - toolObjects.transform.position;
                    IncrementStep();
                }

                break;

            // the user triggers the object 
            case 1:

                // Tool follows mouse
                ObjectFollowMouse(toolObjects, toolOffset);

                if (toolObjects.GetComponent<Rigidbody>().velocity.magnitude > 0.5f) 
                    VibrateController(0, Mathf.Lerp(0.1f, 0.2f, toolObjects.GetComponent<Rigidbody>().velocity.magnitude / 10f), Time.deltaTime, devices);

                ToolLookAtBall();

                // non vr and vr turning on the collider on the tool
                if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
                {
                    toolObjects.GetComponentInChildren<Collider>().enabled = mousePoint.z <= 0.05f;
                }
                    
                else
                {
                    toolObjects.GetComponentInChildren<Collider>().enabled = ctrllerPoint.z <= 0.05f;
                }

                pos = toolObjects.transform.position;
                break;

            // After the user hits the object
            // Used to determine if the triggerd object is heading away from the target or not
            case 2:
                
                if (!hasHit)
                {
                    sound.Play();


                    Debug.Log(VibrateController(0, Mathf.Lerp(0.5f, 1f, toolObjects.GetComponent<Rigidbody>().velocity.magnitude / 10f), Time.deltaTime * 3, devices));


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
