using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;


public class FreeHandDrawing : MonoBehaviour
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

    //private KDTree kdTree; // KDTree field
    private List<Vector3> allPoints = new List<Vector3>(); 
    private bool NewTree = false;

    public MeshRenderer mesh;
    public static GameObject newContainer;
    public GameObject previewContainer;
    public static Material baseFallbackMaterial;

    //METRICS
    public static int SketchedLinesNum;
    public static int UndoSketchedLinesNum;


    private void Start()
    {
        CreateNewLineContainer();
        currentColorIndex = 0;

     
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
            //currentDrawing.gameObject.tag = "Spawned";  
            currentDrawing.transform.SetParent(newContainer.transform); 
            currentDrawing.material = drawingMaterial;
            currentDrawing.startColor = currentDrawing.endColor = penColors[currentColorIndex];
            currentDrawing.startWidth = currentDrawing.endWidth = penWidth;
            currentDrawing.positionCount = 1;
            currentDrawing.SetPosition(0, tip.position); 


        }
        else
        {
            var currentPos = tip.position;

            // add a new point if the distance is significant
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
                newLinePoints.Add(point); 
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
        SketchTimer.StartTimer();

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

         
            if (lineRenderers.Length > 0)
            {
                LineRenderer lastLine = lineRenderers[lineRenderers.Length - 1];
                DestroyImmediate(lastLine.gameObject);
                UndoSketchedLinesNum++;

            }
            else
            {
                Debug.LogWarning("Nothing to Undo.");
            }
        }
    }

}
