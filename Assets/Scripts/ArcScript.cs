using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcScript : MonoBehaviour
{
    public int arcSpan = 60;
    public float TargetDistance;
    public float Angle;

    private int prevSpan = 60;
    public float width = 2f;
    public float thickness = 0.015f;
    private float lerpTimer;

    private bool expand;

    // Start is called before the first frame update
    void Start()
    {
        GenerateArc();
    }

    // Update is called once per frame
    void Update()
    {
        if (expand && arcSpan < 180)
        {
            arcSpan = (int)Mathf.Lerp(60f, 180f, lerpTimer);
            lerpTimer += Time.deltaTime * 0.75f;
            GenerateArc();
        }
    }

    public void Expand()
    {
        expand = true;
    }

    public void GenerateArc()
    {
        Mesh mesh = new Mesh();
        float halfWidth = width / 200f;
        float halfThickness = thickness / 2f;

        float delta = Mathf.Deg2Rad * arcSpan / arcSpan;

        float angle = 0f;

        int length = (arcSpan * 4) + 4;

        Vector3[] vertices = new Vector3[length];

        float distance = TargetDistance + transform.position.z;
        for (int i = 0; i < length; i += 4)
        {
            float outerX = Mathf.Sin(angle) * (distance + halfWidth);
            float outerY = Mathf.Cos(angle) * (distance + halfWidth);
            float innerX = Mathf.Sin(angle) * (distance - halfWidth);
            float innerY = Mathf.Cos(angle) * (distance - halfWidth);

            vertices[i].Set(outerX, outerY, -halfThickness);
            vertices[i + 1].Set(innerX, innerY, -halfThickness);
            vertices[i + 2].Set(outerX, outerY, halfThickness);
            vertices[i + 3].Set(innerX, innerY, halfThickness);

            angle += delta;
        }

        List<int> triangles = new List<int>();

        for (int i = 0; i < 4 * arcSpan; i += 4)
        {
            // Top Face
            triangles.Add(i);
            triangles.Add(i + 4);
            triangles.Add(i + 1);

            triangles.Add(i + 1);
            triangles.Add(i + 4);
            triangles.Add(i + 5);

            // Bottom Face
            triangles.Add(i + 2);
            triangles.Add(i + 3);
            triangles.Add(i + 6);

            triangles.Add(i + 3);
            triangles.Add(i + 7);
            triangles.Add(i + 6);

            // Outside Face
            triangles.Add(i);
            triangles.Add(i + 2);
            triangles.Add(i + 4);

            triangles.Add(i + 2);
            triangles.Add(i + 6);
            triangles.Add(i + 4);

            // Inside Face
            triangles.Add(i + 1);
            triangles.Add(i + 5);
            triangles.Add(i + 3);

            triangles.Add(i + 5);
            triangles.Add(i + 7);
            triangles.Add(i + 3);
        }

        // End Pieces
        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);

        triangles.Add(1);
        triangles.Add(3);
        triangles.Add(2);

        triangles.Add(vertices.Length - 1);
        triangles.Add(vertices.Length - 3);
        triangles.Add(vertices.Length - 2);

        triangles.Add(vertices.Length - 2);
        triangles.Add(vertices.Length - 3);
        triangles.Add(vertices.Length - 4);

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        transform.localEulerAngles = new Vector3(
            90f,
            -arcSpan / 2f,
            0.0f
            );

        GetComponent<MeshFilter>().mesh.Clear();
        GetComponent<MeshFilter>().mesh = mesh;
    }
}
