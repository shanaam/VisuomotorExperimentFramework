using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurlingToolTask : ToolTask
{
    public override void Setup()
    {
        base.Setup();

        Cursor.visible = false;

        baseObject.GetComponent<SphereCollider>().material.bounciness = 1f;

        curlingStone.SetActive(true);

        baseObject.transform.position = Home.transform.position;

        //initial distance between target and ball
        InitialDistanceToTarget = Vector3.Distance(Target.transform.position, ballObjects.transform.position);
        InitialDistanceToTarget += 0.15f;

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

                // Ball follows mouse
                Vector3 dir = mousePoint - baseObject.transform.position;
                dir /= Time.fixedDeltaTime;
                baseObject.GetComponent<Rigidbody>().velocity = dir;

                Vector3 startPos = new Vector3();
                Vector3 shotDir = new Vector3();

                float time = 0f;


                if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.12f)
                {
                    time += Time.fixedDeltaTime;
                    startPos = mousePoint;
                }

                if (Vector3.Distance(curlingStone.transform.position, Home.transform.position) > 0.2f)
                {
                    shotDir = startPos - mousePoint;
                    shotDir /= time;
                    baseObject.GetComponent<Rigidbody>().AddForce(-shotDir * 3f);

                    IncrementStep();
                }

                break;
        }
    }
}
