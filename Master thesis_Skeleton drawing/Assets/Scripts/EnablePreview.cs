using UnityEngine;
using UnityEngine.UI;
using System.IO; 

public class EnablePreview : MonoBehaviour
{
    public Button loadButton;
    public Design designContainer;
    

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
        
        designContainer.TogglePreview();
        //Debug.Log("Preview enabled/disabled.");
    }
}
