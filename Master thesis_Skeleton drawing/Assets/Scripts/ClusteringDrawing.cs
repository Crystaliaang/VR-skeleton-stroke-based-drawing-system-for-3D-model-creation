using System.Collections.Generic;
using UnityEngine;

public class PointCluster
{
    public List<Vector3> points = new List<Vector3>();
    public Vector3 centroid;
    private bool isCentroidFixed = false;

    

    public PointCluster(Vector3 initialCentroid, bool fixCentroid = false)
    {
        centroid = initialCentroid;
        isCentroidFixed = fixCentroid;
        points.Add(initialCentroid);
    }
    public void SetCentroid(Vector3 intersectionPoint)
    {
        centroid = intersectionPoint;
        isCentroidFixed = true; 
    }



    public void AddPoint(Vector3 point, bool isIntersection = false)
    {
        points.Add(point);

        // If this is an intersection, fix the centroid
        if (isIntersection && !isCentroidFixed)
        {
            isCentroidFixed = true;
            centroid = point; 
        }
        else if (!isCentroidFixed)
        {
            UpdateCentroid();
        }
    }


    public bool RemovePoint(Vector3 point)
    {
        if (points.Contains(point))
        {
            points.Remove(point);

            // Update the centroid if it's not fixed and there are still points left
            if (!isCentroidFixed && points.Count > 0)
            {
                UpdateCentroid();
            }

            // If no points are left, return true to indicate the cluster is empty
            return points.Count == 0;
        }
        return false;
    }

    private void UpdateCentroid()
    {
        //if (points.Count == 0) return;
        if (points.Count == 0 || isCentroidFixed) return;


        // Sort the points by their distance to the origin (or any consistent sorting criteria)
        points.Sort((a, b) =>
        {
            float distanceA = a.sqrMagnitude;  
            float distanceB = b.sqrMagnitude;
            return distanceA.CompareTo(distanceB);
        });

        
        int count = points.Count;

        if (count % 2 == 1)
        {
           
            centroid = points[count / 2];
        }
        else
        {
           
            centroid = points[count / 2 - 1];
        }
    }
}

public class ClusteringDrawing 
{
    private List<PointCluster> clusters = new List<PointCluster>();
    private float clusterRadius = 0.02f;  //threshold 

    public void AddPointToCluster(Vector3 newPoint, bool isIntersection = false)
    {
        PointCluster nearestCluster = FindNearestCluster(newPoint);

        if (nearestCluster != null && Vector3.Distance(nearestCluster.centroid, newPoint) <= clusterRadius)
        {
            nearestCluster.AddPoint(newPoint, isIntersection);
        }
        else
        {
            PointCluster newCluster = new PointCluster(newPoint, isIntersection);
            clusters.Add(newCluster);
        }
    }

    public PointCluster FindNearestCluster(Vector3 point)
    {
        PointCluster nearestCluster = null;
        float nearestDistance = float.MaxValue;

        foreach (var cluster in clusters)
        {
            float distance = Vector3.Distance(cluster.centroid, point);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCluster = cluster;
            }
        }
        return nearestCluster;
    }

    public PointCluster FindClusterForPoint(Vector3 point)
    {
        foreach (var cluster in clusters)
        {
            if (cluster.points.Contains(point))
            {
                return cluster; 
            }
        }
        return null; 
    }

    public void PrintClusters()
    {
        for (int i = 0; i < clusters.Count; i++)
        {
            Debug.Log($"Cluster {i}:");
            Debug.Log($"Centroid: {clusters[i].centroid}");

            foreach (var point in clusters[i].points)
            {
                Debug.Log($"Point: {point}");
            }
        }
    }

    // List to keep track of visual markers so they can be cleared if needed
    //private List<GameObject> visualizationMarkers = new List<GameObject>();


    //public void ClearVisualizations()
    //{
    //    foreach (var marker in visualizationMarkers)
    //    {
    //        if (marker != null)
    //        {
    //            Destroy(marker); // Destroy should work here
    //        }
    //    }
    //    visualizationMarkers.Clear();
    //}


    //public void VisualizeClusters()
    //{
    //    ClearVisualizations(); // Clear any existing visualizations

    //    foreach (var cluster in clusters)
    //    {
    //        // Visualize the centroid with a red cube
    //        GameObject centroidMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //        centroidMarker.transform.position = cluster.centroid;
    //        centroidMarker.transform.localScale = Vector3.one * 0.02f; // Make centroid cubes slightly larger
    //        centroidMarker.GetComponent<Renderer>().material.color = Color.red;
    //        visualizationMarkers.Add(centroidMarker);

    //        // Visualize each point in the cluster with blue cubes
    //        foreach (var point in cluster.points)
    //        {
    //            GameObject pointMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //            pointMarker.transform.position = point;
    //            pointMarker.transform.localScale = Vector3.one * 0.01f; // Smaller scale for regular points
    //            pointMarker.GetComponent<Renderer>().material.color = Color.blue;
    //            visualizationMarkers.Add(pointMarker);
    //        }
    //    }
    //}

    public void RemovePointFromCluster(Vector3 point)
    {
        PointCluster cluster = FindClusterForPoint(point);

        if (cluster != null)
        {
            bool isClusterEmpty = cluster.RemovePoint(point);

            if (isClusterEmpty)
            {
                clusters.Remove(cluster);
            }
        }
    }
}