using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;


public class RadiiEditorGrip : MonoBehaviour
{
    public Design designContainer;

    [Header("Controller Input")]
    [SerializeField]
    private InputActionReference gripAction;

    [SerializeField]
    [Header("Pointer and Controller References")]
    public Transform pointerTransform; 
    public Transform rightControllerTransform; 

    [Header("Materials")]
    public Material hoverMaterial;
    public Material defaultMaterial;
    public Material selectedMaterial;
    public Material rangeSphereMaterial; 

    [Header("Movement Settings")]
    public float selectionRadius = 0.03f; // Radius for selecting points
    public float smoothingSpeed = 5.0f;


    private GameObject rangeSphereInstance; 


    private GameObject hoveredPoint;
    private GameObject selectedPoint;
    private LineRenderer selectedLine;
    private int selectedPointIndex = -1;
    private bool isPointSelected = false;
    private Vector3 initialOffset;

    private bool isGripHeld = false;
    private Vector3 previousControllerPosition;

    public static int RadiousChangedNum = 0;


    public Dictionary<LineRenderer, List<Vector3>> updatedLines = new Dictionary<LineRenderer, List<Vector3>>();
    public Dictionary<LineRenderer, List<Vector3>> oldupdatedLines = new Dictionary<LineRenderer, List<Vector3>>();

    private void Start()
    {
        rangeSphereInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rangeSphereInstance.transform.localScale = Vector3.one * 0.02f; 
        rangeSphereInstance.GetComponent<Renderer>().material = rangeSphereMaterial;
        rangeSphereInstance.SetActive(false); 
    }


    private void Update()
    {

        UpdateClosestSegment();

        UpdateRangeSphere(); 

        if (!isPointSelected || (gripAction != null && !gripAction.action.IsPressed()))
        {
            HandleHover();
        }

        if (gripAction != null && gripAction.action.WasPressedThisFrame())
        {
            //Debug.Log("Trying to select point");
            TrySelectPoint();

        }


        if (gripAction == null || gripAction.action == null) return;

        float gripValue = gripAction.action.ReadValue<float>();

        // Detect if grip is held
        if (gripValue > 0.8f)
        {
            if (!isGripHeld) 
            {
                TrySelectPoint();
                if (isPointSelected)
                {
                    isGripHeld = true;
                    previousControllerPosition = rightControllerTransform.position;
                    HandleSelect(closestPoint);
                }
            }
            else if (isGripHeld && isPointSelected)
            {
                Vector3 currentControllerPosition = rightControllerTransform.position;
                float movementDelta = currentControllerPosition.z - previousControllerPosition.z;
                //Debug.Log("Movement "+ movementDelta);
                if (Mathf.Abs(movementDelta) > 0.0001f) // Threshold to avoid noise
                {
                    //Debug.Log("movementDelta = "+ movementDelta);
                    if (movementDelta > 0) // Pushing forward
                    {
                        //IncreaseSegmentWidth();
                        //Debug.Log("Increase ");

                        DecreaseSegmentWidth();
                        //Debug.Log("--Decrease ");
                    }
                    else if (movementDelta < 0) // Pulling backward
                    {
                        //DecreaseSegmentWidth();
                        //Debug.Log("--Decrease ");

                        IncreaseSegmentWidth();
                        //Debug.Log("Increase ");
                    }
                }
                if (Design.isPreview)
                {
                    designContainer.TogglePreview();
                    designContainer.TogglePreview();
                }

                previousControllerPosition = currentControllerPosition; // Update for the next frame
            }
        }
        else
        {
            //if (Design.isPreview)
            //{
            //    designContainer.TogglePreview();
            //    designContainer.TogglePreview();
            //}
            isGripHeld = false;
        }


        if (isPointSelected && gripAction.action.WasReleasedThisFrame())
        {
            RadiousChangedNum++;
            MarkWidthChanged(selectedLine);
            DeselectPoint();
            //Debug.Log("Joystick returned to neutral: Point deselected.");
            
        }
    }

    GameObject closestPoint;
    private void HandleHover()
    {
        closestPoint = FindClosestPoint();
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


    private void HandleSelect(GameObject selectedPoint)
    {
        GameObject closestPoint = FindClosestPoint();
        if (closestPoint != null && closestPoint != selectedPoint)
        {
            ResetSelectedPoint(selectedPoint); 
            selectedPoint = closestPoint; 
            ApplySelectEffect(selectedPoint); 
        }
        else if (closestPoint == null)
        {
            ResetSelectedPoint(selectedPoint);
        }
    }

    private void ApplySelectEffect(GameObject point)
    {
        if (point != null)
        {
            MeshRenderer renderer = point.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = selectedMaterial; 
            }
        }
    }

    private void ResetSelectedPoint(GameObject selectedPoint)
    {
        if (selectedPoint != null)
        {
            MeshRenderer renderer = selectedPoint.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = defaultMaterial; 
            }
            selectedPoint = null; 
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

    private int startIndex = -1; 
    private int endIndex = -1;   
    private void TrySelectPoint()
    {
        GameObject closestPoint = FindClosestPoint();
        if (closestPoint != null)
        {
            selectedPoint = closestPoint;
            selectedLine = selectedPoint.GetComponentInParent<LineRenderer>();

            if (selectedLine != null)
            {
               
                for (int i = 0; i < selectedLine.positionCount; i++)
                {
                    if (selectedLine.GetPosition(i) == selectedPoint.transform.position)
                    {
                        selectedPointIndex = i;
                        
                        startIndex = Mathf.Max(selectedPointIndex - 3, 0);
                        endIndex = Mathf.Min(selectedPointIndex + 3, selectedLine.positionCount - 1);
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
    public float defaultWidth = 0.01f; 
    public float widthIncrement = 0.001f; 
    private float currentSegmentWidth; 

    private int closestSegmentIndex = -1; 
    public float segmentWidthIncrement = 0.001f; 

    private void IncreaseSegmentWidth()
    {
        if (selectedLine == null || closestSegmentIndex == -1)
        {
            //Debug.LogWarning("Cannot increase width: selectedLine or closestSegmentIndex is invalid.");
            return;
        }

        ModifySegmentWidth(closestSegmentIndex, widthIncrement);
        //Debug.Log($"Width successfully increased by {widthIncrement} for segment index {closestSegmentIndex}");
    }


    private void DecreaseSegmentWidth()
    {
        if (selectedLine == null || closestSegmentIndex == -1)
        {
            //Debug.LogWarning("Cannot decrease width: selectedLine or closestSegmentIndex is invalid.");
            return;
        }

        ModifySegmentWidth(closestSegmentIndex, -widthIncrement);
        //Debug.Log($"Width successfully decreased by {widthIncrement} for segment index {closestSegmentIndex}");
    }



    //NEW
    private void ModifySegmentWidth(int segmentIndex, float widthIncrement)
    {
        if (selectedLine == null || startIndex == -1 || endIndex == -1) return;

        // Get or initialize the width curve
        AnimationCurve widthCurve = selectedLine.widthCurve ?? new AnimationCurve();

        // Calculate weights for normalization
        float[] weights = new float[endIndex - startIndex + 1];
        float totalWeight = 0.0f;
        //Debug.Log($"Selected: {selectedPointIndex}");
        for (int i = startIndex; i <= endIndex; i++)
        {
            int relativeIndex = i - startIndex;
            if (i == selectedPointIndex)
            {
                weights[relativeIndex] = 0.9f; 
            }
            else if (Mathf.Abs(i - selectedPointIndex) == 1)
            {
                weights[relativeIndex] = 0.6f; 
            }
            else if (Mathf.Abs(i - selectedPointIndex) == 2)
            {
                weights[relativeIndex] = 0.3f; 
            }
            else if (Mathf.Abs(i - selectedPointIndex) == 3)
            {
                weights[relativeIndex] = 0.0f; 
            }
            else
            {
                weights[relativeIndex] = 0.0f; 
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

            float currentWidth = widthCurve.Evaluate(normalizedTime);
            float newWidth = Mathf.Max(0, currentWidth + widthIncrement * weights[relativeIndex]);

            // Update the curve
            AddOrUpdateKey(widthCurve, normalizedTime, newWidth);
        }

     
        selectedLine.widthCurve = widthCurve;
    }


    private void AddOrUpdateKey(AnimationCurve curve, float time, float value)
    {
        for (int i = 0; i < curve.length; i++)
        {
            if (Mathf.Approximately(curve.keys[i].time, time))
            {
                curve.RemoveKey(i); 
                break;
            }
        }
        curve.AddKey(new Keyframe(time, value));
    }



    private void UpdateClosestSegment()
    {
        if (selectedLine == null || pointerTransform == null || startIndex == -1 || endIndex == -1) return;

        closestSegmentIndex = selectedPointIndex;
    }



    private void UpdateRangeSphere()
    {
        //Debug.Log($"Selected range: {selectedPointIndex}");
        if (rangeSphereInstance == null || selectedLine == null || startIndex == -1 || endIndex == -1 || !isPointSelected)
        {
            rangeSphereInstance.SetActive(false);
            return;
        }

        Vector3[] positions = new Vector3[selectedLine.positionCount];
        selectedLine.GetPositions(positions);

      
        if (selectedPointIndex < 0 || selectedPointIndex >= positions.Length)
        {
            rangeSphereInstance.SetActive(false);
            return;
        }


        Vector3 segmentMidpoint = positions[selectedPointIndex]; 

       
        if (startIndex >= 0 && startIndex < positions.Length && endIndex >= 0 && endIndex < positions.Length)
        {
            Vector3 segmentStart = positions[startIndex];
            Vector3 segmentEnd = positions[endIndex];

            
            if (segmentStart != segmentEnd)
            {
                rangeSphereInstance.transform.rotation = Quaternion.LookRotation(segmentEnd - segmentStart);
            }
        }

        rangeSphereInstance.transform.position = segmentMidpoint;

        float sphereSize = selectionRadius * 2;
        rangeSphereInstance.transform.localScale = new Vector3(sphereSize, sphereSize, sphereSize);

        rangeSphereInstance.SetActive(true);
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


    public Dictionary<LineRenderer, bool> changedWidthLines = new Dictionary<LineRenderer, bool>();

    public void MarkWidthChanged(LineRenderer line)
    {
        if (line != null && !changedWidthLines.ContainsKey(line))
        {
            changedWidthLines[line] = true;
            //Debug.Log($"Line {line.gameObject.name} width marked as changed.");
        }
    }

    public void ResetWidthToDefault(LineRenderer line)
    {
        if (line == null) return;

        AnimationCurve defaultWidthCurve = new AnimationCurve();
        defaultWidthCurve.AddKey(0f, defaultWidth); 
        defaultWidthCurve.AddKey(1f, defaultWidth); 

        line.widthCurve = defaultWidthCurve;
        //Debug.Log($"Line {line.gameObject.name} width reset to default.");
    }

    public bool TryResetAndRemoveLastChangedWidth()
    {
        if (changedWidthLines.Count == 0) return false;

        var lastChangedLine = changedWidthLines.Keys.Last();
        ResetWidthToDefault(lastChangedLine);

        changedWidthLines.Remove(lastChangedLine);
        //Debug.Log($"Line {lastChangedLine.gameObject.name} removed from changed width dictionary.");

        return true;
    }


}