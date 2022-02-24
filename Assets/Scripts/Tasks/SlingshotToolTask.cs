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

    // Increments step
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
    
    // Animate the ball back to start position then run the next function
    // FIX: the time is arbritrary atm. Figure out what the velocity applied is.
    protected void AnimateBallToHome()
    {
        LeanTween.move(baseObject, ball_start_pos, .10f).setOnComplete(FireAndIncrement);
    }

    // This applies force, then increments
    protected void FireAndIncrement()
    {
        shotDir = shotDir.normalized ;
        // Apply rotation if necessary
        if (ctrler.Session.CurrentBlock.settings.GetString("per_block_type") == "rotated")
        {
            float angle = ctrler.Session.CurrentTrial.settings
                .GetFloat("per_block_rotation");

            shotDir = Quaternion.Euler(0f, -angle, 0f) * shotDir;
        }

        // record and apply launch velocity
        launchAngle = Vector2.SignedAngle(new Vector2(1f, 0f), new Vector2(shotDir.x, shotDir.z));
        baseObject.GetComponent<Rigidbody>().velocity = shotDir * FIRE_FORCE;

        IncrementStep();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        elasticL.SetPosition(1, selectedObject.transform.GetChild(0).gameObject.transform.position);
        elasticR.SetPosition(1, selectedObject.transform.GetChild(1).gameObject.transform.position);


        switch (currentStep)
        {
            // initlize the scene 
            case 0:
                //ObjectFollowMouse(toolObjects, Vector3.zero);
                //ToolLookAtBall();

                toolObjects.GetComponent<Rigidbody>().velocity = Vector3.zero;

                if (Vector3.Distance(mousePoint, toolObjects.transform.position) <= 0.05f && Input.GetMouseButton(0))
                {
                    toolOffset = mousePoint - toolObjects.transform.position;
                    IncrementStep();
                }
                // conditions for grabbing the object in VR

                if (Vector3.Distance(ctrllerPoint, toolObjects.transform.position) <= 0.07f)
                {
                    VibrateController(0, 0.2f, Time.deltaTime, devices);
                }

                // grab object
                if (Vector3.Distance(ctrllerPoint, toolObjects.transform.position) <= 0.07f && ctrler.CursorController.IsTriggerDown())
                {
                    VibrateController(0, 0.34f, Time.deltaTime, devices);
                    toolOffset = ctrllerPoint - toolObjects.transform.position;
                    IncrementStep();
                }


                baseObject.GetComponent<Rigidbody>().velocity = Vector3.zero;

/*                if (Vector3.Distance(mousePoint, ballObjects.transform.position) <= 0.05f)
                {
                    IncrementStep();
                }

                if (Vector3.Distance(ctrllerPoint, ballObjects.transform.position) <= 0.05f)
                {
                    IncrementStep();
                }
*/
                break;

            // the user triggers the object 
            case 1:
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
                    Vector3 direc = new Vector3(Home.transform.position.x - toolObjects.transform.position.x, 0, Home.transform.position.z - mousePoint.z);
                    toolObjects.transform.localRotation = Quaternion.LookRotation(direc);

                    if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.12f)
                    {
                        time += Time.fixedDeltaTime;
                    }

                    // fire condition (when we reach a threshold)
                    if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.17f && !fired)
                    {
                        shotDir = Home.transform.position - mousePoint;
                        shotDir /= time;

                        //baseObject.GetComponent<Rigidbody>().velocity = shotDir * 0.2f;
                        pos = toolObjects.transform.position;

                        // play sound and increment
                        sound.Play();
                        fired = true;
                        AnimateBallToHome();
                    }
                }
                else
                {

                    Vector3 direc = new Vector3(Home.transform.position.x - toolObjects.transform.position.x, 0, Home.transform.position.z - ctrllerPoint.z);
                    toolObjects.transform.localRotation = Quaternion.LookRotation(direc);

                    if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.12f)
                    {
                        time += Time.fixedDeltaTime;
                    }

                    

                    if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.17f && !fired)
                    {
                        // Vibrate controller before firing
                        VibrateController(0, 1f, Time.deltaTime * 4, devices);

                        shotDir = Home.transform.position - ctrllerPoint;
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
                break;
            case 2:

                toolObjects.transform.position = Home.transform.position;

                //if (toolObjects.transform.position.z > Home.transform.position.z)
                //{
                //    toolObjects.transform.position = ballObjects.transform.position;
                //}

                //else
                //{
                //    toolObjects.transform.position = Home.transform.position;
                //}


                break;
            case 3:
                toolObjects.transform.position = Home.transform.position;
                break;
        }
    }
}
