using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SlingshotToolTask : ToolTask
{
    Vector3 pos = new Vector3();
    private Vector3 ball_start_pos;
    private Vector3 shotDir;
    private bool fired;

    public override void Setup()
    {
        base.Setup();

        Cursor.visible = false;

        toolObjects.GetComponentInChildren<Collider>().enabled = false;

        toolObjects.transform.position = baseObject.transform.position;

        // activate the slingshot ball
        slingShotBall.SetActive(true);

        // store this starting position to move back to later
        ball_start_pos = slingShotBall.transform.position;

        baseObject.GetComponent<ToolObjectScript>().enabled = false;
    }

    /// <summary>
    /// Increments step
    /// </summary>
    public override bool IncrementStep()
    {
        if (currentStep == 0)
        {
            toolObjects.transform.rotation = toolSpace.transform.rotation;
        }
        // sets fired to false at the start of every case 1
        if (currentStep == 1)
        {
            fired = false;
        }

        return base.IncrementStep();
    }


    /// <summary>
    /// Animate the ball back to start position then run the next function
    /// FIX: the time is arbritrary atm. Figure out what the velocity applied is.
    /// </summary>
    protected void AnimateBallToHome()
    {
        LeanTween.move(baseObject, ball_start_pos, .10f).setOnComplete(FireAndIncrement);
    }

    /// <summary>
    /// This applies force, and rotation if stated in the json, then increments
    /// </summary>
    protected void FireAndIncrement()
    {
        shotDir = shotDir.normalized;
        // Apply rotation if necessary
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated")
        {
            shotDir = RotateShot(shotDir);
        }

        // record and apply launch velocity
        launchAngle = Vector2.SignedAngle(new Vector2(1f, 0f), new Vector2(shotDir.x, shotDir.z));

        // run the FireBilliadsBall function from the BilliardsBallBehaviour script
        baseObject.GetComponent<BilliardsBallBehaviour>().FireBilliardsBall(shotDir, FIRE_FORCE);

        IncrementStep();
    }

    /// <summary>
    /// sets the endpoints of the elastic to the sides of the slingshot tool
    /// </summary>
    protected void SetElastic()
    {
        elasticL.SetPosition(1, selectedObject.transform.GetChild(0).gameObject.transform.position);
        elasticR.SetPosition(1, selectedObject.transform.GetChild(1).gameObject.transform.position);
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        SetElastic();

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

                baseObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
                break;

            // the user triggers the object 
            case 1:
                //as long as the ball is not fired the slingshot and ball will follow the mouse or vr controller
                if (!fired)
                {
                    BallFollowMouse(baseObject, toolOffset);
                    ObjectFollowMouse(toolObjects, toolOffset);
                }
                else
                {
                    // previously in case 2 (can take out of there maybe)
                    if (ballObjects.transform.position.z < Home.transform.position.z)
                    {
                        toolObjects.transform.position = ballObjects.transform.position;
                    }

                    else
                    {
                        toolObjects.transform.position = Home.transform.position;
                    }
                }

                // CHECK: what is this being used for?
                float time = 0f;

                // non vr and vr control of the slingshot
                if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
                {
                    FireCondition(mousePoint, time);
                }
                else
                {
                    FireCondition(ctrllerPoint, time);
                }
                break;
            case 2:
                //makes sure the tool does not follow anything after being fired
                toolObjects.transform.position = Home.transform.position;
                break;

            case 3:
                //makes sure the tool does not follow anything after being fired
                toolObjects.transform.position = Home.transform.position;
                break;
        }
    }

    /// <summary>
    /// This method is used to check the firing condition of the slingshot.
    /// it checks the position of the slingshot relative to the home position
    /// if the slingshot gets 0.17 units away from the home position the slingshot is fired
    /// a sound plays and the vr controller vibrates when fired.
    /// When not fired the vr controller vibrates according to how far away it is from the 
    /// home position. 
    /// </summary>
    /// <param name="pos">The position of the mouse or the vr controller.</param>
    /// <param name="time">TBH filled in</param>
    protected void FireCondition(Vector3 pos, float time)
    {
        Vector3 direc = new Vector3(Home.transform.position.x - toolObjects.transform.position.x, 0, Home.transform.position.z - pos.z);
        toolObjects.transform.localRotation = Quaternion.LookRotation(direc);

        if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.12f)
        {
            time += Time.fixedDeltaTime;
        }

        // fire condition (when we reach a threshold)
        if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.17f && !fired)
        {
            VibrateController(0, 1f, Time.deltaTime * 4, devices);
            shotDir = Home.transform.position - mousePoint;
            shotDir /= time;

            //baseObject.GetComponent<Rigidbody>().velocity = shotDir * 0.2f;
            pos = toolObjects.transform.position;

            // play sound and increment
            sound.Play();
            fired = true;
            AnimateBallToHome();
        }
        else
        {
            // Vibrate controller (scaled to distance from home)
            VibrateController(0, Mathf.Lerp(0.01f, 0.3f, Vector3.Distance(slingShotBall.transform.position, Home.transform.position) * 4f), Time.deltaTime, devices);
        }

    }
}
