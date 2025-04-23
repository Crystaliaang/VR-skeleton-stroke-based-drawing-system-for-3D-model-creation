using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class OBJFileSaver : MonoBehaviour
{
    public Button loadButton;
    public OBJSpawner objSpawner; 
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
        Debug.Log($"Saving OBJ");
        if (objSpawner != null)
        {
            objSpawner.SaveOBJFile(saveFolderPath);
        }
        else
        {
            Debug.LogError("OBJSpawner reference is not set.");
        }
    }
}