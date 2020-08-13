using UnityEngine;

public class Grabbable : MonoBehaviour
{
    public bool Grabbed { get; private set; }

    void OnCollisionStay(Collision collider)
    {
        Grabbed = ExperimentController.Instance().CursorController.CurrentCollider().name == collider.gameObject.name &&
            ExperimentController.Instance().CursorController.IsTriggerDown();
    }

    void OnCollisionExit(Collision collider)
    {
        Grabbed = false;
    }
}
