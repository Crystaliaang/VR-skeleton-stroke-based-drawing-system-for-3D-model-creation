using System.Collections.Generic;
using UnityEngine;

public class KDTreeNode
{
    public Vector3 point;
    public int axis;
    public KDTreeNode left;
    public KDTreeNode right;

    public KDTreeNode(Vector3 point, int axis)
    {
        this.point = point;
        this.axis = axis;
        left = null;
        right = null;
    }
}

public class KDTree
{
    private KDTreeNode root;
    private int k = 3; // 3 dimensions for Vector3 points

    // Build the KD-Tree from a list of points
    public KDTree(List<Vector3> points)
    {
        root = BuildTree(points, 0);
    }

    private KDTreeNode BuildTree(List<Vector3> points, int depth)
    {
        if (points.Count == 0) return null;

        int axis = depth % k;

        
        points.Sort((a, b) => a[axis].CompareTo(b[axis])); // Sort points by the current axis

        int median = points.Count / 2;

        KDTreeNode node = new KDTreeNode(points[median], axis);

        node.left = BuildTree(points.GetRange(0, median), depth + 1);
        node.right = BuildTree(points.GetRange(median + 1, points.Count - median - 1), depth + 1);

        return node;
    }

    public Vector3 FindNearestNeighbor(Vector3 targetPoint)
    {
        return FindNearest(root, targetPoint, root.point, 0);
    }

    private Vector3 FindNearest(KDTreeNode node, Vector3 targetPoint, Vector3 bestPoint, int depth)
    {
        if (node == null) return bestPoint;

        int axis = depth % k;

        Vector3 nextBest = bestPoint;
        float bestDistance = Vector3.Distance(targetPoint, bestPoint);

        // Check if the current node is closer
        float currentDistance = Vector3.Distance(targetPoint, node.point);
        if (currentDistance < bestDistance)
        {
            nextBest = node.point;
            bestDistance = currentDistance;
        }

        // Recursively search the next subtree
        KDTreeNode nextNode = targetPoint[axis] < node.point[axis] ? node.left : node.right;
        KDTreeNode otherNode = targetPoint[axis] < node.point[axis] ? node.right : node.left;

        nextBest = FindNearest(nextNode, targetPoint, nextBest, depth + 1);

        // Check the other side 
        if (Mathf.Abs(targetPoint[axis] - node.point[axis]) < bestDistance)
        {
            nextBest = FindNearest(otherNode, targetPoint, nextBest, depth + 1);
        }

        return nextBest;
    }

 
    public void Insert(Vector3 newPoint)
    {
        root = InsertRecursively(root, newPoint, 0);
    }

  
    private KDTreeNode InsertRecursively(KDTreeNode node, Vector3 newPoint, int depth)
    {
        // If reached a null node-> insert the new point here
        if (node == null)
        {
            return new KDTreeNode(newPoint, depth % k);
        }

        int axis = depth % k;

        // insert the point in the correct subtree based on the axis
        if (newPoint[axis] < node.point[axis])
        {
            node.left = InsertRecursively(node.left, newPoint, depth + 1);
        }
        else
        {
            node.right = InsertRecursively(node.right, newPoint, depth + 1);
        }

        return node;
    }

  
    public void PrintTree()
    {
        if (root == null)
        {
            Debug.Log("KD-Tree is empty.");
            return;
        }
        Debug.Log("KD-Tree Contents:");
        TraverseAndPrint(root);
    }

    private void TraverseAndPrint(KDTreeNode node)
    {
        if (node == null) return;
        Debug.Log($"Point: {node.point}");
        TraverseAndPrint(node.left);
        TraverseAndPrint(node.right);
    }
}