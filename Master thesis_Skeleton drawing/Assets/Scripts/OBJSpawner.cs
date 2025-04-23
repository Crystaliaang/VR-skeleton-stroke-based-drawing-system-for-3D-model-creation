using System.Collections;
using System.IO;
using UnityEngine;
using Dummiesman; // Import the OBJ loader
using UnityEngine.Networking;
using System;
using System.Collections.Generic;

public class OBJSpawner : MonoBehaviour
{
    private string objFileName = "file.obj";
    private string objFilePath;
    public Material objectMaterial; 
    //public float spawnDistance = 3f; 
    //public float scaleFactor = 1f; 

    public GameObject loadedObject;
    public Mesh loadedMesh;

    private bool isWireframe = false;
    public Material wireframeMaterial; 
    public Material VRWireframeMaterial;

    public Design designScript;

    public string outputFileName = "ProcessedData.graph";  
    private string outputFilePath;

    private void Start()
    {
        // Set the file path based on platform
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        objFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", objFileName);
        outputFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", outputFileName);
#elif UNITY_ANDROID
        objFilePath = Path.Combine(Application.persistentDataPath, objFileName); // Use persistentDataPath on Android
        outputFilePath = Path.Combine(Application.persistentDataPath, outputFileName);
#endif
    }

    public void SpawnObject()
    {
        // Platform-specific file loading
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        // On macOS or other desktop platforms, load the file directly from StreamingAssets
        if (File.Exists(objFilePath))
        {
            LoadAndCreatePrefab();
        }
        else
        {
            Debug.LogError($"OBJ file not found at {objFilePath}");
        }
#elif UNITY_ANDROID
        // On Android, load the OBJ file directly from persistentDataPath
        if (File.Exists(objFilePath))
        {
            LoadAndCreatePrefab();
        }
        else
        {
            Debug.LogError($"1. OBJ file not found at {objFilePath}");
        }
#endif
    }


    private void LoadAndCreatePrefab()
    {
        loadedMesh = LoadOBJMesh(objFilePath);

        if (loadedMesh != null)
        {
            loadedObject = new GameObject("SpawnedObject");
            MeshFilter mf = loadedObject.AddComponent<MeshFilter>();
            mf.mesh = loadedMesh;

            Vector3 oldCenterWorldPosition = mf.transform.TransformPoint(mf.mesh.bounds.center);
            //Debug.Log($"Old Center Position Before Pivot Adjustment: {oldCenterWorldPosition}");

            // Center the pivot of the mesh
            //Vector3 originalSpawnPosition = loadedObject.transform.position;
            CenterPivot(mf, oldCenterWorldPosition);

            MeshRenderer mr = loadedObject.AddComponent<MeshRenderer>();

            //new
            loadedObject.AddComponent<RemoveLongEdges>();

            // Fallback in case material or shader is missing
            if (objectMaterial == null || objectMaterial.shader == null)
            {
                //Debug.LogWarning("Applying fallback material.");

#if UNITY_ANDROID
                objectMaterial = new Material(Shader.Find("Mobile/Diffuse"));
#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
                objectMaterial = new Material(Shader.Find("Standard"));
#endif
            }
            
            mr.material = objectMaterial; 
            BoxCollider collider = loadedObject.AddComponent<BoxCollider>();

          
            Rigidbody rb = loadedObject.AddComponent<Rigidbody>();
            rb.useGravity = false; // Disable gravity
            rb.isKinematic = true; // Enable physics interaction
            rb.mass = 10f;          // Set mass to 10
            rb.drag = 10f;          // Set drag
            rb.constraints = RigidbodyConstraints.FreezeRotation; // Freeze rotation


            // Add XRGrabInteractable components for grabbing functionality
            loadedObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            loadedObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Transformers.XRGeneralGrabTransformer>();

            // Add the wireframe component
            MeshWireframeComputor wireframeComputor = loadedObject.AddComponent<MeshWireframeComputor>();
            wireframeComputor.UpdateMesh(); 

            // Apply inverse scaling to bring the object back to its original size
            //float inverseScaleFactor = 0.01f; // Inverse of x100 scaling
            //loadedObject.transform.localScale = new Vector3(inverseScaleFactor, inverseScaleFactor, inverseScaleFactor);
            // Position the object at the default position (0, 0, 0) or adjust if needed
            //PositionAtDefault(loadedObject);

            loadedObject.tag = "Spawned";
            SaveAsPrefab(loadedObject);

            GameObject[] previewObjects = GameObject.FindGameObjectsWithTag("PreviewContainer");

            foreach (GameObject obj in previewObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false); // Disable the object
                    Design.isPreview = false;
                    Debug.Log($"Disabled: Preview Sketch");
                }
            }

            //Debug.Log("Object spawned successfully with inverse scaling.");

        }
        else
        {
            Debug.LogError("Failed to load the OBJ mesh.");
        }
    }


    private void SaveAsPrefab(GameObject obj)
    {
#if UNITY_EDITOR
        // Ensure the Prefabs folder exists
        string folderPath = "Assets/Prefabs";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Save the prefab in the correct location
        string prefabPath = Path.Combine(folderPath, "SpawnedObject.prefab");
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
        Debug.Log($"Prefab saved at {prefabPath}");
#endif
    }

    private Mesh LoadOBJMesh(string filePath)
    {
        GameObject obj = null;

        if (!File.Exists(filePath))
        {
            //Debug.LogError("OBJ file not found at: " + filePath);
            return null;
        }

        // Dummiesman OBJLoader to load the OBJ file into a GameObject
        try
        {
            obj = new Dummiesman.OBJLoader().Load(filePath);
            Debug.Log("OBJ file loaded successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading OBJ file: " + ex.Message);
            return null;
        }

        if (obj != null)
        {
            // Log the loaded object's structure
            //Debug.Log($"Loaded OBJ GameObject: {obj.name}");
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

            // If no MeshFilter is directly attached, try to find it in child objects
            if (meshFilter == null)
            {
                //Debug.Log("MeshFilter not found directly on the object, checking children...");
                meshFilter = obj.GetComponentInChildren<MeshFilter>();
            }
            else
            {
                //Debug.Log("MeshFilter found directly on the object.");
            }

            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.mesh;

                if (mesh != null)
                {
                    //Debug.Log("Mesh loaded successfully.");
                    mesh.RecalculateNormals();
                    Destroy(obj); // Clean up the loaded object 
                    return mesh;
                }
                else
                {
                    Debug.LogError("MeshFilter found, but mesh is null.");
                }
            }

            Destroy(obj); // Clean up even if mesh is not found
        }
        else
        {
            Debug.LogError("Failed to load OBJ file into GameObject.");
        }

        return null;
    }

    // Method to center the pivot of the mesh
    void CenterPivot(MeshFilter meshFilter, Vector3 originalSpawnPosition)
    {
        Mesh mesh = meshFilter.mesh;
        Vector3 center = mesh.bounds.center; //  mesh's center
        Vector3[] vertices = mesh.vertices;

        //the center of the mesh becomes the pivot
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= center; // Move each vertex in the opposite direction of the center
        }
        
        mesh.vertices = vertices;// Apply the new vertices to the mesh

        // recalculate mesh bounds and normals
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        Vector3 originalPosition = meshFilter.transform.position; // Store the current object position
        meshFilter.transform.position = originalSpawnPosition; // Set the object back to its original position before pivot adjustment
        //Debug.Log($"Final Position After Resetting to Original Spawn Position: {meshFilter.transform.position}");
    }

    public void ToggleWireframe()
    {
        isWireframe = !isWireframe;

        if (loadedObject == null)
        {
            Debug.LogError("No loaded object to toggle wireframe on.");
            return;
        }

        MeshRenderer mr = loadedObject.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            if (isWireframe && wireframeMaterial != null)
            {
                mr.material = wireframeMaterial;
#if UNITY_STANDALONE_OSX
                mr.material = wireframeMaterial;

#elif UNITY_ANDROID
                mr.material = VRWireframeMaterial;

#endif
            }
            else
            {
                mr.material = objectMaterial;
            }
        }
        else
        {
            //Debug.LogError("No MeshRenderer found on the loaded object.");
        }
    }


    public void SaveOBJFile(string saveDirectory)
    {
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        objFilePath = Path.Combine(Application.persistentDataPath, objFileName); // Use persistentDataPath on Android
        outputFilePath = Path.Combine(Application.persistentDataPath, outputFileName);

        // Create a unique subfolder
        string folderName = $"NewObject_{DateTime.Now:yyyyMMdd_HHmmss}";
        string folderPath = Path.Combine(saveDirectory, folderName);

        try
        {
            // Create the subfolder
            Directory.CreateDirectory(folderPath);
            Debug.Log($"Folder created at {folderPath}");

            // Define save paths for the files within the new folder
            string objSavePath = Path.Combine(folderPath, "savedFile.obj");
            string graphSavePath = Path.Combine(folderPath, "savedFile.graph");
            string objSavePathSketch = Path.Combine(folderPath, "savedSketch.obj");

            // Save the OBJ file
            if (File.Exists(objFilePath))
            {
                File.Copy(objFilePath, objSavePath, overwrite: false); // No overwriting
                //Debug.Log($"OBJ file saved successfully at {objSavePath}");
                Debug.Log("Mesh file saved successfully}");
            }
            else
            {
                Debug.LogError($" Original OBJ file not found at {objFilePath}");
            }

            // Save the .graph file
            if (File.Exists(outputFilePath))
            {
                File.Copy(outputFilePath, graphSavePath, overwrite: false); // No overwriting
                //Debug.Log($".graph file saved successfully at {graphSavePath}");
                Debug.Log("Graph file saved successfully }");
            }
            else
            {
                Debug.LogWarning($"Output file not found at {outputFilePath}");
            }

            GenerateOBJFile(objSavePathSketch);
            //Debug.Log($"New OBJ file generated at: {objSavePathSketch}");


            string SketchMetricsSavePath = Path.Combine(folderPath, "ConstraintDesignMetrics.txt");
            SaveDesignMetrics(SketchMetricsSavePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving files: {ex.Message}");
        }
    }

    private void GenerateOBJFile(string objFilePath)
    {
        if (Design.newContainer == null)
        {
            Debug.LogError("No newContainer found to generate OBJ file.");
            return;
        }

        LineRenderer[] lineRenderers = Design.newContainer.GetComponentsInChildren<LineRenderer>();

        if (lineRenderers.Length == 0)
        {
            Debug.LogError("No LineRenderers found in newContainer. Cannot generate OBJ.");
            File.WriteAllText(objFilePath, "# Empty OBJ file generated");
            return;
        }

        using (StreamWriter objWriter = new StreamWriter(objFilePath))
        {
            objWriter.WriteLine("# OBJ file generated from Unity drawing");

            Dictionary<Vector3, int> nodeIndices = new Dictionary<Vector3, int>();
            int vertexCount = 1; // OBJ index starts from 1

            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                if (lineRenderer.positionCount == 0)
                {
                    Debug.LogWarning($"LineRenderer {lineRenderer.name} has no points.");
                    continue; // Skip empty lines
                }

                for (int i = 0; i < lineRenderer.positionCount; i++)
                {
                    Vector3 point = lineRenderer.GetPosition(i);

                    if (!nodeIndices.ContainsKey(point))
                    {
                        nodeIndices[point] = vertexCount++;
                        objWriter.WriteLine($"v {point.x} {point.y} {point.z}");
                    }
                }
            }

            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                for (int i = 1; i < lineRenderer.positionCount; i++)
                {
                    Vector3 p1 = lineRenderer.GetPosition(i - 1);
                    Vector3 p2 = lineRenderer.GetPosition(i);

                    if (nodeIndices.ContainsKey(p1) && nodeIndices.ContainsKey(p2))
                    {
                        objWriter.WriteLine($"l {nodeIndices[p1]} {nodeIndices[p2]}");
                    }
                    else
                    {
                        //Debug.LogWarning($"Skipping edge between points {p1} and {p2} (not found in dictionary).");
                    }
                }
            }
        }

        //Debug.Log($"Generated new OBJ file at: {objFilePath}");
        Debug.Log("Sketch file saved}");
    }


    private void SaveDesignMetrics(string objFilePath)
    {

        using (StreamWriter sketchWriter = new StreamWriter(objFilePath))
        {
            SketchTimer.StopTimerAndSave();

            sketchWriter.WriteLine("# Constraint Design Metrics file generated");
            sketchWriter.WriteLine($"Sketch Time: {SketchTimer.elapsedTime} seconds");
            sketchWriter.WriteLine($"   ");

            sketchWriter.WriteLine($"Sketched lines drawn: {Design.SketchedLinesNum}");
            sketchWriter.WriteLine($"Undo lines drawn: {Design.UndoSketchedLinesNum}");
            sketchWriter.WriteLine($"Medium lines drawn: {Design.MediumLinesNum}");
            sketchWriter.WriteLine($"Preview times button pressed: {Design.PreviewTimesNum}");
            sketchWriter.WriteLine($"Moved a point: {LinePointPokeMover.MovedPointNum}");
            sketchWriter.WriteLine($"Changed Radius: {RadiiEditorGrip.RadiousChangedNum}");
            sketchWriter.WriteLine($"Generated Mesh: {Create3d.GeneratedMeshNum}");
            sketchWriter.WriteLine($"Saw Wireframe: {EnableWireframe.WireframeToggledNum}");
            sketchWriter.WriteLine($"Cleared Sketch: {ClearData.ClearedSketchNum}");
            sketchWriter.WriteLine($"Cleared Mesh: {ClearMesh.ClearedMeshNum}");
            sketchWriter.WriteLine($"Mirroring used: {EnableMirror.MirrorEnabledNum}");
        }

        Debug.Log("Design metrics file saved.");

    }
}