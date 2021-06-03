using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolObjectScript : MonoBehaviour
{

    void FixedUpdate()
    {
        if (ExperimentController.Instance().CurrentTask.GetCurrentStep == 1 &&
            GetComponent<Rigidbody>().velocity.sqrMagnitude > 0.0f)
        {
            ExperimentController.Instance().CurrentTask.IncrementStep();
        }
    }

    void OnTriggerEnter(Collider col)
    {

        if ( (col.gameObject.name == "ToolBox" || col.gameObject.name == "ToolSphere") &&
            ExperimentController.Instance().CurrentTask.GetCurrentStep == 1)
        {
            //ExperimentController.Instance().CurrentTask.IncrementStep();
            Debug.Log(" you hit me");
        }
    }
}
