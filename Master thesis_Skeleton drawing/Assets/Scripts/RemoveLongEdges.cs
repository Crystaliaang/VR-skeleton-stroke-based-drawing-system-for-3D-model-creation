using UnityEngine;
using System.Collections.Generic;

public class RemoveLongEdges : MonoBehaviour
{
    public float maxEdgeLength = 0.2f; 

    void Start()
    {
        //Debug.Log($"Checking mesh: {gameObject.name}");
        RemoveEdges();
    }

    void RemoveEdges()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogWarning("No MeshFilter found on the object.");
            return;
        }

        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        List<int> newTriangles = new List<int>();

        Dictionary<int, HashSet<int>> edgeMap = new Dictionary<int, HashSet<int>>();

        // edge map to track long edges
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            float d1 = Vector3.Distance(v1, v2);
            float d2 = Vector3.Distance(v2, v3);
            float d3 = Vector3.Distance(v3, v1);

            // Check each edge and mark long edges
            if (d1 > maxEdgeLength)
                MarkEdge(edgeMap, i1, i2);
            if (d2 > maxEdgeLength)
                MarkEdge(edgeMap, i2, i3);
            if (d3 > maxEdgeLength)
                MarkEdge(edgeMap, i3, i1);
        }

        // Rebuild the mesh with filtered edges
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            if (IsEdgeLong(edgeMap, i1, i2) || IsEdgeLong(edgeMap, i2, i3) || IsEdgeLong(edgeMap, i3, i1))
                continue; // Skip triangles with long edges

            newTriangles.Add(i1);
            newTriangles.Add(i2);
            newTriangles.Add(i3);
        }

        mesh.triangles = newTriangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();



    }

    void MarkEdge(Dictionary<int, HashSet<int>> edgeMap, int v1, int v2)
    {
        if (!edgeMap.ContainsKey(v1))
            edgeMap[v1] = new HashSet<int>();
        if (!edgeMap.ContainsKey(v2))
            edgeMap[v2] = new HashSet<int>();

        edgeMap[v1].Add(v2);
        edgeMap[v2].Add(v1);
    }

    bool IsEdgeLong(Dictionary<int, HashSet<int>> edgeMap, int v1, int v2)
    {
        return edgeMap.ContainsKey(v1) && edgeMap[v1].Contains(v2);
    }
}