using UnityEngine;
using UnityEngine.UI;
using System.IO; 

public class ClearMesh : MonoBehaviour
{
    public Button loadButton; 
    private string inputFilePath;
    private string outputFilePath;
    private string inputconnectionsFilePath;

    private string objFilePath;
    private string prefabFilePath;

    private OBJSpawner objSpawner; 
    public static int ClearedMeshNum = 0;

    public new string tag = "Spawned"; 

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
        
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        objFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", "file.obj");
        prefabFilePath = Path.Combine(Application.streamingAssetsPath, "Prefabs", "SpawnedObject.prefab"); 
#elif UNITY_ANDROID
        objFilePath = Path.Combine(Application.persistentDataPath, "file.obj"); 
        prefabFilePath = Path.Combine(Application.persistentDataPath, "SpawnedObject.prefab"); 
#endif

        // Disable all objects with the specified tag
        DisableAllObjectsWithTag(tag);

    
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(ClearAllData);
        }
        else
        {
            Debug.LogError("Button not assigned in the Inspector!");
        }
    }

    void DisableAllObjectsWithTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);

        if (objects.Length > 0)
        {
            //Debug.Log("Found spawned objects");
        }

     
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                //Debug.Log($"Disabled: {obj.name}");
            }
            else
            {
                Debug.LogWarning("Found a null object.");
            }
        }
    }

    public void ClearAllData()
    {
        ClearedMeshNum++;

        // Delete input data file if it exists
        if (File.Exists(inputFilePath))
        {
            File.Delete(inputFilePath);
            Debug.Log("Input data file deleted: " + inputFilePath);
        }
        else
        {
            Debug.Log("No input data file found at: " + inputFilePath);
        }

        // Delete output data file if it exists
        if (File.Exists(outputFilePath))
        {
            File.Delete(outputFilePath);
            Debug.Log("Output data file deleted: " + outputFilePath);
        }
        else
        {
            Debug.Log("No output data file found at: " + outputFilePath);
        }

        if (File.Exists(inputconnectionsFilePath))
        {
            File.Delete(inputconnectionsFilePath);
            Debug.Log("Output data file deleted: " + inputconnectionsFilePath);
        }
        else
        {
            Debug.Log("No output data file found at: " + inputconnectionsFilePath);
        }

        // Delete OBJ file if it exists
        if (File.Exists(objFilePath))
        {
            File.Delete(objFilePath);
            Debug.Log("OBJ file deleted: " + objFilePath);
        }
        else
        {
            Debug.Log("No OBJ file found at: " + objFilePath);
        }

#if UNITY_EDITOR
        // Delete prefab file if it exists (Editor Only)
        if (File.Exists(prefabFilePath))
        {
            File.Delete(prefabFilePath);
            Debug.Log("Prefab file deleted: " + prefabFilePath);
        }
        else
        {
            Debug.Log("No prefab file found at: " + prefabFilePath);
        }
#endif

        // Clear and disable the objects
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(false); // Disable the object
                Debug.Log($"Disabled: Spawned Objects");
            }
        }

        // Clear the mesh data from OBJSpawner
        if (objSpawner != null)
        {
            if (objSpawner.loadedObject != null)
            {
                MeshRenderer meshRenderer = objSpawner.loadedObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = false; 
                    Debug.Log("Loaded object hidden.");
                }
                else
                {
                    Debug.LogError("No MeshRenderer found on the loaded object.");
                }
            }
            else
            {
                Debug.Log("No loaded object to hide.");
            }

            // Clear any remaining references to the mesh
            if (objSpawner.loadedMesh != null)
            {
                objSpawner.loadedMesh.Clear(); 
                Debug.Log("Loaded mesh cleared.");
            }
            else
            {
                Debug.Log("No mesh to clear.");
            }
        }
        else
        {
            Debug.LogError("OBJSpawner not assigned.");
        }
    }
}