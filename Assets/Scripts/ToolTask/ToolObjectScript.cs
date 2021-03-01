using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolObjectScript : MonoBehaviour
{

    void OnTriggerEnter(Collider col)
    {

        if (col.gameObject.name == "Tool" &&
            ExperimentController.Instance().CurrentTask.GetCurrentStep == 1)
        {
            ExperimentController.Instance().CurrentTask.IncrementStep();
            //Debug.Log(" you hit me");
        }
    }
}
