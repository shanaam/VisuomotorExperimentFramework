using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolObjectScript : MonoBehaviour
{





    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.name == "Tool" &&
            ExperimentController.Instance().CurrentTask.GetCurrentStep == 1)
        {

            col.gameObject.GetComponent<Rigidbody>().velocity = 0.001f *
                col.gameObject.GetComponent<Rigidbody>().velocity.normalized; 

            ExperimentController.Instance().CurrentTask.IncrementStep();
            //Debug.Log(" you hit me");
        }
    }
}
