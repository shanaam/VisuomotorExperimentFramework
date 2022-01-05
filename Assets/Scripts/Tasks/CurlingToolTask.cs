using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurlingToolTask : ToolTask
{
    List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    Vector3 pos = new Vector3();

    public override void Setup()
    {
        base.Setup();

        UnityEngine.XR.InputDevices.GetDevicesWithRole(UnityEngine.XR.InputDeviceRole.RightHanded, devices);

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

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (currentStep < 2)
        {
            ObjectFollowMouse(toolObjects);
        }
        else
        {
            toolObjects.transform.position = pos;
        }
        

        switch (currentStep)
        {
            // initlize the scene 
            case 0:

                ToolLookAtBall();

                baseObject.GetComponent<Rigidbody>().velocity = Vector3.zero;

                if (Vector3.Distance(mousePoint, ballObjects.transform.position) <= 0.05f)
                {
                    IncrementStep();
                }
                else if (Vector3.Distance(ctrllerPoint, ballObjects.transform.position) <= 0.05f)
                {

                    IncrementStep();
                }

                break;

            // the user triggers the object 
            case 1:

                // Ball follows mouse
                ObjectFollowMouse(baseObject);

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

                    foreach (var device in devices)
                    {
                        UnityEngine.XR.HapticCapabilities capabilities;
                        if (device.TryGetHapticCapabilities(out capabilities))
                        {
                            if (capabilities.supportsImpulse)
                            {
                                uint channel = 0;
                                float amplitude = 0.4f;
                                float duration = Time.deltaTime;
                                device.SendHapticImpulse(channel, amplitude, duration);
                            }
                        }
                    }

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
        }
    }
}
