using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;


public class DesignDraw : MonoBehaviour
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
    private InputActionReference changeWidthButtonAction;  // New Input action for changing width


    [Header("Right Controller")]
    public Transform rightControllerTransform; // Reference to the right controller's transform
    public Transform penTransform; // Assign this in the Inspector

    private List<LineRenderer> drawnLines = new List<LineRenderer>(); // Initialize list for drawn lines
    private LineRenderer currentDrawing;
    private int index;
    private int currentColorIndex;
    private bool isDrawing;

    //private KDTree kdTree; // KDTree field
    private List<Vector3> allPoints = new List<Vector3>(); // Store all points for KD-Tree
    private bool NewTree = false;

    public MeshRenderer mesh;
    public GameObject newContainer;
    public GameObject previewContainer;
    public static Material baseFallbackMaterial;

    //Metrics - Statistics
    public static int SketchedLinesNum;
    public static int UndoSketchedLinesNum;


    private void Start()
    {
        CreateNewLineContainer();
        currentColorIndex = 0;

        // Make the pen a child of the right controller
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

         SketchedLinesNum = 0;
         UndoSketchedLinesNum = 0;

}

    private void Update()
    {


        bool isRightHandDrawing = IsButtonPressed(rightControllerTriggerAction);
        bool isLeftHandDrawing = IsButtonPressed(leftControllerTriggerAction);
        bool wasDrawing = isDrawing;
        isDrawing = isRightHandDrawing || isLeftHandDrawing;

        if (isDrawing)
        {
            //Debug.Log("Drawing----");
            Draw();

        }
        else if (!isDrawing && currentDrawing != null)
        {
            SketchedLinesNum++;

            EndDrawing();
            
            //Debug.Log("Stopped Drawing----");

        }
        //else if (IsButtonPressed(rightControllerPrimaryButtonAction))
        //{
        //    SwitchColor();
        //}
    }



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
            currentDrawing = new GameObject("DoodleLine").AddComponent<LineRenderer>();
            //currentDrawing.gameObject.tag = "Spawned";  // Set tag for the line
            currentDrawing.transform.SetParent(newContainer.transform); // Set parent 
            currentDrawing.material = drawingMaterial;
            currentDrawing.startColor = currentDrawing.endColor = penColors[currentColorIndex];
            currentDrawing.startWidth = currentDrawing.endWidth = penWidth;
            currentDrawing.positionCount = 1;
            currentDrawing.SetPosition(0, tip.position); // Start at the tip position


        }
        else
        {
            var currentPos = tip.position;

            // Only add a new point if the distance is significant
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

            // Collect the points of the newly drawn line
            for (int i = 0; i < currentDrawing.positionCount; i++)
            {
                Vector3 point = currentDrawing.GetPosition(i);
                newLinePoints.Add(point); // Collect points for intersection check
            }

            currentDrawing = null;

        }
        else
        {
            Debug.LogWarning("currentDrawing is null, cannot add to list.");
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


    private static InputAction GetInputAction(InputActionReference actionReference)
    {
        return actionReference != null ? actionReference.action : null;
    }


    int lineContainerCount = 0;
    int lineContainerCountLines = 0;
    public GameObject CreateNewLineContainer()
    {
        allPoints = new List<Vector3>();
        NewTree = true;


        lineContainerCount++;
        newContainer = new GameObject($"DrawingContainer_{lineContainerCount}");
        newContainer.gameObject.tag = "DrawingContainer";
        //Debug.Log($"CreateNewLineContainer: " + "LinesContainer");
        return newContainer;
    }

    public void UndoLastLine()
    {
        // Check if there's a container to undo from
        if (newContainer != null)
        {
            LineRenderer[] lineRenderers = newContainer.GetComponentsInChildren<LineRenderer>();

            // Check if there are any lines to undo
            if (lineRenderers.Length > 0)
            {
                // Get the last LineRenderer
                LineRenderer lastLine = lineRenderers[lineRenderers.Length - 1];
                DestroyImmediate(lastLine.gameObject);
              
                UndoSketchedLinesNum++;
                //Debug.Log($"UNDO LINE - NEW METRIC: {UndoSketchedLinesNum}.");

            }   
            else
            {
                Debug.LogWarning("Nothing to Undo.");
            }
        }
    }

}
