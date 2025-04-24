using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;


public class Design : MonoBehaviour
{
    [Header("Pen Properties")]
    public Transform tip;
    public Material drawingMaterial;
    public Material tipMaterial;
    [Range(0.01f, 0.1f)]
    public float penWidth = 0.01f;
    public Color[] penColors;

    [Header("Controller Input")]
    [SerializeField]
    private InputActionReference rightControllerTriggerAction;
    [SerializeField]
    private InputActionReference leftControllerTriggerAction;
    [SerializeField]
    private InputActionReference rightControllerPrimaryButtonAction;
    [SerializeField]
    private InputActionReference changeWidthButtonAction;  


    [Header("Right Controller")]
    public Transform rightControllerTransform; 
    public Transform penTransform; 

    private List<LineRenderer> drawnLines = new List<LineRenderer>(); 
    private LineRenderer currentDrawing;
    private int index;
    private int currentColorIndex;
    private bool isDrawing;

    private KDTree kdTree; // KDTree field
    private List<Vector3> allPoints = new List<Vector3>(); // Store all points for KD-Tree
    private bool NewTree = false;

    public MeshRenderer mesh;
    public static GameObject newContainer;
    public GameObject previewContainer;
    public static Material baseFallbackMaterial;


    [Header("Save line positions")]
    public string saveFileName = "DrawingData.pts"; 
    private DrawingData drawingData = new DrawingData();
    private string saveFilePath; 
    private string connectionsFilePath;  
    public string connectionsFileName = "Connections.graph";


    private ClusteringDrawing clusterManager;
    //private bool NewCluster = false;
    private bool firstLine = true;
    public static bool isPreview = false;

    [Header("Preview materials")]
    public Material pointMaterial;
    public Material VRpointMaterial;

    [Header("Sphere materials")]
    public Material VRBluePointMaterial;
    public Material VRRedPointMaterial;

    //Metrics
    public static int SketchedLinesNum = 0;
    public static int UndoSketchedLinesNum = 0;
    public static int MediumLinesNum = 0;
    public static int PreviewTimesNum = 0;
    
    
    

    private void Start()
    {
        CreateNewLineContainer();
        currentColorIndex = 0;
        tipMaterial.color = penColors[currentColorIndex];

        //clusterManager = new ClusteringDrawing();

        //// Initialize KDTree 
        //NewTree = true;
        //UpdateKDTree();

      
#if UNITY_STANDALONE_OSX 
        saveFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", saveFileName); 
        connectionsFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", connectionsFileName); 
#elif UNITY_ANDROID
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName); 
        connectionsFilePath = Path.Combine(Application.persistentDataPath, connectionsFileName); 
#endif

       
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        string directoryPath = Path.Combine(Application.streamingAssetsPath, "Drawing");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
#endif

      
        if (rightControllerTransform != null && penTransform != null)
        {
            penTransform.SetParent(rightControllerTransform, true);
            Quaternion rotationOffset = Quaternion.Euler(90f, 0f, 0f);
            Vector3 positionOffset = new Vector3(0f, 0f, 0.1f);
            penTransform.localPosition = Vector3.zero;
            penTransform.localRotation = rotationOffset;

            penTransform.position = rightControllerTransform.position + rightControllerTransform.TransformVector(positionOffset);
            penTransform.rotation = rightControllerTransform.rotation * Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            //Debug.LogError("Assign both rightControllerTransform and penTransform in the Inspector.");
        }

    }

    private void Update()
    {

        //if (changeWidthButtonAction != null && changeWidthButtonAction.action.triggered)
        //{
        //    // Change the pen width dynamically when the button is pressed
        //    ChangePenWidth();
        //}

        bool isRightHandDrawing = IsButtonPressed(rightControllerTriggerAction);
        bool isLeftHandDrawing = IsButtonPressed(leftControllerTriggerAction);
        bool wasDrawing = isDrawing;
        isDrawing = isRightHandDrawing || isLeftHandDrawing;

        if (isDrawing)
        {
            if (LinePointPokeMover.editApplied)
            {
                LinePointPokeMover.editApplied = false;
                if (newContainer != null)
                {
                    UpdateAfterEdit();
                }
            }
            Draw();
        }
        else if (!isDrawing && currentDrawing != null)
        {
            SketchedLinesNum++;
            EndDrawing();
            if (isPreview)
            {
                TogglePreview();
                TogglePreview();
            }
        }
        //else if (IsButtonPressed(rightControllerPrimaryButtonAction))
        //{
        //    SwitchColor();
        //}
    }

    //private void ChangePenWidth()
    //{
    //    // Example logic to increase the width and cycle it
    //    penWidth += 0.01f;
    //    if (penWidth > 0.1f)  // Maximum width
    //    {
    //        penWidth = 0.01f;  // Reset to the minimum width
    //    }
    //    Debug.Log($"New Pen Width: {penWidth}");
    //}

    private bool IsButtonPressed(InputActionReference actionReference)
    {
        var action = GetInputAction(actionReference);
        return action != null && action.ReadValue<float>() > 0.1f;
    }

    private void Draw()
    {

        if (currentDrawing == null)
        {
            // Start a new line
            index = 0;
            currentDrawing = new GameObject("Line").AddComponent<LineRenderer>();
            //currentDrawing.gameObject.tag = "Spawned";  
            currentDrawing.transform.SetParent(newContainer.transform); 
            currentDrawing.material = drawingMaterial;
            currentDrawing.startColor = currentDrawing.endColor = penColors[currentColorIndex];
            currentDrawing.startWidth = currentDrawing.endWidth = penWidth;
            currentDrawing.positionCount = 1;
            currentDrawing.SetPosition(0, tip.position); // Start at the tip position

            
        }
        else
        {
            var currentPos = tip.position;
            if (Vector3.Distance(currentDrawing.GetPosition(index), currentPos) > 0.01f)
            {
                index++;
                currentDrawing.positionCount = index + 1;
                currentDrawing.SetPosition(index, currentPos);
            }
        }
    }

    private void EndDrawing()
    {

        if (currentDrawing != null)
        {


            LineData lineData = new LineData();
            List<Vector3> newLinePoints = new List<Vector3>();

       
            for (int i = 0; i < currentDrawing.positionCount; i++)
            {
                Vector3 point = currentDrawing.GetPosition(i);
                newLinePoints.Add(point); //  points for intersection check
            }
           
            (bool foundSimilar, List<Vector3> newLinePointsMedium) = checkSimilarLineExists(newLinePoints);

            List<Vector3> updatedPoints;
            List<Vector3> newPoints;
            List<(int, int)> intersectionConnections;
            List<Vector3> newPointsintersectionPoints;
            if (foundSimilar)
            {
                //(updatedPoints, newPoints,  intersectionConnections, newPointsintersectionPoints) = CheckForIntersections(newLinePointsMedium, 0.02f); //threshold
                //UpdateAfterMedium(newLinePointsMedium);
                UpdateAfterMedium();
                currentDrawing = null;
                MediumLinesNum++;
            }
            else
            {
                (updatedPoints, newPoints, intersectionConnections, newPointsintersectionPoints) = CheckForIntersections(newLinePoints, 0.02f); //threshold



                // Check for intersections and get both updated and new points
                //(List<Vector3> updatedPoints, List<Vector3> newPoints, List<(int, int)> intersectionConnections, List<Vector3> newPointsintersectionPoints) = CheckForIntersections(newLinePoints, 0.02f); //threshold


                // Update the LineRenderer with the intersected points
                UpdateLineRenderer(currentDrawing, updatedPoints);
                VisualizePoints(currentDrawing);
                VisualizeIntersectionPoints(currentDrawing, newPointsintersectionPoints);

                AddMeshColliderToLine(currentDrawing);


                currentDrawing = null;

                // Add all updated points (intersected or not) to the line data
                foreach (var updatedPoint in updatedPoints)
                {
                    lineData.positions.Add(updatedPoint);
                }

                // Only add the new (non-intersected) points to the KD-Tree and allPoints
                foreach (var newPoint in newPoints)
                {
                    allPoints.Add(newPoint);
                }

                drawingData.lines.Add(lineData);
                drawnLines.Add(currentDrawing);

                UpdateKDTree(newPoints); // Update KD-Tree with only the new points

                //First and last points of each line are centroids 
                //if (newPoints != null) {
                if (newPoints.Count > 0)
                {
                    clusterManager.AddPointToCluster(newPoints[0], true); // First point as initial centroid
                                                                          //Debug.LogWarning($"newPoints.Count - 1: {newPoints.Count - 1}");

                    clusterManager.AddPointToCluster(newPoints[newPoints.Count - 1], true); // Last point as initial centroid

                }

                //if (firstLine == true)
                //{
                //    firstLine = false;
                //    clusterManager.AddPointToCluster(newPoints[0], true); // First point as initial centroid
                //    clusterManager.AddPointToCluster(newLinePoints[newPoints.Count - 1], true); // Last point as initial centroid

                //}

                foreach (var newPoint in newPoints) // Add all new points to a cluster
                {
                    clusterManager.AddPointToCluster(newPoint);  // Only cluster non-intersected points
                }
            }
        }
        else
        {
            Debug.LogWarning("currentDrawing is null, cannot add to list.");
        }
    }


    private void VisualizePoints(LineRenderer line)
    {
        
        for (int i = 0; i < line.positionCount; i++)
        {
            Vector3 point = line.GetPosition(i);
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = point;
            sphere.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
            Renderer renderer = sphere.GetComponent<Renderer>();


#if UNITY_ANDROID
           
            //sphere.GetComponent<Renderer>().material = VRBluePointMaterial;
        
            if (VRBluePointMaterial != null)
            {
                renderer.sharedMaterial = VRBluePointMaterial;
            }
#elif UNITY_STANDALONE_OSX
            // Load the macOS material by its name
            sphere.GetComponent<Renderer>().material.color = Color.blue;
#endif
            sphere.tag = "LinePoint";
            sphere.transform.SetParent(line.transform);
            //sphere.transform.SetParent(newContainer.transform);
            SphereCollider collider = sphere.GetComponent<SphereCollider>();
            collider.isTrigger = true;
        }
    }

    private void removeAllIntersectedVisualizations(Vector3 point)
    {
        // Find all with "Intersection" tag
        GameObject[] intersectionObjects = GameObject.FindGameObjectsWithTag("Intersection");

        foreach (GameObject obj in intersectionObjects)
        {
          
            if (Vector3.Distance(obj.transform.position, point) < 0.001f) // Adjust tolerance as needed
            {
                //Debug.Log("Destroyed Intersection ");
             
                DestroyImmediate(obj);
            }
        }
    }

    // store intersection counts
    public static Dictionary<Vector3, int> intersectionCounts = new Dictionary<Vector3, int>();

    private void VisualizeIntersectionPoints(LineRenderer line, List<Vector3> newPointsintersectionPoints)
    {
       
        foreach (var point in newPointsintersectionPoints )
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = point;
            sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
 
#if UNITY_ANDROID
           
            sphere.GetComponent<Renderer>().material = VRRedPointMaterial;
#elif UNITY_STANDALONE_OSX
         
            sphere.GetComponent<Renderer>().material.color = Color.red;
#endif
            sphere.tag = "Intersection";
            sphere.transform.SetParent(line.transform);

            SphereCollider collider = sphere.GetComponent<SphereCollider>();
            collider.isTrigger = true;

          
            if (intersectionCounts.ContainsKey(point))
            {
               
                intersectionCounts[point]++;
            }
            else
            {
                
                intersectionCounts[point] = 2;
            }

        }
    }

    [System.Serializable]
    public class LineData
    {
        public List<Vector3> positions = new List<Vector3>();
    }

    [System.Serializable]
    public class DrawingData
    {
        public List<LineData> lines = new List<LineData>();
    }

    private void SwitchColor()
    {
        currentColorIndex = (currentColorIndex + 1) % penColors.Length;
        tipMaterial.color = penColors[currentColorIndex];
    }

    private static InputAction GetInputAction(InputActionReference actionReference)
    {
        return actionReference != null ? actionReference.action : null;
    }


    //Find the nearest point/node
    // and Build or update the KD-Tree with all points
    private void UpdateKDTree(List<Vector3> newPoints)
    {
        if (allPoints.Count > 0 && NewTree == true)
        {
            kdTree = null;
            kdTree = new KDTree(allPoints); 
            NewTree = false;
        }
        else if (allPoints.Count > 0 && NewTree == false)
        {
            
            foreach (var newPoint in newPoints)
            {
                kdTree.Insert(newPoint);
                //Debug.Log("Inserted new point into KD-Tree: " + newPoint);
            }
        }
        else
        {
            kdTree = null; // If no points, KD-Tree should be null
            //Debug.LogWarning("KD-Tree not built because there are no points.");
        }
    }


    //private (List<Vector3> updatedPoints, List<Vector3> newPoints, List<(int, int)> connectionIndices) CheckForIntersections(List<Vector3> newLinePoints, float threshold)
    //{
    //    List<Vector3> updatedPoints = new List<Vector3>();
    //    List<Vector3> newPoints = new List<Vector3>(); // Points that did not intersect and need to be saved
    //    List<(int, int)> connectionIndices = new List<(int, int)>(); // To store pairs of indices of intersecting points
    //    int currentAllPointsIndex;

    //    if (allPoints.Count == 0)
    //    {
    //        currentAllPointsIndex = 0;
    //    }
    //    else
    //    {
    //        currentAllPointsIndex = allPoints.Count;
    //    }


    //    Debug.Log("New Line----currentAllPointsIndex: " + currentAllPointsIndex);
    //    int lastPointIndex = -1; //  track the last added point index

    //    for (int i = 0; i < newLinePoints.Count; i++)
    //    {
    //        var point = newLinePoints[i];
    //        Debug.Log("Processing point index: " + i + " and point:" + point);

    //        // If this is the first line (no existing points)
    //        if (allPoints.Count == 0)
    //        {
    //            updatedPoints.Add(point);
    //            newPoints.Add(point);

    //            // Create edges between sequential nodes for the first line
    //            if (i > 0)
    //            {
    //                connectionIndices.Add((currentAllPointsIndex, currentAllPointsIndex - 1));
    //                Debug.Log("New point edge: " + currentAllPointsIndex + " and " + (currentAllPointsIndex - 1));
    //            }
    //            currentAllPointsIndex++; // Increment for next point
    //        }
    //        else
    //        {
    //            // Check for intersection with existing points
    //            Vector3 closestPoint = FindClosestPoint(point, threshold);
    //            int closestIndex = allPoints.IndexOf(closestPoint); // Get the index of the closest point

    //            if (Vector3.Distance(point, closestPoint) <= threshold && point != closestPoint)
    //            {
    //                // Snap to closest point and create an intersection edge
    //                updatedPoints.Add(closestPoint); // Snap the point to the closest point

    //                // Visualize the intersection point
    //                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    //                sphere.transform.SetParent(newContainer.transform);
    //                sphere.transform.position = closestPoint;
    //                sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    //                sphere.GetComponent<Renderer>().material.color = Color.red;

    //                if (lastPointIndex != -1)
    //                {
    //                    if (lastPointIndex == closestIndex)
    //                    {
    //                        Debug.Log("Intersection found between: " + point + " and " + closestPoint);
    //                        Debug.Log("Intersection edge Not Created: " + lastPointIndex + " and " + closestIndex);
    //                    }
    //                    else {
    //                        connectionIndices.Add((lastPointIndex, closestIndex)); // Connect the previous point to the intersection
    //                        Debug.Log("Intersection found between: " + point + " and " + closestPoint);
    //                        Debug.Log("Intersection edge between: " + lastPointIndex + " and " + closestIndex);
    //                    }
    //                }
    //                else
    //                {
    //                    Debug.Log("Intersection found between: " + point + " and " + closestPoint + "with No edge");

    //                }
    //                lastPointIndex = closestIndex;

    //            }
    //            else
    //            {
    //                // No intersection: Add the new point and create edge if it's not the first point
    //                updatedPoints.Add(point);
    //                newPoints.Add(point);


    //                if (lastPointIndex != -1)
    //                {
    //                    connectionIndices.Add((lastPointIndex, currentAllPointsIndex)); // Create edge between new point and last added point
    //                    Debug.Log("New point edge: " + currentAllPointsIndex + " and " + lastPointIndex);
    //                }
    //                else
    //                {
    //                    Debug.Log("With No edge. ");
    //                }

    //                lastPointIndex = currentAllPointsIndex; // Update the last added point index
    //                currentAllPointsIndex++; // Increment for the next new point
    //            }
    //        }
    //    }
    //    // Return both updated points for the line renderer and the new points for saving
    //    return (updatedPoints, newPoints, connectionIndices);
    //}

    //With clusters

    private (List<Vector3> updatedPoints, List<Vector3> newPoints, List<(int, int)> connectionIndices, List<Vector3> intersectionPoints) CheckForIntersections(List<Vector3> newLinePoints, float threshold)
    {
        List<Vector3> updatedPoints = new List<Vector3>();
        List<Vector3> newPoints = new List<Vector3>(); 
        List<(int, int)> connectionIndices = new List<(int, int)>(); 
        List<Vector3> intersectionPoints = new List<Vector3>();
        HashSet<Vector3> fixedPoints = new HashSet<Vector3>();
        int currentAllPointsIndex;

        if (allPoints.Count == 0)
        {
            currentAllPointsIndex = 0;
        }
        else
        {
            currentAllPointsIndex = allPoints.Count;
        }

        int lastPointIndex = -1; // Track the last added point index

        for (int i = 0; i < newLinePoints.Count; i++)
        {
            var point = newLinePoints[i];
            //Debug.Log("Processing point index: " + i + " and point:" + point);

            if (allPoints.Count == 0)
            {
                updatedPoints.Add(point);
                //newPoints.Add(point);

                if (i > 0)
                {
                    connectionIndices.Add((currentAllPointsIndex, currentAllPointsIndex - 1));
                }
                currentAllPointsIndex++;
            }
            else
            {

                //Debug.Log("Trying to find intersection for point: " + point);
                // Find the closest point in the KD-Tree
                Vector3 closestPoint = FindClosestPoint(point, threshold);
                int OLDclosestIndex = allPoints.IndexOf(closestPoint);
                //Debug.Log("--------Closest point found: " + closestPoint);

                if (Vector3.Distance(point, closestPoint) <= threshold && point != closestPoint)
                {
                    //Debug.Log("--------Closest point found: " + closestPoint + "searching cluster");
                    // Get the cluster that contains the closest point
                    PointCluster cluster = clusterManager.FindClusterForPoint(closestPoint);
                    int closestIndex = 0;
                    

                    if (cluster != null)
                    {
                        //Debug.Log("--------cluster != null");
     
                        Vector3 clusterCentroid = cluster.centroid;
                        cluster.SetCentroid(clusterCentroid);
                        closestIndex = allPoints.IndexOf(clusterCentroid);

                        // Check if the new point (clusterCentroid) is the same as the last point
                        if (updatedPoints.Count == 0 || updatedPoints[updatedPoints.Count - 1] != clusterCentroid)
                        {
                            updatedPoints.Add(clusterCentroid); 
                            intersectionPoints.Add(clusterCentroid); 
                            fixedPoints.Add(clusterCentroid); 

                            if (lastPointIndex != -1)
                            {
                                connectionIndices.Add((lastPointIndex, closestIndex)); 
                            }

                            lastPointIndex = closestIndex;
                            //Debug.Log("--------new intersection");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No cluster found for closest point!");
                    }

                    lastPointIndex = closestIndex;
                }
                else
                {
                    // No intersection
                    updatedPoints.Add(point);
                    //newPoints.Add(point);

                    if (lastPointIndex != -1)
                    {
                        connectionIndices.Add((lastPointIndex, currentAllPointsIndex));
                    }

                    lastPointIndex = currentAllPointsIndex;
                    currentAllPointsIndex++;
                }
            }
        }
        // Apply Laplacian smoothing 
        List<Vector3> smoothedPoints = ApplyLaplacianSmoothing(updatedPoints, intersectionPoints, 0.3f, 5);
        for (int i = 0; i < smoothedPoints.Count; i++)
        {
            if (!fixedPoints.Contains(smoothedPoints[i]))
            {
                newPoints.Add(smoothedPoints[i]);
                
            }
        }
        return (smoothedPoints, newPoints, connectionIndices, intersectionPoints);
    }

    private List<Vector3> ApplyLaplacianSmoothing(List<Vector3> points, List<Vector3> fixedPoints, float alpha = 0.5f, int iterations = 5)
    {
        List<Vector3> smoothedPoints = new List<Vector3>(points);

      
        for (int iter = 0; iter < iterations; iter++)
        {
            List<Vector3> tempPoints = new List<Vector3>(smoothedPoints); 
            for (int i = 1; i < smoothedPoints.Count - 1; i++)
            {
                if (fixedPoints.Contains(smoothedPoints[i])) 
                    continue; 
                // Apply Laplacian smoothing
                tempPoints[i] = (1 - alpha) * smoothedPoints[i] + alpha * (smoothedPoints[i - 1] + smoothedPoints[i + 1]) / 2;
            }

            smoothedPoints = tempPoints; 
        }
        return smoothedPoints;
    }

    //private List<Vector3> SmoothAndResampleCurve(List<Vector3> points, List<Vector3> fixedPoints, int numSamples = 10)
    //{
    //    //List<Vector3> smoothedPoints = ApplyLaplacianSmoothing(points, fixedPoints, alpha, laplaceIterations);

    //    //  Resample points along the smoothed curve
    //    List<Vector3> resampledPoints = new List<Vector3>();

    //    // Find the distances along the smoothed curve to define positions for resampling
    //    float totalDistance = 0f;
    //    List<float> distances = new List<float> { 0f }; // Start with zero distance

    //    for (int i = 1; i < points.Count; i++)
    //    {
    //        float distance = Vector3.Distance(points[i - 1], points[i]);
    //        totalDistance += distance;
    //        distances.Add(totalDistance);
    //    }

    //    // Now, generate evenly spaced points along this total distance
    //    float segmentLength = totalDistance / (numSamples - 1);

    //    for (int i = 0; i < numSamples; i++)
    //    {
    //        float targetDistance = i * segmentLength;
    //        Vector3 resampledPoint = InterpolateAlongCurve(points, distances, targetDistance);
    //        resampledPoints.Add(resampledPoint);
    //    }

    //    return resampledPoints;
    //}

   
    //private Vector3 InterpolateAlongCurve(List<Vector3> points, List<float> distances, float targetDistance)
    //{
       
    //    int segmentIndex = 0;
    //    while (segmentIndex < distances.Count - 1 && targetDistance > distances[segmentIndex + 1])
    //    {
    //        segmentIndex++;
    //    }

   
    //    float segmentStartDist = distances[segmentIndex];
    //    float segmentEndDist = distances[segmentIndex + 1];
    //    Vector3 start = points[segmentIndex];
    //    Vector3 end = points[segmentIndex + 1];

    //    float t = (targetDistance - segmentStartDist) / (segmentEndDist - segmentStartDist);
    //    return Vector3.Lerp(start, end, t);
    //}


    // Modify the method to use the KD-Tree for closest point search
    private Vector3 FindClosestPoint(Vector3 newPoint, float threshold)
    {
        if (allPoints.Count == 0 || kdTree == null)
        {
            //Debug.LogWarning("KD-Tree is not initialized, returning original point.");
            return newPoint; // Return the original point if KD-Tree is not initialized
        }

        Vector3 closestPoint = kdTree.FindNearestNeighbor(newPoint);

        if (closestPoint == null)
        {
            Debug.LogError("Closest point is null, something went wrong.");
            return newPoint;
        }

        // If the closest point is within the threshold, return it
        if (Vector3.Distance(newPoint, closestPoint) <= threshold)
        {
            return closestPoint;
        }

        return newPoint;
    }

    private void UpdateLineRenderer(LineRenderer lineRenderer, List<Vector3> updatedPoints)
    {
        lineRenderer.positionCount = updatedPoints.Count;

        for (int i = 0; i < updatedPoints.Count; i++)
        {
            lineRenderer.SetPosition(i, updatedPoints[i]);
        }
    }

    int lineContainerCount = 0;
    int lineContainerCountLines = 0;
    public GameObject CreateNewLineContainer()
    {
        SketchTimer.StartTimer();
        //// Initialize KDTree 
        firstLine = true;

        allPoints = new List<Vector3>();
        NewTree = true;
        //NewCluster = true;
        clusterManager = new ClusteringDrawing();

        lineContainerCount++;
        newContainer = new GameObject($"LinesContainer_{lineContainerCount}");
        newContainer.gameObject.tag = "LineSegment";
        //Debug.Log($"CreateNewLineContainer: " + "LinesContainer");
        intersectionCounts = new Dictionary<Vector3, int>();
        LinePointPokeMover.editApplied = false;
        return newContainer;
    }

    public void SaveAllDrawingData()
    {
        if (newContainer != null)
        {
            // Get all the LineRenderers in the container
            LineRenderer[] lineRenderers = newContainer.GetComponentsInChildren<LineRenderer>();

            Dictionary<Vector3, int> nodeIndices = new Dictionary<Vector3, int>(); 
            List<(int, int)> intersectionConnections = new List<(int, int)>(); 

            int currentNodeIndex = 0;

#if UNITY_STANDALONE_OSX
            string nodeFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing");
            if (!Directory.Exists(nodeFilePath))
            {
                Directory.CreateDirectory(nodeFilePath);
            }
#endif

            using (StreamWriter nodeWriter = new StreamWriter(saveFilePath, true))
            using (StreamWriter edgeWriter = new StreamWriter(connectionsFilePath, true))
            {
                // Iterate through each LineRenderer and its points
                foreach (LineRenderer lineRenderer in lineRenderers)
                {
                    int previousNodeIndex = -1; // Track the index of the previous point in the line
                    for (int i = 0; i < lineRenderer.positionCount; i++)
                    {
                        Vector3 point = lineRenderer.GetPosition(i);

                        // Check if the point already exists -intersection case
                        //if (!nodeIndices.ContainsKey(point))
                        if (!nodeIndices.TryGetValue(point, out int currentNode))
                        {
                            
                            nodeIndices.Add(point, currentNodeIndex);
                            //nodeWriter.WriteLine($"{point.x} {point.y} {point.z}"); // Write the point to the file

                            //---------------------------------- RADII !--------------------------
                            
                            float normalizedPosition = i / (float)(lineRenderer.positionCount - 1);

                            float pointWidth = lineRenderer.widthCurve.Evaluate(normalizedPosition);

                            nodeWriter.WriteLine($"{point.x} {point.y} {point.z} {pointWidth}");
                            //---------------------------------------------------------------------

                            currentNodeIndex++;
                        }

                        
                        currentNode = nodeIndices[point];
                        //int currentNode = nodeIndices[point];

                       
                        if (previousNodeIndex != -1 && previousNodeIndex != currentNode)
                        {
                            // Avoid self-loop 
                            intersectionConnections.Add((previousNodeIndex, currentNode));
                            edgeWriter.WriteLine($"c {previousNodeIndex} {currentNode}"); 
                        }

                        
                        previousNodeIndex = currentNode;
                    }
                }
            }
            Debug.Log("All points and edges saved successfully.");
        }
        else
        {
            Debug.LogWarning("No active newContainer found.");
        }
    }



    public void TogglePreview()
    {
        PreviewTimesNum++;
        isPreview = !isPreview;

        if (newContainer != null)
        {
            if (isPreview)
            {
                previewContainer = new GameObject($"PreviewContainer_{lineContainerCount}");
                previewContainer.tag = "PreviewContainer";
                LineRenderer[] lineRenderers = newContainer.GetComponentsInChildren<LineRenderer>();
                //Debug.Log("Previewing : "+ newContainer.name);
                int line = 0;
                foreach (LineRenderer originalLineRenderer in lineRenderers)
                {
                    GameObject previewContainerLine = new GameObject($"PreviewContainerLine_{lineContainerCountLines}");
                    lineContainerCountLines++;
                    previewContainerLine.tag = "PreviewContainerLine";
                    previewContainerLine.transform.SetParent(previewContainer.transform);
                    //Debug.Log("Previewing line: " + line);
                    int numOriginalPoints = originalLineRenderer.positionCount;
                    Vector3[] originalPoints = new Vector3[numOriginalPoints];
                    originalLineRenderer.GetPositions(originalPoints);

                    //List<Vector3> resampledPoints = ResamplePoints(originalPoints); // Number spheres
#if UNITY_ANDROID
                  
                    pointMaterial = VRpointMaterial;
#elif UNITY_STANDALONE_OSX
                   
                    //pointMaterial = pointMaterial;
#endif

                    //Change width of preview points
                    for (int i = 0; i < originalPoints.Length; i++)
                    {
                        Vector3 point = originalPoints[i];

                        GameObject pointMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        pointMarker.transform.position = point;

                        // Calculate the normalized position
                        float normalizedPosition = i / (float)(numOriginalPoints - 1);
                        float radius = originalLineRenderer.widthCurve.Evaluate(normalizedPosition) * 0.5f; 

                 
                        pointMarker.transform.localScale = Vector3.one * 3f * radius;
                        pointMarker.GetComponent<Renderer>().material = pointMaterial;
                        pointMarker.transform.SetParent(previewContainerLine.transform);
                    }


                    line++;
                }
            }
            else
            {
                if (previewContainer != null)
                {
                    //previewContainer.SetActive(false);
                    Destroy(previewContainer);
                }
            }
        }
        else
        {
            Debug.LogError("No sketch to toggle preview on.");
        }
    }



    [System.Serializable]
    public class DrawnLine
    {
        public LineRenderer lineRenderer;
        public List<GameObject> spheres = new List<GameObject>();
    }

    //private Dictionary<Vector3, int> pointReferenceCount = new Dictionary<Vector3, int>();
    //intersectionCounts
    public void UndoLastLine()
    {
        // Check if there's a container to undo from
        if (newContainer != null)
        {
            LineRenderer[] lineRenderers = newContainer.GetComponentsInChildren<LineRenderer>();

        
            if (lineRenderers.Length > 0)
            {
                UndoSketchedLinesNum++;

           
                LineRenderer lastLine = lineRenderers[lineRenderers.Length - 1];
                List<Vector3> pointsToRemove = new List<Vector3>();
                for (int i = 0; i < lastLine.positionCount; i++)
                {
                    Vector3 point = lastLine.GetPosition(i);
                    //Debug.Log($"Undoing point at index {i}: {point}");

                    // Check if the point exists in the intersectionCounts 
                    if (intersectionCounts.ContainsKey(point))
                    {
                        intersectionCounts[point]--;

                        if (intersectionCounts[point] == 1)
                        {
                            //pointsToRemove.Add(point);
                            intersectionCounts.Remove(point);
                            removeAllIntersectedVisualizations(point);
                            //Debug.Log($"Intersection count reached zero, removing point: {point}");
                        }
                    }
                    else
                    {
                        // If the point is not in the intersectionCounts dictionary, mark it for removal
                        pointsToRemove.Add(point);
                    }
                }

                // Remove points datastructures
                foreach (Vector3 point in pointsToRemove)
                {
                    //allPoints.Remove(point);
                   
                    for (int i = allPoints.Count - 1; i >= 0; i--)
                    {
                      
                        if (Vector3.Distance(allPoints[i], point) <= 0.002f) 
                        {
                            allPoints.RemoveAt(i);
                            //Debug.Log($"Removed point from allPoints: {point}");
                            //break; // Exit the loop once the point is found and removed
                        }
                    }
                    allPoints.Remove(point);
                    clusterManager.RemovePointFromCluster(point);
                    //Debug.Log($"Removed point from KD-Tree and clusters: {point}");
                }

                if (allPoints.Count > 0)
                {
                    //Debug.Log("Creating new KDTree.");
                    NewTree = true;
                    UpdateKDTree(allPoints);
                }
                else
                {
                    NewTree = true;
                    kdTree = null;
                    clusterManager = new ClusteringDrawing();
                    //Debug.Log("No points left. KD-Tree and clusters updated.");

                }
                DestroyImmediate(lastLine.gameObject);
                //Debug.Log("Last line removed from the active container.");
                if (isPreview)
                {
                    TogglePreview();
                    TogglePreview();         
                }
            }
            else
            {
                Debug.LogWarning("Nothing to Undo.");
            }
        }
    }

    private void AddMeshColliderToLine(LineRenderer lineRenderer)
    {
       
        Mesh mesh = new Mesh();
        lineRenderer.BakeMesh(mesh, true);

        MeshCollider meshCollider = lineRenderer.gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;
        //meshCollider.isTrigger = true; // Make it a trigger for easy interaction
    }


    private void UpdateAfterEdit()
    {
        LinePointPokeMover pokeMover = FindObjectOfType<LinePointPokeMover>();

        if (pokeMover == null || pokeMover.updatedLines.Count == 0)
        {
            Debug.LogWarning("No lines were updated.");
            return;
        }

        //Debug.LogWarning("Updating sketch after edit.");

        foreach (var entry in pokeMover.oldupdatedLines)
        {
            //Debug.LogWarning("Deleting old line.");

            LineRenderer oldLine = entry.Key;
            List<Vector3> pointsFromDictionary = entry.Value;

            DeleteLineWithoutDestrying(oldLine, pointsFromDictionary);
        }

        foreach (var entry in pokeMover.updatedLines)
        {
            LineRenderer updatedLine = entry.Key;
            List<Vector3> newPoints = entry.Value;

            // Create a new LineRenderer with the updated points
            //LineRenderer newLine = CreateNewLineRenderer();
            //updatedLine.positionCount = newPoints.Count;
            //updatedLine.SetPositions(newPoints.ToArray());

            AddLineToDataStructures(updatedLine);
        }

        pokeMover.updatedLines.Clear();
        pokeMover.oldupdatedLines.Clear();
    }


    private void AddLineToDataStructures(LineRenderer line)
    {
        List<Vector3> pointsToAdd = new List<Vector3>();
        for (int i = 0; i < line.positionCount; i++)
        {
            Vector3 point = line.GetPosition(i);
            allPoints.Add(point);
            pointsToAdd.Add(point);
            //clusterManager.AddPointToCluster(point);
        }
        NewTree = true;
        UpdateKDTree(allPoints);

        if (pointsToAdd.Count > 1)
        {
            clusterManager.AddPointToCluster(pointsToAdd[0], true); // First point as initial centroid
            clusterManager.AddPointToCluster(pointsToAdd[pointsToAdd.Count - 1], true); // Last point as initial centroid
        }

        // Add points to clusters
        foreach (Vector3 point in pointsToAdd)
        {
            clusterManager.AddPointToCluster(point);
        }

        //Debug.Log($"Added {pointsToAdd.Count} points from the new line to data structures.");
    }


    private void UpdateAfterMedium()
    {
        //Debug.LogWarning("Updating sketch after medium.");
        //DeleteLineWithoutDestrying(oldLine, pointsFromDictionary);
        AddLineToDataStructures(currentDrawing);
    }



    //Check if a line is close enough to another
    private (bool, List<Vector3>) checkSimilarLineExists(List<Vector3> newLinePoints)
    {
        if (newContainer != null)
        {
            //currentDrawing  //line rednderer of current line
            ///newLinePoints // points of the line
            LineRenderer[] lineRenderers = newContainer.GetComponentsInChildren<LineRenderer>();

            if (lineRenderers.Length > 1)
            {
                foreach (LineRenderer checkLine in lineRenderers) {

                    if (checkLine == currentDrawing)
                    {
                        continue;
                    }

                    if (checkSimilarity(currentDrawing, checkLine))
                    {
                        //LineRenderer newLine = currentDrawing;
                        //Debug.Log("Found similar line. Creating a medium line.");
                        List<Vector3> mediumLinePoints = CreateMediumLine(currentDrawing, checkLine);
                        //DeleteLine(checkLine);

                        return (true, mediumLinePoints);
                        
                    }
                }
            }
        }
        //Debug.Log("Couldn't find similar line.");
        return (false, newLinePoints);
    }

    private bool checkSimilarity(LineRenderer currentLine, LineRenderer oldLine)
    {
        float proximityThreshold = 0.05f; // Threshold to consider points close 
        int minimumSimilarPoints = 90;  // Percentage of points required to be similar

        // Get points from both lines
        List<Vector3> oldLinePoints = GetLinePoints(oldLine);
        List<Vector3> currentLinePoints = GetLinePoints(currentLine);

        KDTree kdTree = new KDTree(oldLinePoints);

        int closePointCount = 0;

        // Check each point in the current line against the KD-Tree of the old line
        foreach (var point in currentLinePoints)
        {
            Vector3 nearestPoint = kdTree.FindNearestNeighbor(point);
            float distance = Vector3.Distance(point, nearestPoint);

            if (distance < proximityThreshold)
            {
                closePointCount++;
            }
        }

    
        float similarityRatio = (float)closePointCount / currentLinePoints.Count * 100;
        //Debug.Log("Similarity Ratio : " + similarityRatio);
        return similarityRatio >= minimumSimilarPoints;
    }

    private List<Vector3> GetLinePoints(LineRenderer lineRenderer)
    {
        List<Vector3> points = new List<Vector3>();
        int numPoints = lineRenderer.positionCount;
        for (int i = 0; i < numPoints; i++)
        {
            points.Add(lineRenderer.GetPosition(i));
        }
        return points;
    }

    private List<Vector3> CreateMediumLine(LineRenderer currentLine, LineRenderer similarLine)
    {
        List<Vector3> mediumLinePoints = CalculateMediumLine(currentLine, similarLine);

        DestroyImmediate(currentDrawing.gameObject);
        currentDrawing = similarLine;
        return mediumLinePoints;
    }


    private List<Vector3> CalculateMediumLine(LineRenderer currentLine, LineRenderer similarLine)
    {
        List<Vector3> line1Points = GetLinePoints(currentLine);
        List<Vector3> line2Points = GetLinePoints(similarLine);
        List<GameObject> points = new List<GameObject>();

        foreach (Transform child in similarLine.transform)
        {
            if (child.CompareTag("LinePoint"))
            {
                points.Add(child.gameObject);
            }
        }

        KDTree LinekdTree = new KDTree(line1Points);
        List<Vector3> mediumLinePoints = new List<Vector3>();
        float influenceThreshold = 0.04f; // Threshold
        List<Vector3> fixedPoints = new List<Vector3>();


        for (int i = 0; i < line2Points.Count; i++)
        {
            Vector3 oldPoint = line2Points[i];
            Vector3 closestPoint = LinekdTree.FindNearestNeighbor(oldPoint);
            float distance = Vector3.Distance(closestPoint, oldPoint);

            Vector3 newPoint;
            if (intersectionCounts.ContainsKey(oldPoint))
            {
                // If the closest point is an intersection-> keep it unchanged
                newPoint = oldPoint;
                fixedPoints.Add(newPoint);
                //Debug.Log($"newPoint: {newPoint} == oldPoint");
            }
            else if (distance <= influenceThreshold)
            {
                newPoint = Vector3.Lerp(closestPoint, oldPoint, 0.5f);
                //Debug.Log($"newPoint: {newPoint} , oldPoint:{oldPoint}");
                
            }
            else
            {
                newPoint = oldPoint;
                //Debug.Log($"newPoint: {newPoint} == oldPoint");
            }

            mediumLinePoints.Add(newPoint);
        }

        DeleteLineWithoutDestrying(similarLine, line2Points);
        List<Vector3> smoothedPoints = ApplyLaplacianSmoothing(mediumLinePoints, fixedPoints, 0.2f, 5);
        similarLine.positionCount = smoothedPoints.Count; // mediumLinePoints
        similarLine.SetPositions(smoothedPoints.ToArray()); //mediumLinePoints

        for (int i = 0; i < smoothedPoints.Count; i++)
        {
            points[i].transform.position = smoothedPoints[i];
        }

            return smoothedPoints; //mediumLinePoints
    }


    //Delete line after creating medium line
    public void DeleteLine(LineRenderer line)
    {
        if (newContainer != null)
        {
            LineRenderer[] lineRenderers = newContainer.GetComponentsInChildren<LineRenderer>();

            if (lineRenderers.Length > 0)
            {
                List<Vector3> pointsToRemove = new List<Vector3>();
                for (int i = 0; i < line.positionCount; i++)
                {
                    Vector3 point = line.GetPosition(i);

                    if (intersectionCounts.ContainsKey(point))
                    {
                        intersectionCounts[point]--;
                        if (intersectionCounts[point] == 1)
                        {
                            //pointsToRemove.Add(point);
                            intersectionCounts.Remove(point);
                            removeAllIntersectedVisualizations(point);

                        }
                    }
                    else
                    {
                        pointsToRemove.Add(point);
                    }
                }

                foreach (Vector3 point in pointsToRemove)
                {
                    for (int i = allPoints.Count - 1; i >= 0; i--)
                    {
                        if (Vector3.Distance(allPoints[i], point) <= 0.002f)
                        {
                            allPoints.RemoveAt(i);
                            //Debug.Log($"Removed point from allPoints: {point}");
                        }
                    }
                    allPoints.Remove(point);
                    clusterManager.RemovePointFromCluster(point);
                    //Debug.Log($"Removed point from KD-Tree and clusters: {point}");
                }

                NewTree = true;
                UpdateKDTree(allPoints);
                DestroyImmediate(line.gameObject);


            }
            else
            {
                Debug.LogWarning("Nothing to Delete.");
            }
        }
    }

    //Delete line after edit point/line
    public void DeleteLineWithoutDestrying(LineRenderer line, List<Vector3> pointsFromDictionary)
    {
        if (newContainer != null)
        {
            LineRenderer[] lineRenderers = newContainer.GetComponentsInChildren<LineRenderer>();

            foreach(Vector3 point in pointsFromDictionary)
            {
                //Debug.Log($"Deleting point: {point}");
                for (int i = allPoints.Count - 1; i >= 0; i--)
                {
                    //if (Vector3.Distance(allPoints[i], point) <= 0.001f) // Adjust tolerance as needed
                    if (allPoints[i] == point)
                    {
                        allPoints.RemoveAt(i);
                    }
                }
                allPoints.Remove(point);
                clusterManager.RemovePointFromCluster(point);
            }

            if (allPoints.Count > 0)
            {
                NewTree = true;
                UpdateKDTree(allPoints);
            }
            else
            {
                NewTree = true;
                kdTree = null;
                clusterManager = new ClusteringDrawing();
            }
        }
        else
        {
            Debug.LogWarning("Nothing to delete.");
        }
    }

    public void Mirror()
    {
        //Debug.Log("Mirroring.");

        if (newContainer != null)
        {
            LineRenderer[] lineRenderers = newContainer.GetComponentsInChildren<LineRenderer>();

            // Check if there are any lines to mirror
            if (lineRenderers.Length > 0)
            {
                // Get the last LineRenderer
                LineRenderer lastLine = lineRenderers[lineRenderers.Length - 1];
                if (lastLine.gameObject.tag != "Mirrored") {
                    // Get the original points of the line
                    int positionCount = lastLine.positionCount;
                    Vector3[] originalPoints = new Vector3[positionCount];
                    lastLine.GetPositions(originalPoints);

                    // Define the axis to mirror across (X-axis)
                    Vector3 mirrorAxis = Vector3.right; // Change-> to Vector3.up or Vector3.forward ->for other axes

                    // Calculate the mirrored points
                    Vector3[] mirroredPoints = new Vector3[positionCount];
                    for (int i = 0; i < positionCount; i++)
                    {
                        //Debug.Log("Before:" + originalPoints[i]);
                        mirroredPoints[i] = Vector3.Reflect(originalPoints[i], mirrorAxis);
                        //Debug.Log("After:" + mirroredPoints[i]);
                    }
                
                    Vector3 originalLineEnd = originalPoints[0];
                    Vector3 mirroredLineStart = mirroredPoints[0]; // First point of the mirrored line
                    Vector3 offset = originalLineEnd - mirroredLineStart;

                    for (int i = 0; i < positionCount; i++)
                    {
                        // Add a small random offset to avoid identical points
                        Vector3 epsilonOffset = new Vector3(
                            Random.Range(-0.001f, 0.001f),
                            Random.Range(-0.001f, 0.001f),
                            Random.Range(-0.001f, 0.001f)
                        );
                        mirroredPoints[i] += offset + epsilonOffset;
                    }

                    // Create a new GameObject for the mirrored line
                    GameObject mirroredLineObject = new GameObject($"MirroredLine_{lineRenderers.Length}");
                    mirroredLineObject.gameObject.tag = "Mirrored";
                    mirroredLineObject.transform.SetParent(newContainer.transform);

                    LineRenderer mirroredLine = mirroredLineObject.AddComponent<LineRenderer>();

                    mirroredLine.widthCurve = lastLine.widthCurve;
                    mirroredLine.material = lastLine.material;
                    mirroredLine.startColor = lastLine.startColor;
                    mirroredLine.endColor = lastLine.endColor;
                    mirroredLine.startWidth = lastLine.startWidth;
                    mirroredLine.endWidth = lastLine.endWidth;

        
                    mirroredLine.positionCount = positionCount;
                    mirroredLine.SetPositions(mirroredPoints);

                    currentDrawing = mirroredLine;
                    //Debug.Log("Mirroring completed.");
                }
            }
            else
            {
                Debug.LogWarning("No lines found to mirror.");
            }
        }
        else
        {
            Debug.LogError("No container found.");
        }
    }


}
