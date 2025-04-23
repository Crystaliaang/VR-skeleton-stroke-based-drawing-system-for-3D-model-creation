using UnityEngine;
using UnityEngine.UI;
using System.IO; 

public class ClearData : MonoBehaviour
{

    public Button loadButton; 

    private string inputFilePath;
    private string outputFilePath;
    private string inputconnectionsFilePath;
    public Design designScript;

    public static int ClearedSketchNum = 0;

    void Start()
    {
       
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        inputFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", "DrawingData.pts");
        outputFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", "ProcessedData.graph");
        inputconnectionsFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", "Connections.graph");

#elif UNITY_ANDROID
        inputFilePath = Path.Combine(Application.persistentDataPath, "DrawingData.pts");
        outputFilePath = Path.Combine(Application.persistentDataPath, "ProcessedData.graph");
        inputconnectionsFilePath = Path.Combine(Application.persistentDataPath, "Connections.graph");
#endif

       
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(ClearAllData);
        }
        else
        {
            Debug.LogError("Button not assigned in the Inspector!");
        }
    }

    void ClearAllData()
    {

        SketchTimer.StartTimer();
        ClearedSketchNum++;
        Design.SketchedLinesNum = 0;
        Design.UndoSketchedLinesNum = 0; 
        Design.MediumLinesNum = 0;
        Design.PreviewTimesNum = 0;
        LinePointPokeMover.MovedPointNum = 0;
        RadiiEditorGrip.RadiousChangedNum = 0;
        Create3d.GeneratedMeshNum = 0;
        EnableWireframe.WireframeToggledNum = 0;
        ClearMesh.ClearedMeshNum = 0;
        EnableMirror.MirrorEnabledNum = 0;


        GameObject[] objects = GameObject.FindGameObjectsWithTag("LineSegment");

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                //obj.SetActive(false); 
                DestroyImmediate(obj);
                Debug.Log($"Disabled: Lines Sketch");
            } 
        }

        GameObject[] previewObjects = GameObject.FindGameObjectsWithTag("PreviewContainer");

        foreach (GameObject obj in previewObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Design.isPreview = false;
                Debug.Log($"Disabled: Preview Sketch");
            }
        }
        designScript.CreateNewLineContainer();
        

    }
}