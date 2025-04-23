using UnityEngine;
using UnityEngine.UI;

public class LoadMesh : MonoBehaviour
{
    public Button loadButton; // Reference to your Button
    public OBJSpawner objSpawner; // Reference to your OBJSpawner script

    void Start()
    {
        if (loadButton != null && objSpawner != null)
        {
            // Add the listener for the button click
            loadButton.onClick.AddListener(objSpawner.SpawnObject);
        }
        else
        {
            // Error handling if references are not set
            Debug.LogError("Button or OBJSpawner not assigned in the Inspector.");
        }
    }
}
