using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactToolTask : ToolTask
{

    public override void Setup()
    {
        base.Setup();

        impactBox.GetComponent<BoxCollider>().material.bounciness = 1f;
        impactBox.GetComponent<BoxCollider>().enabled = false;

        curlingStone.SetActive(false);
        slingShotBall.SetActive(false);

        string puck_type = Convert.ToString(ctrler.PollPseudorandomList("per_block_list_puck_type"));

        // set up puck type 
        if (puck_type == "puck")
        {
            baseObject.GetComponent<MeshRenderer>().enabled = false;
            ballObjects.GetComponent<ToolFollower>().RotateWithObject = false;
        }
        else if (puck_type == "ball")
        {
            puckobj.SetActive(false);
        }

        baseObject.GetComponent<SphereCollider>().material.bounciness = 0.8f;

        //initial distance between target and ball
        InitialDistanceToTarget = Vector3.Distance(Target.transform.position, ballObjects.transform.position);
        InitialDistanceToTarget += 0.15f;

        // Disable object(puck) for first step
        baseObject.SetActive(false);
    }

    public override bool IncrementStep()
    {
        if (currentStep == 0)
        {
            baseObject.SetActive(true);
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
                impactBox.GetComponent<Rigidbody>().velocity = Vector3.zero;

                if (Vector3.Distance(mousePoint, impactBox.transform.position) <= 0.05f)
                {
                    IncrementStep();
                }

                break;

            // the user triggers the object 
            case 1:
                Vector3 dir = mousePoint - impactBox.transform.position;
                dir /= Time.fixedDeltaTime;
                impactBox.GetComponent<Rigidbody>().velocity = dir;


                // Rotate the impact: always looking at the puck when close enough 
                if (Vector3.Distance(impactBox.transform.position, baseObject.transform.position) < 0.2f)
                {
                    impactBox.transform.LookAt(baseObject.transform);
                }
                else
                {
                    impactBox.transform.rotation = Quaternion.identity;
                }

                impactBox.GetComponent<Collider>().enabled = mousePoint.z <= 0.05f;


                break;

            // After the user hits the object
            // Used to determine if the triggerd object is heading away from the target or not
            case 2:
                dir = mousePoint - impactBox.transform.position;
                dir /= Time.fixedDeltaTime;
                impactBox.GetComponent<Rigidbody>().velocity = dir;

                impactBox.GetComponent<Collider>().enabled = mousePoint.z <= 0.05f;

                break;

            // after we either hit the Target or passed by it
            case 3:
                //freeze impact box 
                impactBox.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                impactBox.GetComponent<Rigidbody>().isKinematic = true;

                break;
        }
    }
}
