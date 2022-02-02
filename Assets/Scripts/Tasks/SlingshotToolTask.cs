using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlingshotToolTask : ToolTask
{
    List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    Vector3 pos = new Vector3();

    public override void Setup()
    {
        base.Setup();

        UnityEngine.XR.InputDevices.GetDevicesWithRole(UnityEngine.XR.InputDeviceRole.RightHanded, devices);

        Cursor.visible = false;

        toolObjects.GetComponentInChildren<Collider>().enabled = false;


        slingShotBall.SetActive(true);

        baseObject.GetComponent<ToolObjectScript>().enabled = false;
    }

    public override bool IncrementStep()
    {
        if (currentStep == 0)
        {
            toolObjects.transform.rotation = toolSpace.transform.rotation;
        }

        return base.IncrementStep();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        // Tool follows mouse
        if (currentStep <  1)
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

                if (Vector3.Distance(ctrllerPoint, ballObjects.transform.position) <= 0.05f)
                {
                    IncrementStep();
                }

                break;

            // the user triggers the object 
            case 1:
                BallFollowMouse(baseObject);

                toolObjects.transform.position = new Vector3(Home.transform.position.x, 0.01f, Home.transform.position.z) ;

                float time = 0f;

                // non vr and vr control of the slingshot
                if (ctrler.Session.settings.GetString("experiment_mode") == "tool")
                {
                    Vector3 direc = new Vector3(Home.transform.position.x - mousePoint.x, 0, Home.transform.position.z - mousePoint.z);
                    toolObjects.transform.localRotation = Quaternion.LookRotation(direc);
                    // Line rendere representing the slingshot band is attached to home GameObject
                    Home.GetComponent<LineRenderer>().positionCount = 2;
                    Home.GetComponent<LineRenderer>().SetPosition(0, Home.transform.position);
                    Home.GetComponent<LineRenderer>().SetPosition(1, mousePoint);

                    if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.12f)
                    {
                        time += Time.fixedDeltaTime;
                    }

                    if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.2f)
                    {
                        Vector3 shotDir = Home.transform.position - mousePoint;
                        shotDir /= time;

                        //baseObject.GetComponent<Rigidbody>().velocity = shotDir * 0.2f;
                        pos = toolObjects.transform.position;

                        baseObject.GetComponent<Rigidbody>().velocity = shotDir.normalized * FIRE_FORCE;
                        Home.GetComponent<LineRenderer>().positionCount = 0;
                        sound.Play();
                        IncrementStep();
                    }
                }
                else
                {

                    Vector3 direc = new Vector3(Home.transform.position.x - ctrllerPoint.x, 0, Home.transform.position.z - ctrllerPoint.z);
                    toolObjects.transform.localRotation = Quaternion.LookRotation(direc);
                    // Line rendere representing the slingshot band is attached to home GameObject
                    Home.GetComponent<LineRenderer>().positionCount = 2;
                    Home.GetComponent<LineRenderer>().SetPosition(0, Home.transform.position);
                    Home.GetComponent<LineRenderer>().SetPosition(1, ctrllerPoint);

                    if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.12f)
                    {
                        time += Time.fixedDeltaTime;
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

                    if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.2f)
                    {
                        Vector3 shotDir = Home.transform.position - ctrllerPoint;
                        shotDir /= time;

                        //baseObject.GetComponent<Rigidbody>().velocity = shotDir * 0.2f;

                        pos = toolObjects.transform.position;

                        baseObject.GetComponent<Rigidbody>().velocity = shotDir.normalized * FIRE_FORCE;
                        Home.GetComponent<LineRenderer>().positionCount = 0;
                        sound.Play();
                        IncrementStep();
                    }
                } 
                break;
        }
    }
}
