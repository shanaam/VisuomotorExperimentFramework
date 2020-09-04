using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolObjectScript : MonoBehaviour
{
    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.name == "Tool" && ExperimentController.Instance().CurrentTask.GetCurrentStep == 0)
        {
            ExperimentController.Instance().CurrentTask.IncrementStep();
        }
    }
}
