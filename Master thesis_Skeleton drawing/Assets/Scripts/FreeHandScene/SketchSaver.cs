using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Collections.Generic;

public class SketchSaver : MonoBehaviour
{
    public Button loadButton;
    private string saveFolderPath;


    private void Start()
    {
#if UNITY_ANDROID
        saveFolderPath = Path.Combine(Application.persistentDataPath, "SavedFiles");
#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        saveFolderPath = Path.Combine(Application.streamingAssetsPath, "SavedFiles");
#endif

        // Ensure the save folder exists
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
            Debug.Log($"Save folder created at {saveFolderPath}");
        }
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(OnSaveButtonClick);
        }
        else
        {
            Debug.LogError("Button not assigned in the Inspector!");
        }
    }

    public void OnSaveButtonClick()
    {
        Debug.Log($"Saving Free-Hand Sketch");
        SaveOBJFile(saveFolderPath);

    }


    //private string objFileName = "file.obj";
    //public string outputFileName = "ProcessedData.graph";
    //private string objFilePath;
    //private string outputFilePath;

    public void SaveOBJFile(string saveDirectory)
    {
        // Check if the base save directory exists; create if it doesn't
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        //objFilePath = Path.Combine(Application.persistentDataPath, objFileName); // Use persistentDataPath on Android
        //outputFilePath = Path.Combine(Application.persistentDataPath, outputFileName);

        // Create a unique subfolder
        string folderName = $"NewFreeHandSketch_{DateTime.Now:yyyyMMdd_HHmmss}";
        string folderPath = Path.Combine(saveDirectory, folderName);

        try
        {
            // Create the subfolder
            Directory.CreateDirectory(folderPath);
            //Debug.Log($"Folder created at {folderPath}");

            // Define save paths for the files within the new folder
            string objSavePathSketch = Path.Combine(folderPath, "FreeHandSketch.obj");

            GenerateOBJFile(objSavePathSketch);
            //Debug.Log($"New OBJ file generated at: {objSavePathSketch}");

            string SketchMetricsSavePath = Path.Combine(folderPath, "FreeHandSketchMetrics.txt");
            SaveSketchMetrics(SketchMetricsSavePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving files: {ex.Message}");
        }


    }

    private void GenerateOBJFile(string objFilePath)
    {
        if (FreeHandDrawing.newContainer == null)
        {
            Debug.LogError("No newContainer found to generate OBJ file.");
            return;
        }

        LineRenderer[] lineRenderers = FreeHandDrawing.newContainer.GetComponentsInChildren<LineRenderer>();

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
        Debug.Log("Sketch file saved.");
    }


    private void SaveSketchMetrics(string objFilePath)
    {

        using (StreamWriter sketchWriter = new StreamWriter(objFilePath))
        {
            SketchTimer.StopTimerAndSave();

            sketchWriter.WriteLine("# Sketch Metrics file generated");
            sketchWriter.WriteLine($"Sketch Time: {SketchTimer.elapsedTime} seconds");
            sketchWriter.WriteLine($"   ");

            sketchWriter.WriteLine($"Sketched lines drawn from Free-Hand Sketching: {FreeHandDrawing.SketchedLinesNum}");
            sketchWriter.WriteLine($"Undo lines drawn from Free-Hand Sketching: {FreeHandDrawing.UndoSketchedLinesNum}");
        }
        Debug.Log("Sketch metrics file saved.");

    }
}