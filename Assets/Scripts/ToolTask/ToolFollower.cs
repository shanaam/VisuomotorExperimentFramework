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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = ObjToFollow.position;

        if (RotateWithObject) transform.rotation = ObjToFollow.rotation;
    }
}
