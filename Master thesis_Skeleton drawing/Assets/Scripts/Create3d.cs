using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class Create3d : MonoBehaviour
{
    public SkeletonizePointCloud skeletonizePointCloud;
    public PointCloudPreprocessor pointCloudPreprocessor;
    public OBJSpawner objSpawner;
    public ClearMesh meshCleaner;
    public Design design;

    public string inputFileName = "DrawingData.pts";  
    public string outputFileName = "file.obj";  

    private string inputFilePath;
    private string outputFilePath;

    public static int GeneratedMeshNum = 0;

    private void Start()
    {
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        inputFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", inputFileName);
        outputFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", outputFileName);
#elif UNITY_ANDROID
        inputFilePath = Path.Combine(Application.persistentDataPath, inputFileName);  
        outputFilePath = Path.Combine(Application.persistentDataPath, outputFileName);  
#endif
    }

    public void OnButtonClick()
    {
        GeneratedMeshNum++;
        //Clear old mesh
        meshCleaner.ClearAllData(); 

        //Save data
        design.SaveAllDrawingData(); 
        //Debug.LogError("2: Save data in Create3D ");


        // Preprocess point cloud
        if (pointCloudPreprocessor != null)
        {
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
            
            pointCloudPreprocessor.PreprocessPoints();  // Preprocess points before skeletonization //SOS -----------------------
#elif UNITY_ANDROID
            
            if (File.Exists(inputFilePath)) 
            {
                pointCloudPreprocessor.PreprocessPoints();
            }
            else
            {
                Debug.LogError("Point cloud file not found at " + inputFilePath);
                return;
            }
#endif
        }
        else
        {
            Debug.LogError("PointCloudPreprocessor component not assigned.");
            return;
        }

       
        if (skeletonizePointCloud != null)
        {
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
           
            string processedFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", "ProcessedData.graph");

            
            if (File.Exists(processedFilePath))
            {
                string fileContents = File.ReadAllText(processedFilePath);
                //Debug.Log("Processed file contents:\n" + fileContents);
            }
            else
            {
                Debug.LogError("Processed file not found at: " + processedFilePath);
            }

            skeletonizePointCloud.ProcessCloud(processedFilePath, outputFilePath);
            Debug.Log("SkeletonizePointCloud called.");
#elif UNITY_ANDROID
            
            string processedFileUri = Path.Combine(Application.persistentDataPath, "ProcessedData.graph");

            if (File.Exists(processedFileUri))
            {
                //Debug.Log("Processed point cloud file found at " + processedFileUri);
                skeletonizePointCloud.ProcessCloud(processedFileUri, outputFilePath);
            }
            else
            {
                //Debug.LogError("Processed point cloud file not found at " + processedFileUri);
            }
#endif
        }
        else
        {
            Debug.LogError("SkeletonizePointCloud component not assigned.");
        }


        //View mesh
        //objSpawner.SpawnObject();

        // delay before spawning the object
        StartCoroutine(DelayedSpawnObject());

    }

    // Coroutine to delay the spawn
    private IEnumerator DelayedSpawnObject()
    {
        yield return new WaitForSeconds(0.5f); 
        Debug.Log("Spawning object after delay...");
        objSpawner.SpawnObject();
    }

#if UNITY_ANDROID
    private IEnumerator PreprocessAndLoadPointCloudForAndroid()
    {
        string uri = Path.Combine(Application.persistentDataPath, inputFileName);  
        if (!uri.Contains("://"))
        {
            uri = "file://" + uri; 
        }

        using (UnityWebRequest request = UnityWebRequest.Get(uri))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading point cloud file: " + request.error);
            }
            else
            {
                Debug.Log("DrawingData.pts file loaded for preprocessing.");

                string savePath = Path.Combine(Application.persistentDataPath, "ProcessedData.graph");
                File.WriteAllBytes(savePath, request.downloadHandler.data);

                pointCloudPreprocessor.PreprocessPoints();  
            }
        }
    }
#endif
}