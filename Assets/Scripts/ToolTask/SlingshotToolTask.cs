using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlingshotToolTask : ToolTask
{
    public override void Setup()
    {
        base.Setup();

        baseObject.transform.position = Home.transform.position;

        //initial distance between target and ball
        InitialDistanceToTarget = Vector3.Distance(Target.transform.position, ballObjects.transform.position);
        InitialDistanceToTarget += 0.15f;

        impactBox.SetActive(false);
        puckobj.SetActive(false);
        curlingStone.SetActive(false);

        baseObject.GetComponent<MeshRenderer>().enabled = false;
        baseObject.GetComponent<ToolObjectScript>().enabled = false;
    }

    public override bool IncrementStep()
    {
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

                    baseObject.GetComponent<Rigidbody>().velocity = shotDir * 0.1f;
                    Home.GetComponent<LineRenderer>().positionCount = 0;

                    IncrementStep();
                }
                break;
        }
    }
}
