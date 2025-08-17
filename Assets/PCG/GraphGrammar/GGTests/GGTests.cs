using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// small experiment with unity's mesh low level programming
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GGTests : MonoBehaviour
{
    [SerializeField] private int segments = 32;
    [SerializeField] private float radius = 1f;

    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.name = "Procedural Circle";

        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];
        Vector2[] uvs = new Vector2[vertices.Length];

        // Center vertex
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        // Generate outer vertices
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            vertices[i + 1] = new Vector3(x, 0, z);
            uvs[i + 1] = new Vector2((x / radius + 1f) * 0.5f, (z / radius + 1f) * 0.5f);
        }

        // Generate triangle indices
        for (int i = 0; i < segments; i++)
        {
            int start = (i + 1);
            int next = (i + 2) <= segments ? (i + 2) : 1;

            triangles[i * 3] = 0;       // center
            triangles[i * 3 + 1] = next;
            triangles[i * 3 + 2] = start;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;
    }
}
