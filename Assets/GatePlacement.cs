using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GatePlacement : MonoBehaviour
{
    public List<Mesh> mesh = new List<Mesh>();

    private List<Vector3> v = new List<Vector3>();




    // Start is called before the first frame update
    public void Setup()
    {
        foreach (Mesh m in mesh)
        {
            Vector2[] RotatedUVs = m.uv;//Store the existing UV's


            for (var i = 0; i < RotatedUVs.Length; i++)
            {//Go through the array
                var rot = Quaternion.Euler(0, 0, -90);
                RotatedUVs[i] = rot * RotatedUVs[i];
            }

            m.uv = RotatedUVs;//re-apply the adjusted uvs

            for (int i = 2; i < (m.vertices.Length / 2); i += 1)
                v.Add(m.vertices[i]);
        }        
    }

    public void SetGatePosition(GameObject gateParent, GameObject gate1, GameObject gate2, LineRenderer lr, BoxCollider col, float percent)
    {
        gateParent.transform.DetachChildren();

        // Get a vertex position in array from percent
        int placement = Mathf.RoundToInt((1f - percent) * (v.Count - 3));

        Vector3 p1 = v[placement];
        Vector3 p2 = v[placement + 1];
        Vector3 p3 = v[placement + 2];

        /*
         * Vertex positions along splinemesh track:
         *
         *   2 
         *  \/\/\/\/\/\/
         *  1 3 
         *  
         */

        // Place first pole between p1/p3
        gate1.transform.position = (p1 + p3) / 2 + Vector3.up * 0.5f;
        // Place second pole at p2
        gate2.transform.position = p2 + Vector3.up * 0.5f;

        // Place gate parent between gate poles
        gateParent.transform.position = (gate1.transform.position + gate2.transform.position) / 2;

        // Place collider between gate points
        col.transform.position = (gate1.transform.position + gate2.transform.position) / 2;
        // Stretch collider to meet both gate points
        col.size = new Vector3(Vector3.Distance(gate1.transform.position, gate2.transform.position), col.size.y, col.size.z);

        // Find direction perpendicular to the line between the two gate points
        Vector3 dir = gate1.transform.position - gate2.transform.position;
        Vector3 forward = Vector3.Cross(dir, Vector3.up).normalized;
        gate1.transform.forward = forward;
        gate2.transform.forward = forward;
        gateParent.transform.forward = forward;
        col.transform.forward = forward;

        // Stretch line renderer between gate poles
        lr.SetPosition(0, gate1.transform.position + Vector3.up * 0.45f);
        lr.SetPosition(1, gate2.transform.position + Vector3.up * 0.45f);

        // Reparent to gateParent
        gate1.transform.SetParent(gateParent.transform);
        gate2.transform.SetParent(gateParent.transform);
        lr.transform.SetParent(gateParent.transform);
        col.transform.SetParent(gateParent.transform);
    }

    public void SetColliderPosition(BoxCollider col, float percent)
    {
        // Get a vertex position in array from percent
        int placement = Mathf.RoundToInt((1f - percent) * (v.Count - 3));

        Vector3 p1 = v[placement];
        Vector3 p2 = v[placement + 1];
        Vector3 p3 = v[placement + 2];

        // Place first point between p1/p3
        Vector3 point1 = (p1 + p3) / 2 + Vector3.up * 0.5f;
        // Place second point at p2
        Vector3 point2 = p2 + Vector3.up * 0.5f;

        // Place collider between gate points
        col.transform.position = (point1 + point2) / 2;
        // Stretch collider to meet both gate points
        col.size = new Vector3(Vector3.Distance(point1, point2), col.size.y, col.size.z);

        // Find direction perpendicular to the line between the two gate points
        Vector3 dir = point1 - point2;
        Vector3 forward = Vector3.Cross(dir, Vector3.up).normalized;
        col.transform.forward = forward;
    }
}
