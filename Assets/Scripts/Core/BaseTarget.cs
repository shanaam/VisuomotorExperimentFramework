using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A general target that could be expanded into other uses
public class BaseTarget : MonoBehaviour
{
    ExperimentController ctrler;

    private void Start()
    {
        ctrler = ExperimentController.Instance();
    }

    private void AdvanceStep()
    {
        ctrler.CurrentTask.IncrementStep();
    }

    private void OnTriggerEnter(Collider other)
    {
        switch (other.gameObject.tag)
        {
            case "Hand":
                AdvanceStep();
                break;
            default:
                Debug.LogWarning("Tag not implemented");
                break;
        }
    }
}
