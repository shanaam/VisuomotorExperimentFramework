using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GatePlacement : MonoBehaviour
{
    public List<Mesh> mesh = new List<Mesh>();

    private List<Vector3> v = new List<Vector3>();

    /*
     * Vertex positions along splinemesh path:
     *  
     * 0 2 4
     *  \/\/\/\/\/\/
     *  1 3 
     */


    // Start is called before the first frame update
    public void Setup()
    { 
        foreach (Mesh m in mesh)
            for (int i = 2; i < (m.vertices.Length / 2); i += 1)
                v.Add(m.vertices[i]);
    }

    [ContextMenu("set gate position")]
    public void SetGatePosition(GameObject gate1, GameObject gate2, LineRenderer lr, BoxCollider col, float percent)
    {
        int placement = Mathf.RoundToInt(percent * (v.Count - 3));

        Vector3 n1 = v[placement];
        Vector3 n2 = v[placement + 2];

        gate1.transform.position = (n2 + n1) / 2;
        gate2.transform.position = v[placement + 1];

        Vector3 dir = gate1.transform.position - gate2.transform.position;
        Vector3 left = Vector3.Cross(dir, Vector3.up).normalized;

        gate1.transform.forward = left;
        gate2.transform.forward = left;

        lr.SetPosition(0, gate1.transform.position);
        lr.SetPosition(1, gate2.transform.position);

        col.transform.position = (gate1.transform.position + gate2.transform.position) / 2;
        col.transform.forward = left;
        col.size = new Vector3 (Vector3.Distance(gate1.transform.position, gate2.transform.position), 1, 0.1f);
    }
}
