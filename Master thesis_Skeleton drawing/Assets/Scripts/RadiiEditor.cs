using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

//[AddComponentMenu("XR/Line Point Poke Mover")]
public class RadiiEditor : MonoBehaviour
{
    [Header("Controller Input")]
    [SerializeField]
    private InputActionReference pokeAction; // Action to select points
    [SerializeField]
    private InputActionReference moveAction; // Action to move points

    //[SerializeField]
    //private InputActionReference increaseWidthAction; // Action to increase width
    //[SerializeField]
    //private InputActionReference decreaseWidthAction; // Action to decrease width

    [SerializeField]
    private InputActionReference joystickAction; // Bind to the joystick input

    [Header("Pointer and Controller References")]
    public Transform pointerTransform; // Reference to your custom pointer object
    public Transform rightControllerTransform; // Controller Transform

    [Header("Materials")]
    public Material hoverMaterial;
    public Material defaultMaterial;
    public Material rangeSphereMaterial; // Material for the range sphere

    [Header("Movement Settings")]
    public float selectionRadius = 0.03f; // Radius for selecting points
    public float smoothingSpeed = 5.0f;


    private GameObject rangeSphereInstance; // The sphere to visualize the range


    private GameObject hoveredPoint;
    private GameObject selectedPoint;
    private LineRenderer selectedLine;
    private int selectedPointIndex = -1;
    private bool isPointSelected = false;
    private Vector3 initialOffset;


    public Dictionary<LineRenderer, List<Vector3>> updatedLines = new Dictionary<LineRenderer, List<Vector3>>();
    public Dictionary<LineRenderer, List<Vector3>> oldupdatedLines = new Dictionary<LineRenderer, List<Vector3>>();

    private void Start()
    {
        rangeSphereInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rangeSphereInstance.transform.localScale = Vector3.one * 0.02f; // Adjust the initial size
        rangeSphereInstance.GetComponent<Renderer>().material = rangeSphereMaterial;
        rangeSphereInstance.SetActive(false); // Hide it initially
    }


    private void Update()
    {

        UpdateClosestSegment();
        
        UpdateRangeSphere(); // Update the position and visibility of the range sphere

        // Only handle hover when a point is not currently being moved
        if (!isPointSelected || (moveAction != null && !moveAction.action.IsPressed()))
        {
            HandleHover();
        }

        if (pokeAction != null && pokeAction.action.WasPressedThisFrame())
        {
            //Debug.Log("Trying to select point");
            TrySelectPoint();

        }

        if (isPointSelected)
        {
            // Read the joystick input (Vector2)
            Vector2 joystickInput = joystickAction.action.ReadValue<Vector2>();

            // Use the X-axis for increasing/decreasing width
            if (joystickInput.x > 0.5f && joystickAction.action.WasPressedThisFrame())
            {
                //Debug.Log("Joystick moved right: Increasing width");
                IncreaseSegmentWidth();
            }
            else if (joystickInput.x < -0.5f && joystickAction.action.WasPressedThisFrame())
            {
                //Debug.Log("Joystick moved left: Decreasing width");
                DecreaseSegmentWidth();

            }
        }
        //else if (isPointSelected && joystickAction != null && joystickAction.action.WasReleasedThisFrame())
        //{
        //    DeselectPoint();
        //    //UpdateEditedPoints();
        //}
        //else if (isPointSelected && joystickAction != null)
        //{
        //    Vector2 joystickInput = joystickAction.action.ReadValue<Vector2>();

        //    // Check if joystick has returned to neutral (approximately zero)
        //    if (joystickInput.magnitude < 0.1f) // Adjust threshold as needed
        //    {
        //        DeselectPoint();
        //        Debug.Log("Joystick returned to neutral: Point deselected.");


        //    }
        //}

        if (isPointSelected && pokeAction.action.WasReleasedThisFrame())
        {
            DeselectPoint();
            //Debug.Log("Joystick returned to neutral: Point deselected.");
        }
    }


    private void HandleHover()
    {
        //GameObject closestPoint = FindClosestPoint();
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

    private int startIndex = -1; // Start index of the fixed range
    private int endIndex = -1;   // End index of the fixed range
    private void TrySelectPoint()
    {
        GameObject closestPoint = FindClosestPoint();
        //GameObject closestLine = FindClosestPoint();
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

                        /////----------
                        // Calculate and store the fixed symmetrical range
                        startIndex = Mathf.Max(selectedPointIndex - 2, 0);
                        endIndex = Mathf.Min(selectedPointIndex + 2, selectedLine.positionCount - 1);

                        /////----------
                        break;
                    }
                }

                isPointSelected = true;
                initialOffset = selectedPoint.transform.position - rightControllerTransform.position;
                //Debug.Log("Point selected for moving!");
            }
        }
    }



    [Header("Line Width Settings")]
    public float defaultWidth = 0.01f; // Default width of the line
    public float widthIncrement = 0.001f; // Increment for increasing/decreasing segment width
    private float currentSegmentWidth; // Track the current segment width

    private int closestSegmentIndex = -1; // Track the closest segment index

    public float segmentWidthIncrement = 0.001f; // Amount to increase or decrease the width of the segment

    private void IncreaseSegmentWidth()
    {
        if (selectedLine == null || closestSegmentIndex == -1)
        {
            Debug.LogWarning("Cannot increase width: selectedLine or closestSegmentIndex is invalid.");
            return;
        }

        ModifySegmentWidth(closestSegmentIndex, widthIncrement);
    }

    private void DecreaseSegmentWidth()
    {
        if (selectedLine == null || closestSegmentIndex == -1)
        {
            Debug.LogWarning("Cannot decrease width: selectedLine or closestSegmentIndex is invalid.");
            return;
        }

        // Call ModifySegmentWidth with a negative width increment to decrease the width
        ModifySegmentWidth(closestSegmentIndex, -widthIncrement);
    }


    //private void ModifySegmentWidth(int segmentIndex, float widthIncrement)
    //{
    //    if (selectedLine == null || segmentIndex < 0 || segmentIndex >= selectedLine.positionCount - 1) return;

    //    // Get the current width curve or initialize a new one if it's null
    //    AnimationCurve widthCurve = selectedLine.widthCurve ?? new AnimationCurve();

    //    // Normalize time values based on the position count
    //    float normalizedStart = (segmentIndex) / (float)(selectedLine.positionCount - 1); //CHANGE could be segmentIndex-1
    //    float normalizedEnd = (segmentIndex) / (float)(selectedLine.positionCount - 1); //CHANGE it was segmentIndex+1

    //    // Get the current width values at the segment's start and end
    //    float currentStartWidth = widthCurve.Evaluate(normalizedStart);
    //    float currentEndWidth = widthCurve.Evaluate(normalizedEnd);

    //    // Add the width increment to the current widths
    //    float newStartWidth = Mathf.Max(0, currentStartWidth + widthIncrement); // Ensure non-negative width
    //    float newEndWidth = Mathf.Max(0, currentEndWidth + widthIncrement);

    //    // Update the curve with new widths
    //    AddOrUpdateKey(widthCurve, normalizedStart, newStartWidth);
    //    AddOrUpdateKey(widthCurve, normalizedEnd, newEndWidth);

    //    // Apply the updated curve to the LineRenderer
    //    selectedLine.widthCurve = widthCurve;


    //    //Debug.Log($"Segment {segmentIndex}: StartWidth = {newStartWidth}, EndWidth = {newEndWidth}");
    //}

    //private void ModifySegmentWidth(int segmentIndex, float widthIncrement)
    //{
    //    if (selectedLine == null || startIndex == -1 || endIndex == -1) return;

    //    // Get or initialize the width curve
    //    AnimationCurve widthCurve = selectedLine.widthCurve ?? new AnimationCurve();

    //    // Normalize time values
    //    int rangeSize = endIndex - startIndex + 1;
    //    for (int i = startIndex; i <= endIndex; i++)
    //    {
    //        // Calculate the weight for influence (higher for points near selectedPointIndex)
    //        float weight = 1.0f - Mathf.Abs(selectedPointIndex - i) / (float)rangeSize;

    //        // Normalize time values for the AnimationCurve
    //        float normalizedTime = i / (float)(selectedLine.positionCount - 1);

    //        // Get the current width and apply the weighted increment
    //        float currentWidth = widthCurve.Evaluate(normalizedTime);
    //        float newWidth = Mathf.Max(0, currentWidth + widthIncrement * weight);

    //        // Update the curve
    //        AddOrUpdateKey(widthCurve, normalizedTime, newWidth);
    //    }

    //    // Apply the updated curve to the LineRenderer
    //    selectedLine.widthCurve = widthCurve;
    //}

    private void ModifySegmentWidth(int segmentIndex, float widthIncrement)
    {
        if (selectedLine == null || startIndex == -1 || endIndex == -1) return;

        // Get or initialize the width curve
        AnimationCurve widthCurve = selectedLine.widthCurve ?? new AnimationCurve();

        // Calculate weights for normalization
        float[] weights = new float[endIndex - startIndex + 1];
        float totalWeight = 0.0f;

        for (int i = startIndex; i <= endIndex; i++)
        {
            int relativeIndex = i - startIndex;
            if (i == selectedPointIndex)
            {
                weights[relativeIndex] = 1.0f; // Full influence for the selected point
            }
            else if (Mathf.Abs(i - selectedPointIndex) == 1)
            {
                weights[relativeIndex] = 0.5f; // Half influence for adjacent points
            }
            else if (Mathf.Abs(i - selectedPointIndex) == 2)
            {
                weights[relativeIndex] = 0.25f; // Quarter influence for second-level neighbors
            }
            else
            {
                weights[relativeIndex] = 0.0f; // No influence outside the range
            }
            totalWeight += weights[relativeIndex];
        }

        // Normalize weights
        if (totalWeight > 0)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] /= totalWeight;
            }
        }

        // Apply normalized influence
        for (int i = startIndex; i <= endIndex; i++)
        {
            int relativeIndex = i - startIndex;

            // Normalize time values for the AnimationCurve
            float normalizedTime = i / (float)(selectedLine.positionCount - 1);

            // Get the current width and apply the weighted increment
            float currentWidth = widthCurve.Evaluate(normalizedTime);
            float newWidth = Mathf.Max(0, currentWidth + widthIncrement * weights[relativeIndex]);

            // Update the curve
            AddOrUpdateKey(widthCurve, normalizedTime, newWidth);
        }

        // Apply the updated curve to the LineRenderer
        selectedLine.widthCurve = widthCurve;
    }

    // Helper method to add or update a key in the AnimationCurve
    private void AddOrUpdateKey(AnimationCurve curve, float time, float value)
    {
        // Check if a key exists at the given time
        for (int i = 0; i < curve.length; i++)
        {
            if (Mathf.Approximately(curve.keys[i].time, time))
            {
                curve.RemoveKey(i); // Remove the old key
                break;
            }
        }

        // Add the new key
        curve.AddKey(new Keyframe(time, value));
    }


    // Helper method to detect the closest segment and update the `closestSegmentIndex`
    //private void UpdateClosestSegment()
    //{
    //    if (selectedLine == null || pointerTransform == null) return;

    //    Vector3[] positions = new Vector3[selectedLine.positionCount];
    //    selectedLine.GetPositions(positions);

    //    float closestDistance = float.MaxValue;
    //    closestSegmentIndex = -1;

    //    for (int i = 0; i < positions.Length - 1; i++)
    //    {
    //        float distance = DistanceToSegment(pointerTransform.position, positions[i], positions[i + 1]);
    //        if (distance < closestDistance)
    //        {
    //            closestDistance = distance;
    //            closestSegmentIndex = i;
    //        }
    //    }
    //}
    private void UpdateClosestSegment()
    {
        if (selectedLine == null || pointerTransform == null || startIndex == -1 || endIndex == -1) return;

        // Closest segment logic is no longer needed; keep the segment fixed
        closestSegmentIndex = startIndex; // Always use the fixed range
    }




    // Helper method to calculate the shortest distance from a point to a line segment
    private float DistanceToSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLengthSquared = lineDirection.sqrMagnitude;

        if (lineLengthSquared == 0f) return Vector3.Distance(point, lineStart); // The lineStart and lineEnd are the same point

        float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, lineDirection) / lineLengthSquared);
        Vector3 projection = lineStart + t * lineDirection;

        return Vector3.Distance(point, projection);
    }

    

    //private void UpdateRangeSphere()
    //{
    //    if (rangeSphereInstance == null) return;
    //    if (selectedLine != null && closestSegmentIndex != -1)
    //    {
    //        // Get the positions for the fixed range
    //        Vector3[] positions = new Vector3[selectedLine.positionCount];
    //        selectedLine.GetPositions(positions);

    //        Vector3 segmentStart = positions[startIndex];
    //        Vector3 segmentEnd = positions[endIndex];

    //        // Position the sphere at the midpoint of the full segment
    //        Vector3 segmentMidpoint = (segmentStart + segmentEnd) / 2f;
    //        rangeSphereInstance.transform.position = segmentMidpoint;

    //        // Align the sphere with the segment
    //        rangeSphereInstance.transform.rotation = Quaternion.LookRotation(segmentEnd - segmentStart);

    //        // Scale the sphere to match the full segment length
    //        float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
    //        rangeSphereInstance.transform.localScale = new Vector3(segmentLength, selectionRadius * 2, selectionRadius * 2);

    //        // Show the sphere
    //        rangeSphereInstance.SetActive(true);
    //    }
    //    else
    //    {
    //        // Hide the sphere if no segment is selected
    //        rangeSphereInstance.SetActive(false);
    //    }
    //}

    private void UpdateRangeSphere()
    {
        if (rangeSphereInstance == null || selectedLine == null || startIndex == -1 || endIndex == -1 || !isPointSelected)
        {
            // Hide the visualization if no point is selected
            rangeSphereInstance.SetActive(false);
            return;
        }

        // Get the positions for the fixed range
        Vector3[] positions = new Vector3[selectedLine.positionCount];
        selectedLine.GetPositions(positions);

        // Get the start and end of the range
        Vector3 segmentStart = positions[startIndex];
        Vector3 segmentEnd = positions[endIndex];

        // Position the sphere at the midpoint of the full segment
        Vector3 segmentMidpoint = (segmentStart + segmentEnd) / 2f;
        rangeSphereInstance.transform.position = segmentMidpoint;

        // Align the sphere with the segment
        rangeSphereInstance.transform.rotation = Quaternion.LookRotation(segmentEnd - segmentStart);

        // Scale the sphere to match the full segment length
        float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
        rangeSphereInstance.transform.localScale = new Vector3(segmentLength, selectionRadius * 2, selectionRadius * 2);

        // Show the visualization only when a point is selected
        rangeSphereInstance.SetActive(true);
    }


    //------------------------------------




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

    private void OnDrawGizmos()
    {
        if (pointerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pointerTransform.position, selectionRadius);
        }
    }


}