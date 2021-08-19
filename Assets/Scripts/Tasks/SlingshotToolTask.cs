using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlingshotToolTask : ToolTask
{
    public override void Setup()
    {
        base.Setup();

        Cursor.visible = false;

        toolObjects.GetComponentInChildren<Collider>().enabled = false;
        baseObject.transform.position = Home.transform.position;

        //initial distance between target and ball
        InitialDistanceToTarget = Vector3.Distance(Target.transform.position, ballObjects.transform.position);
        InitialDistanceToTarget += 0.15f;

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
        Vector3 toolDir = mousePoint - toolObjects.transform.position;
        toolDir /= Time.fixedDeltaTime;
        toolObjects.GetComponent<Rigidbody>().velocity = toolDir;

        switch (currentStep)
        {
            // initlize the scene 
            case 0:

                // Rotate the tool: always looking at the ball when close enough 
                if (Vector3.Distance(toolObjects.transform.position, baseObject.transform.position) < 0.2f)
                {
                    toolObjects.transform.LookAt(baseObject.transform, toolSpace.transform.up);
                }
                else
                {
                    toolObjects.transform.rotation = toolSpace.transform.rotation;
                }

                baseObject.GetComponent<Rigidbody>().velocity = Vector3.zero;

                if (Vector3.Distance(mousePoint, ballObjects.transform.position) <= 0.05f)
                {
                    IncrementStep();
                }

                break;

            // the user triggers the object 
            case 1:
                Vector3 dir = mousePoint - baseObject.transform.position;
                dir /= Time.fixedDeltaTime;
                baseObject.GetComponent<Rigidbody>().velocity = dir;

                float time = 0f;

                // Line rendere representing the slingshot band is attached to home GameObject
                Home.GetComponent<LineRenderer>().positionCount = 2;
                Home.GetComponent<LineRenderer>().SetPosition(0, Home.transform.position);
                Home.GetComponent<LineRenderer>().SetPosition(1, mousePoint);


                if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.12f)
                {
                    time += Time.fixedDeltaTime;
                }

                if (Vector3.Distance(slingShotBall.transform.position, Home.transform.position) > 0.25f)
                {
                    Vector3 shotDir = Home.transform.position - mousePoint;
                    shotDir /= time;

                    baseObject.GetComponent<Rigidbody>().velocity = shotDir * 0.2f;
                    Home.GetComponent<LineRenderer>().positionCount = 0;

                    IncrementStep();
                }
                break;
        }
    }
}
