using UnityEngine;
using UnityEngine.UI;
using System.IO; 

public class EnableWireframe : MonoBehaviour
{
    public Button loadButton; 
    public OBJSpawner spawner;
    public static int WireframeToggledNum = 0;

    void Start()
    {

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(enable);
        }
        else
        {
            Debug.LogError("Button not assigned in the Inspector!");
        }
    }

    void enable()
    {
        WireframeToggledNum++;
        spawner.ToggleWireframe();
        Debug.Log("Wireframe enabled/disabled.");
    }
}
