using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

public class SkeletonizePointCloud : MonoBehaviour
{
#if UNITY_STANDALONE_OSX
    [DllImport("libSkeletonizePointCloud")] // MacOS
    private static extern int ProcessPointCloud(string inputFile, string outputFile);
#elif UNITY_ANDROID
    [DllImport("SkeletonizePointCloud")] // Quest 3 (Android)
    private static extern int ProcessPointCloud(string inputFile, string outputFile);
#endif

    public void ProcessCloud(string inputFilePath, string outputFilePath)
    {
        
#if UNITY_STANDALONE_OSX 
        
        if (File.Exists(inputFilePath))
        {
            //Debug.Log("Input File Path: " + inputFilePath);
            //Debug.Log("Output File Path: " + outputFilePath);
            //Debug.Log("Skeletonize - Processing point cloud. Input: " + inputFilePath + ", Output: " + outputFilePath);
            ProcessCloudInternal(inputFilePath, outputFilePath);
        }
        else
        {
            //Debug.LogError("Skeletonize - Input file not found at " + inputFilePath);
        }
#elif UNITY_ANDROID
        
        StartCoroutine(LoadFileFromAssetsForProcessing(inputFilePath, outputFilePath));
#endif
    }

#if UNITY_ANDROID
    
    private IEnumerator LoadFileFromAssetsForProcessing(string inputFilePath, string outputFilePath)
    {
        string uri = inputFilePath;
        if (!uri.Contains("://"))
        {
            uri = "jar:file://" + uri;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(uri))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("SPC- Error loading file from StreamingAssets: " + request.error);
            }
            else
            {
                //Debug.Log("SPC- File successfully loaded from StreamingAssets.");
                string savePath = Path.Combine(Application.persistentDataPath, Path.GetFileName(inputFilePath));
                File.WriteAllBytes(savePath, request.downloadHandler.data);
                ProcessCloudInternal(savePath, outputFilePath);
            }
        }
    }
#endif


    private void ProcessCloudInternal(string inputFilePath, string outputFilePath)
    {
        ////Debug.Log("Processing point cloud. Input: " + inputFilePath + ", Output: " + outputFilePath);
        int result = ProcessPointCloud(inputFilePath, outputFilePath);

        if (result == 0)
        {
            //Debug.Log("Point cloud processed successfully. ");
        }
        else
        {
            Debug.LogError("Failed to process point cloud.");
        }
    }
}