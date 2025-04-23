using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("XR/Line Point Poke Mover")]
public class LinePointPokeMover : MonoBehaviour
{
    [Header("Controller Input")]
    [SerializeField]
    private InputActionReference pokeAction; 
    [SerializeField]
    private InputActionReference moveAction; 

    [Header("Pointer and Controller References")]
    public Transform pointerTransform; 
    public Transform rightControllerTransform; 

    [Header("Materials")]
    public Material hoverMaterial;
    public Material defaultMaterial;

    [Header("Movement Settings")]
    public float selectionRadius = 0.03f; // Radius for selecting points
    public float smoothingSpeed = 5.0f;

    private GameObject hoveredPoint;
    private GameObject selectedPoint;
    private LineRenderer selectedLine;
    private int selectedPointIndex = -1;
    private bool isPointSelected = false;
    private Vector3 initialOffset;

    private Vector3[] velocities;
    public static bool editApplied = false;
    public static List<Vector3> allEditedPoints = new List<Vector3>();
    private bool pointsAdded = false;

    public static int MovedPointNum = 0;

    public Dictionary<LineRenderer, List<Vector3>> updatedLines = new Dictionary<LineRenderer, List<Vector3>>();
    public Dictionary<LineRenderer, List<Vector3>> oldupdatedLines = new Dictionary<LineRenderer, List<Vector3>>();

    private void Update()
    {
        // Only handle hover when a point is not currently being moved
        if (!isPointSelected || (moveAction != null && !moveAction.action.IsPressed()))
        {
            HandleHover();
        }

        if (pokeAction != null && pokeAction.action.WasPressedThisFrame())
        {
            TrySelectPoint();
        }

        if (isPointSelected && moveAction != null && moveAction.action.IsPressed())
        {
            MoveSelectedPoint();
        }
        else if (isPointSelected && moveAction != null && moveAction.action.WasReleasedThisFrame())
        {
            MovedPointNum++;
            DeselectPoint();
            UpdateEditedPoints();
            sameLine = false;
            //Debug.Log("------DESELECT------");
        }
    }


    private void HandleHover()
    {
        GameObject closestPoint = FindClosestPoint();
        if (closestPoint != null && closestPoint != hoveredPoint)
        {
            ResetHoveredPoint();
            hoveredPoint = closestPoint;
            ApplyHoverEffect(hoveredPoint);
        }
        else if (closestPoint == null)
        {
            ResetHoveredPoint();
        }
    }


    private void ApplyHoverEffect(GameObject point)
    {
        if (point != null)
        {
            MeshRenderer renderer = point.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = hoverMaterial;
        }
    }

    private void ResetHoveredPoint()
    {
        if (hoveredPoint != null)
        {
            MeshRenderer renderer = hoveredPoint.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = defaultMaterial;
            hoveredPoint = null;
        }
    }


    private GameObject FindClosestPoint()
    {
        Collider[] hitColliders = Physics.OverlapSphere(pointerTransform.position, selectionRadius);

        GameObject closestPoint = null;
        float closestDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("LinePoint"))
            {
                float distance = Vector3.Distance(pointerTransform.position, hitCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = hitCollider.gameObject;
                }
            }
        }

        return closestPoint;
    }


    private void TrySelectPoint()
    {
        GameObject closestPoint = FindClosestPoint();
        if (closestPoint != null)
        {
            selectedPoint = closestPoint;
            selectedLine = selectedPoint.GetComponentInParent<LineRenderer>();

            if (selectedLine != null)
            {
                // Find the index of the selected point in the LineRenderer
                for (int i = 0; i < selectedLine.positionCount; i++)
                {
                    if (selectedLine.GetPosition(i) == selectedPoint.transform.position)
                    {
                        selectedPointIndex = i;
                        break;
                    }
                }

                isPointSelected = true;
                initialOffset = selectedPoint.transform.position - rightControllerTransform.position;
                //Debug.Log("Point selected for moving!");
            }
        }
    }


    [SerializeField] private List<GameObject> points = new List<GameObject>(); // List of point GameObjects
    private Dictionary<int, Vector3> originalPositions = new Dictionary<int, Vector3>();
    private Vector3 localLastPosition;
    private bool sameLine = false;

    private void MoveSelectedPoint()
    {
        if (selectedPoint != null && selectedLine != null && selectedPointIndex != -1)
        {
            List<Vector3> oldPositions = new List<Vector3>();
            PopulatePointsList();
            //InitializeVelocities();

            foreach (var point in points)
            {
                oldPositions.Add(point.transform.position);
            }

            if (!Design.intersectionCounts.ContainsKey(oldPositions[selectedPointIndex]))
            {


                if (!updatedLines.ContainsKey(selectedLine))
                {
                    UpdateOldLineData(selectedLine);
                }
                //Debug.Log($"Trying to move point: {selectedPoint} ");
                //PopulatePointsList();

                Vector3 lastPosition;
                if (oldupdatedLines.TryGetValue(selectedLine, out List<Vector3> initialPositions))
                {
                    //PopulatePointsList();
                    lastPosition = initialPositions[selectedPointIndex]; // Get the initial position of the selected point
                }
                else
                {
                    // If no entry exists in oldupdatedLines, fallback to current position
                    lastPosition = selectedPoint.transform.position;
                    Debug.LogWarning($"No entry in oldupdatedLines for {selectedLine.name}. Using current position as fallback.");
                }
                if (sameLine == false)
                {
                    localLastPosition = selectedPoint.transform.position;
                    //Debug.Log("---localLastPosition RESET----");
                }

                // Move the selected point to the pointer position
                Vector3 newPosition = pointerTransform.position;
                selectedPoint.transform.position = Vector3.Lerp(
                    selectedPoint.transform.position,
                    newPosition,
                    Time.deltaTime * smoothingSpeed
                );
                selectedLine.SetPosition(selectedPointIndex, selectedPoint.transform.position);
                points[selectedPointIndex].transform.position = selectedPoint.transform.position;
                Vector3 currentPosition = points[selectedPointIndex].transform.position;
                //ApplyLaplacianSmoothing(points, selectedPointIndex);
                //PropagatePointMovement(lastPosition, selectedPoint.transform.position, initialPositions);

                ApplyProjectionMovement(points, selectedPointIndex, localLastPosition, currentPosition);

                // Mark the line as updated
                UpdateLineData(selectedLine, points);
                sameLine = true;
            }

        }

    }

    //private void PropagatePointMovement(Vector3 lastPosition, Vector3 newPosition, List<Vector3> InitialPoints)
    //{
    //    if (selectedLine == null || selectedPointIndex == -1) return;
    //    editApplied = true;
    //    pointsAdded = true;
    //    //List<Vector3> smoothedPositions = new List<Vector3>();
    //    List<Vector3> smoothedPositions = new List<Vector3>(InitialPoints);


    //    Vector3 movement = newPosition - lastPosition; // Calculate the movement vector
    //    float maxDistance = movement.magnitude; // Maximum distance moved by the selected point
    //    //Debug.Log($"Last Position: {lastPosition}, New Position: {newPosition}");
    //    //Debug.Log($"Point movement: {movement}");
    //    if (!Design.intersectionCounts.ContainsKey(smoothedPositions[selectedPointIndex]))
    //    {
    //        // Forward 
    //        for (int i = selectedPointIndex + 1; i < InitialPoints.Count; i++)
    //        {
    //            //Debug.Log($"Old position: {smoothedPositions[i]}");
    //            if (Design.intersectionCounts.ContainsKey(smoothedPositions[i]))
    //                break; // Stop at intersections

    //            float distanceToSelected = Vector3.Distance(smoothedPositions[selectedPointIndex], smoothedPositions[i]);
    //            float weight = Mathf.Clamp01(1f - (distanceToSelected / maxDistance)); 

    //            Vector3 newPointPosition = smoothedPositions[i] + movement * weight;
    //            //Vector3 newPointPosition = smoothedPositions[i] + movement ;
    //            //InitialPointsGO[i].transform.position = newPointPosition;
    //            points[i].transform.position = newPointPosition;
    //            selectedLine.SetPosition(i, newPointPosition);
    //            //Debug.Log($"New position: {newPointPosition}");
    //        }

    //        // Backward 
    //        for (int i = selectedPointIndex - 1; i >= 0; i--)
    //        {
    //            //Debug.Log($"Old position: {smoothedPositions[i]}");
    //            if (Design.intersectionCounts.ContainsKey(smoothedPositions[i]))
    //                break; // Stop at intersections

    //            float distanceToSelected = Vector3.Distance(smoothedPositions[selectedPointIndex], smoothedPositions[i]);
    //            float weight = Mathf.Clamp01(1f - (distanceToSelected / maxDistance)); 

    //            Vector3 newPointPosition = smoothedPositions[i] + movement * weight;
    //            //Vector3 newPointPosition = smoothedPositions[i] + movement ;
    //            //InitialPointsGO[i].transform.position = newPointPosition;
    //            points[i].transform.position = newPointPosition;
    //            selectedLine.SetPosition(i, newPointPosition);
    //            //Debug.Log($"New position: {newPointPosition}");
    //        }
    //    }
    //    //Debug.Log("Updated Points.");

    //    if (Design.isPreview)
    //    {
    //        designContainer.TogglePreview();
    //        designContainer.TogglePreview();
    //    }
    //}


    private void UpdateLineData(LineRenderer line, List<GameObject> points)
    {
        if (line == null) return;

        List<Vector3> newPositions = points.Select(p => p.transform.position).ToList();

        if (updatedLines.ContainsKey(line))
        {
            updatedLines[line] = newPositions; 
        }
        else
        {
            updatedLines.Add(line, newPositions); 
        }
    }

    private void UpdateOldLineData(LineRenderer line)
    {
        if (line == null) return;

        List<Vector3> newPositions = new List<Vector3>();
        for (int i = 0; i < line.positionCount; i++)
        {
            newPositions.Add(line.GetPosition(i));
        }

        if (oldupdatedLines.ContainsKey(line))
        {
            oldupdatedLines[line] = newPositions; // Update existing entry
        }
        else
        {
            oldupdatedLines.Add(line, newPositions); // Add new entry
        }

        //Debug.Log($"Updated old line data for {line.name} with {newPositions.Count} points.");
    }

    public Design designContainer;
    private void ApplyLaplacianSmoothing(List<GameObject> points, int selectedPointIndex, float lambda = 0.2f)
    {
        editApplied = true;
        pointsAdded = true;
        List<Vector3> smoothedPositions = new List<Vector3>();

   
        foreach (var point in points)
        {
            smoothedPositions.Add(point.transform.position);
        }

        if (!Design.intersectionCounts.ContainsKey(smoothedPositions[selectedPointIndex]))
        {

            for (int i = selectedPointIndex + 1; i < points.Count - 1; i++)
            {
                if (Design.intersectionCounts.ContainsKey(smoothedPositions[i]))
                    break;
                Vector3 smoothedPos = (1 - lambda) * smoothedPositions[i] + lambda * (smoothedPositions[i - 1] + smoothedPositions[i + 1]) / 2.0f;
                points[i].transform.position = smoothedPos;
                selectedLine.SetPosition(i, smoothedPos);
            }
            for (int i = selectedPointIndex - 1; i > 0; i--)
            {
                if (Design.intersectionCounts.ContainsKey(smoothedPositions[i]))
                    break;
                Vector3 smoothedPos = (1 - lambda) * smoothedPositions[i] + lambda * (smoothedPositions[i - 1] + smoothedPositions[i + 1]) / 2.0f;
                points[i].transform.position = smoothedPos;
                selectedLine.SetPosition(i, smoothedPos);
       
            }
        }
        if (Design.isPreview)
        {
            designContainer.TogglePreview();
            designContainer.TogglePreview();
        }

        //UpdateLineData(selectedLine, points);
    }




    private void ApplyProjectionMovement(List<GameObject> points, int selectedPointIndex, Vector3 lastPosition, Vector3 newPosition)
    {
        editApplied = true;
        pointsAdded = true;
        List<Vector3> smoothedPositions = new List<Vector3>();

        if (points == null || selectedPointIndex < 0 || selectedPointIndex >= points.Count) return;

        // Calculate the movement direction and magnitude
        Vector3 movementDirection = newPosition - lastPosition;
        //Debug.Log("MovementDirection: " + movementDirection);

        if (movementDirection == Vector3.zero) return; // No movement

        float movementMagnitude = movementDirection.magnitude; 
        if (movementMagnitude <= 0.001f) return; // Ignore insignificant movements

        movementDirection.Normalize(); // Normalize the direction for consistent scaling

        
        int startIndex = Mathf.Max(selectedPointIndex - 100, 0);
        int endIndex = Mathf.Min(selectedPointIndex + 100, selectedLine.positionCount - 1);
        startIndex = Mathf.Clamp(startIndex, 0, points.Count - 1);
        endIndex = Mathf.Clamp(endIndex, 0, points.Count - 1);

        foreach (var point in points)
        {
            smoothedPositions.Add(point.transform.position);
        }

        if (!Design.intersectionCounts.ContainsKey(smoothedPositions[selectedPointIndex]))
        {
       
            for (int i = selectedPointIndex; i <= endIndex; i++)
            {
                if (i == selectedPointIndex) continue; // Skip the selected point itself
                if (Design.intersectionCounts.ContainsKey(smoothedPositions[i]))
                    break;

                
                float distance = Mathf.Abs(i - selectedPointIndex);
                //float weight = 1f - (distance / 8f); 
                float weight;
                if (distance == 1)
                {
                    weight = 0.95f; 
                }
                else
                {
                    weight = 1f - (distance * 0.05f);
                    //weight = Mathf.Clamp01(1f - ((distance - 1) / 9f)); 
                }


                if (weight <= 0f)
                {
                    // New decay function: exponential decay 
                    weight = Mathf.Exp(-distance * 0.25f); // Exponential decay factor
                    // Ensure some minimum influence even for far points
                    weight = Mathf.Clamp(weight, 0.01f, 1f);
                    //continue; 
                }

                
                Vector3 originalPosition = points[i].transform.position;
                Vector3 projection = originalPosition + (movementDirection * movementMagnitude * weight);
                //Debug.Log($"Point {i}: weight={weight}, projection={projection}, originalPosition={originalPosition}");

               
                points[i].transform.position = projection;
                selectedLine.SetPosition(i, projection);
            }


            for (int i = selectedPointIndex; i >= startIndex; i--)
            {
                if (i == selectedPointIndex) continue; 
                if (Design.intersectionCounts.ContainsKey(smoothedPositions[i]))
                    break;

                
                float distance = Mathf.Abs(i - selectedPointIndex);
                //float weight = 1f - (distance / 8f); 
                float weight;
                if (distance == 1)
                {
                    weight = 0.95f; 
                }
                else
                {
                    weight = 1f - (distance * 0.05f);
                    //weight = Mathf.Clamp01(1f - ((distance - 1) / 9f)); 
                }


                if (weight <= 0f)
                {
                    // New decay function: exponential decay for a smooth falloff
                    weight = Mathf.Exp(-distance * 0.25f); // Exponential decay factor

                    // Ensure some minimum influence even for far points
                    weight = Mathf.Clamp(weight, 0.01f, 1f);
                    //continue; // Skip points with negligible influence
                }

                // Calculate the projected position
                Vector3 originalPosition = points[i].transform.position;
                Vector3 projection = originalPosition + (movementDirection * movementMagnitude * weight);
                //Debug.Log($"Point {i}: weight={weight}, projection={projection}, originalPosition={originalPosition}");

                
                points[i].transform.position = projection;
                selectedLine.SetPosition(i, projection);
            }

            //Laplacian smoothing

            //for (int j = 1; j < 4; j++) {
            //    for (int i = startIndex + 1; i < endIndex; i++) // Skip first and last points
            //    {
            //        if (i <= 0 || i >= points.Count - 1) continue; // Skip if out of bounds
            //        if (i == selectedPointIndex) continue; // Skip the selected point itself
            //        if (Design.intersectionCounts.ContainsKey(smoothedPositions[i]))
            //            break;

            //        // Ensure indices are valid before accessing them
            //        if (i - 1 < 0 || i + 1 >= points.Count) continue;

            //        Vector3 previous = points[i - 1].transform.position;
            //        Vector3 next = points[i + 1].transform.position;
            //        Vector3 current = points[i].transform.position;

            //        // Apply Laplacian smoothing
            //        Vector3 smoothedPosition = (previous + next) / 2f;
            //        points[i].transform.position = Vector3.Lerp(current, smoothedPosition, 0.2f);

            //        // Update the LineRenderer
            //        selectedLine.SetPosition(i, points[i].transform.position);
            //    }
            //}

            // Update the last position
            localLastPosition = newPosition;
        }

        if (Design.isPreview)
        {
            designContainer.TogglePreview();
            designContainer.TogglePreview();
        }

    }







    //private void InitializeOriginalPositions()
    //{
    //    originalPositions.Clear();
    //    for (int i = 0; i < points.Count; i++)
    //    {
    //        originalPositions[i] = points[i].transform.position;
    //    }
    //}
    //private void InitializeVelocities()
    //{
    //    // Initialize the velocities array with the correct size
    //    velocities = new Vector3[points.Count];

    //    // Set initial values to zero
    //    for (int i = 0; i < points.Count; i++)
    //    {
    //        velocities[i] = Vector3.zero;
    //    }
    //}

    private void PopulatePointsList()
    {
        points.Clear();
        if (selectedLine != null)
        {
            // Iterate through all child objects of the selected line
            foreach (Transform child in selectedLine.transform)
            {
                if (child.CompareTag("LinePoint"))
                {
                    points.Add(child.gameObject);
                }
            }
        }

        //Debug.Log($"Points list populated with {points.Count} points.");
    }


    private void DeselectPoint()
    {
        if (isPointSelected)
        {
            selectedPoint = null;
            selectedLine = null;
            selectedPointIndex = -1;
            isPointSelected = false;
            //Debug.Log("Point deselected.");
        }
    }


    private void UpdateEditedPoints()
    {
        if (pointsAdded)
        {
            pointsAdded = false;
        }
    }
}

