using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public abstract class BilliardsTask : BaseTask
{
    public GameObject Surface;

    public override void Setup()
    {
        Surface = GameObject.Find("Surface");
    }

    /// <summary>
    /// Rotates obj around axis by angle degrees
    /// </summary>
    /// <param name="obj">Object to be rotated</param>
    /// <param name="axis">Object to rotate around</param>
    /// <param name="angle">Angle in degrees of rotation</param>
    protected static void SetTilt(GameObject obj, GameObject axis, float angle)
    {
        // Decouple object from parent
        Transform parent = obj.transform.parent;
        obj.transform.SetParent(null);

        obj.transform.RotateAround(axis.transform.position, axis.transform.forward, angle);

        obj.transform.SetParent(parent);
    }

    protected virtual void SetSurfaceMaterial(Material material)
    {
        if (Surface == null)
        {
            Debug.LogError("Surface was not found in the prefab. Please make sure it is added.");
        }

        Surface.GetComponent<MeshRenderer>().material = material;
    }
}
