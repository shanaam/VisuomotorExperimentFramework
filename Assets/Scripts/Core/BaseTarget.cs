using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A general target that could be expanded into other uses
public class BaseTarget : MonoBehaviour
{
    protected ExperimentController ctrler;

    [SerializeField]
    public bool Collided { get; private set; }
    public Collider CollidedWith { get; private set; }

    /// <summary>
    /// When true, objects attached with this script will not increment the step upon collision
    /// </summary>
    public bool CollisionModeOnly = false;

    private void Start()
    {
        ctrler = ExperimentController.Instance();
    }

    private void AdvanceStep()
    {
        ctrler.CurrentTask.IncrementStep();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enabled) return;

        if (!CollisionModeOnly)
        {
            switch (collision.gameObject.tag)
            {
                default:
                    Debug.LogWarning("Tag not implemented");
                    break;
            }
        }

        Collided = true;
        CollidedWith = collision.collider;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;

        if (!CollisionModeOnly)
        {
            switch (other.gameObject.tag)
            {
                case "Hand":
                    AdvanceStep();
                    break;
                case "Car":
                    AdvanceStep();
                    break;
                default:
                    Debug.LogWarning("Tag not implemented");
                    break;
            }
        }

        Collided = true;
        CollidedWith = other;
    }

    private void OnCollisionExit(Collision collision)
    {
        Collided = false;
        CollidedWith = null;
    }

    private void OnTriggerExit(Collider other)
    {
        Collided = false;
        CollidedWith = null;
    }
}
