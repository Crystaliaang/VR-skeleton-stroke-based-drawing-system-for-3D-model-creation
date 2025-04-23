using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DebugLogToTMP : MonoBehaviour
{
    public TMP_Text debugText; 
    public ScrollRect scrollRect; 
    public RectTransform contentRectTransform; 

    private string logMessages = "";

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }


    void HandleLog(string logString, string stackTrace, LogType type)
    {
        logMessages += logString + "\n"; 
        debugText.text = logMessages; 

        // content to recalculates its size
        Canvas.ForceUpdateCanvases();

        ResizeContent();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // Resize the content 
    void ResizeContent()
    {
        float textHeight = debugText.preferredHeight;
        contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, textHeight);
    }
}
