using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurlingToolTask : ToolTask
{
    
    Vector3 pos = new Vector3();
    Vector3 look = new Vector3();
    private LTDescr d;
    private int id;

    public override void Setup()
    {
        base.Setup();

        look = new Vector3(Home.transform.position.x, Home.transform.position.y, 0);



        Cursor.visible = false;

        baseObject.GetComponent<SphereCollider>().material.bounciness = 1f;

        curlingStone.SetActive(true);
        //Home.transform.position = new Vector3(Home.transform.position.x, Home.transform.position.y, -0.2f);
        //ballObjects.transform.position = new Vector3(ballObjects.transform.position.x, ballObjects.transform.position.y, -0.2f);
        //curlingStone.transform.position = new Vector3(curlingStone.transform.position.x, curlingStone.transform.position.y, -0.2f);


        baseObject.GetComponent<ToolObjectScript>().enabled = false;
        baseObject.SetActive(false);
    }

    public override bool IncrementStep()
    {
        if (currentStep == 0)
        {
            baseObject.SetActive(true);
            toolObjects.transform.rotation = toolSpace.transform.rotation;
        }

        return base.IncrementStep();
    }

    protected void Animate()
    {
        id = LeanTween.rotateY(toolObjects, 0, 0.3f).id;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();


        switch (currentStep)
        {
            // initlize the scene 
            case 0:
                ObjectFollowMouse(toolObjects, Vector3.zero);
                ToolLookAtBall();

                baseObject.GetComponent<Rigidbody>().velocity = Vector3.zero;

                if (Vector3.Distance(mousePoint, ballObjects.transform.position) <= 0.01f)
                {
                    Animate();
                    IncrementStep();
                    ToolLookAtBall();
                }
                else if (Vector3.Distance(ctrllerPoint, ballObjects.transform.position) <= 0.01f)
                {
                    Animate();
                    IncrementStep();
                    ToolLookAtBall();
                }

                break;

            // the user triggers the object 
            case 1:

                ObjectFollowMouse(toolObjects, Vector3.zero);
                //Ball follows mouse
                ObjectFollowMouse(baseObject, Vector3.zero);

                d = LeanTween.descr(id);
                if (d == null)
                {
                    toolObjects.transform.LookAt(look, toolSpace.transform.up);
                }

                pos = toolObjects.transform.position;

                Vector3 startPos = new Vector3();
                Vector3 shotDir = new Vector3();

                float time = 0f;
                //non vr and vr control of the curling
                if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
                {
                    if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.12f)
                    {
                        time += Time.fixedDeltaTime;
                        startPos = mousePoint;
                    }

                    if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.2f)
                    {
                        shotDir = startPos - mousePoint;
                        shotDir /= time;
                        baseObject.GetComponent<Rigidbody>().AddForce(-shotDir.normalized * FIRE_FORCE);
                        pos = toolObjects.transform.position;
                        IncrementStep();
                    }
                }
                else
                {
                    if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.12f)
                    {
                        time += Time.fixedDeltaTime;
                        startPos = ctrllerPoint;
                    }

                    // Vibrate controller (scaled to velocity)
                    if (toolObjects.GetComponent<Rigidbody>().velocity.magnitude > 0.01f)
                        VibrateController(0, Mathf.Lerp(0.1f, 0.3f, toolObjects.GetComponent<Rigidbody>().velocity.magnitude / 10f), Time.deltaTime, devices);
                    //VibrateController(0, 0.2f, Time.deltaTime, devices);

                    if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.2f)
                    {
                        shotDir = startPos - ctrllerPoint;
                        shotDir /= time;
                        baseObject.GetComponent<Rigidbody>().AddForce(-shotDir.normalized * FIRE_FORCE);
                        pos = toolObjects.transform.position;
                        IncrementStep();
                    }
                }
                break;
            case 2:
                toolObjects.transform.position = pos;
                break;
            case 3:
                toolObjects.transform.position = pos;
                break;
        }
    }
}
