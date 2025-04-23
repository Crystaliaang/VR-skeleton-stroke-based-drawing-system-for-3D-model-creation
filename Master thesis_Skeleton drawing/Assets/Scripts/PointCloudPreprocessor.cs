using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

using System;
using System.Collections.Generic;
using System.Linq;


public class PointCloudPreprocessor : MonoBehaviour
{
    public string inputFileName = "DrawingData.pts";  
    public string inputFileConnections = "Connections.graph";  
    public string outputFileName = "ProcessedData.graph";  
    private string inputFilePath;
    private string outputFilePath;
    private string inputconnectionsFilePath;

    private void Start()
    {
        // Set file paths based on the platform
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        inputFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", inputFileName);
        outputFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", outputFileName); 
        inputconnectionsFilePath = Path.Combine(Application.streamingAssetsPath, "Drawing", inputFileConnections);
#elif UNITY_ANDROID
        inputFilePath = Path.Combine(Application.persistentDataPath, inputFileName);  
        outputFilePath = Path.Combine(Application.persistentDataPath, outputFileName);  
        inputconnectionsFilePath = Path.Combine(Application.persistentDataPath, inputFileConnections);
#endif
    }

    public void PreprocessPoints()
    {
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
        // For macOS or Windows, read the files directly from StreamingAssets
        if (File.Exists(inputFilePath))
        {
            string[] pointCloudLines = File.ReadAllLines(inputFilePath);
            ProcessPointCloud(pointCloudLines);
        }
        else
        {
            //Debug.LogError("PointCloud: Input file not found at " + inputFilePath);
        }

        if (File.Exists(inputconnectionsFilePath))
        {
            string[] connectionLines = File.ReadAllLines(inputconnectionsFilePath);
            //ProcessConnections(connectionLines);
            //Debug.Log("Mac: Preprocess done output file: " + outputFilePath);
        }
        else
        {
            //Debug.LogError("PointCloud: Connections file not found at " + inputconnectionsFilePath);
        }

#elif UNITY_ANDROID
        
        if (File.Exists(inputFilePath))
        {
            //Debug.Log("PointCloud: File found at " + inputFilePath);
            string[] pointCloudLines = File.ReadAllLines(inputFilePath);
            ProcessPointCloud(pointCloudLines);
        }
        else
        {
            //Debug.LogError("PointCloud: Input file not found at " + inputFilePath);
        }

        if (File.Exists(inputconnectionsFilePath))
        {
            //Debug.Log("PointCloud: Connections file found at " + inputconnectionsFilePath);
            string[] connectionLines = File.ReadAllLines(inputconnectionsFilePath);
            //ProcessConnections(connectionLines);
        }
        else
        {
            //Debug.LogError("PointCloud: Connections file not found at " + inputconnectionsFilePath);
        }
#endif
    }

    // Process the point cloud data 
    private void ProcessPointCloud(string[] lines)
    {
        if (lines != null && lines.Length > 0)
        {
            using (StreamWriter writer = new StreamWriter(outputFilePath)) 
            {
                foreach (string line in lines)
                {
         
                    string[] parts = line.Trim().Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

                    // Ensure there are exactly 3 parts for x, y, and z
                    //if (parts.Length >= 3)
                    //{
                    //    if (float.TryParse(parts[0], out float x) &&
                    //        float.TryParse(parts[1], out float y) &&
                    //        float.TryParse(parts[2], out float z))
                    //    {


                    //        // Scale the points 

                    //        //x = x * 100.0f;
                    //        //y = y * 100.0f;
                    //        //z = z * 100.0f;

                    //        // Write the modified points to the new file
                    //        //writer.WriteLine($"{x} {y} {z}");



                    //        // Write the node (with prefix 'n')
                    //        writer.WriteLine($"n {x} {y} {z}");

                    //    }
                    //}

                    if (parts.Length >= 4) 
                    {
                        if (float.TryParse(parts[0], out float x) &&
                            float.TryParse(parts[1], out float y) &&
                            float.TryParse(parts[2], out float z) &&
                            float.TryParse(parts[3], out float width))
                        {
                            writer.WriteLine($"n {x} {y} {z} {width}");
                        }
                    }

                    else
                    {
                        //Debug.LogWarning($"PointCloud: Invalid line format (not enough parts): {line}");
                    }
                }
            }
        }
        else
        {
            //Debug.LogError("PointCloud: No data to process in the input file.");
        }
    }



private void ProcessConnections(string[] connectionLines)
{
    if (connectionLines == null || connectionLines.Length == 0)
        return;

    HashSet<(int, int)> writtenEdges = new HashSet<(int, int)>();
    HashSet<int> visitedNodes = new HashSet<int>();

    using (StreamWriter writer = new StreamWriter(outputFilePath, append: true))
    {
        foreach (string connectionLine in connectionLines)
        {
            string[] parts = connectionLine.Split(' ');
            if (parts.Length > 3 || parts[0] != "c")
                continue;

            int node1 = int.Parse(parts[1]);
            int node2 = int.Parse(parts[2]);

            var edge = (Math.Min(node1, node2), Math.Max(node1, node2)); 

         
            if (Math.Abs(node1 - node2) <= 3 && visitedNodes.Contains(node1) && visitedNodes.Contains(node2))
            {
                continue; 
            }

           
            writer.WriteLine(connectionLine);
            writtenEdges.Add(edge);
            visitedNodes.Add(node1);
            visitedNodes.Add(node2);
        }
    }
}
}