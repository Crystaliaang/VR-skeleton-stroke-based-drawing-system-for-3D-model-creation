using UnityEngine;
using System.IO;

public class SketchTimer : MonoBehaviour
{
    public static float startTime;
    public static float elapsedTime;
    public static bool isTiming = false;
    //private string saveFilePath;

    //private void Start()
    //{
    //    //saveFilePath = Path.Combine(Application.persistentDataPath, "SketchTime.txt");
    //}

    public static void StartTimer()
    {
        startTime = Time.time;
        isTiming = true;
        //Debug.Log("Timer started.");
    }

    public static void StopTimerAndSave()
    {
        if (isTiming)
        {
            elapsedTime = Time.time - startTime;
            //isTiming = false;
            //Debug.Log($"Timer stopped. Elapsed time: {elapsedTime} seconds");
        }
    }

}