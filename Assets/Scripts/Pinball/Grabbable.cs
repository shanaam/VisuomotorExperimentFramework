using UnityEngine;

public class Grabbable : MonoBehaviour
{
    public bool Grabbed { get; private set; }

    //void OnCollisionStay(Collision collider)
    //{
    //    {
    //        collider_name = collider.gameObject.name;
    //        Grabbed = ExperimentController.Instance().CursorController.CurrentCollider().name == collider.gameObject.name &&
    //            ExperimentController.Instance().CursorController.IsTriggerDown();

    //        Debug.Log(collider_name);
    //    }

    //}

    void OnTriggerStay(Collider other)
    {
        // set Grabbed to True when trigger is down AND cursor model is inside this collider
        Grabbed = "RightHandCollider" == other.gameObject.name &&
            ExperimentController.Instance().CursorController.IsTriggerDown();
    }

    void OnTriggerExit(Collider other)
    {
        Grabbed = false;
    }

    private void Update()
    {
        if (ExperimentController.Instance().CursorController.triggerUp)
        {
            //Debug.Log("Trigger Up");
            Grabbed = false;
        }
    }         
}
