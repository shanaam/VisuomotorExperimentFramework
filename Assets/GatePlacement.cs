using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GatePlacement : MonoBehaviour
{
    public List<Mesh> mesh;

    public List<Vector3> v = new List<Vector3>();

    [SerializeField, Range(0, 1)]
    public float percent;

    public GameObject gate;
    public GameObject gate2;

    public LineRenderer lr;

    public BoxCollider col;

    /*
     * Vertex positions along splinemesh path:
     *  
     * 0 2 4
     *  \/\/\/\/\/\/
     *  1 3 
     */


    // Start is called before the first frame update
    void Start()
    { 

        foreach (Mesh m in mesh)
            for (int i = 2; i < (m.vertices.Length / 2); i += 1)
                v.Add(m.vertices[i]);
        //if (i < m.vertices.Length / 2 - 10)

    }

    // Update is called once per frame
    void Update()
    {
        SetGatePosition();
    }

    [ContextMenu("set gate position")]
    void SetGatePosition()
    {
        int placement = Mathf.RoundToInt(percent * (v.Count - 3));
            
            //Mathf.Clamp(placement, 0, v.Count - 3);

        Vector3 n1 = v[placement];
        Vector3 n2 = v[placement + 2];

        gate.transform.position = (n2 + n1) / 2;
        gate2.transform.position = v[placement + 1];

        Vector3 dir = gate.transform.position - gate2.transform.position;
        Vector3 left = Vector3.Cross(dir, Vector3.up).normalized;

        gate.transform.forward = left;
        gate2.transform.forward = left;

        lr.SetPosition(0, gate.transform.position);
        lr.SetPosition(1, gate2.transform.position);

        col.transform.position = (gate.transform.position + gate2.transform.position) / 2;
        col.transform.forward = left;
        col.size = new Vector3 (Vector3.Distance(gate.transform.position, gate2.transform.position), 1, 0.1f);

    }
}
