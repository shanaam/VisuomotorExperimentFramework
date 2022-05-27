using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolFollower : MonoBehaviour
{
    /* 
     * For objects (ball, puck, other ball, other ball, etc.) in ToolTask that are basically just MeshRenderers 
     * that need to match the location of BaseObject.
     */

    // BaseObject goes here
    [Tooltip("BaseObject goes here")]
    public Transform ObjToFollow;

    // Ball objects rotate with BaseObject, puck does not because that would be weird
    // (The reason this script exists and these objects aren't just children of BaseObject)
    [Tooltip("Set to false on puck and puck-like objects")]
    public bool RotateWithObject;

    Vector3 vel = new Vector3();
    Vector3 prev = new Vector3();
    Vector3 cur = new Vector3();
    float dist;
    float angle;
    Vector3 rotationAxis = new Vector3();

    // Update is called once per frame
    void Update()
    {
        cur = transform.position;
        vel = (cur - prev) / Time.deltaTime;
        dist = vel.magnitude;
        angle = dist * (180f / Mathf.PI) / 2.5f;
        prev = transform.position;
        //Debug.Log(vel);
        rotationAxis = Vector3.Cross(Vector3.up, vel).normalized;
        if (dist > 0.001f)
        {
            ObjToFollow.localRotation = Quaternion.Euler(rotationAxis * angle) * ObjToFollow.localRotation;
        }
        transform.position = ObjToFollow.position;


        if (RotateWithObject) transform.rotation = ObjToFollow.rotation;
    }
}
