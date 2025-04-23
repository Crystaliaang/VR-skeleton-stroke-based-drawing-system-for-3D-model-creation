using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialToggle : MonoBehaviour
{
    [Header("Controller Input")]
    [SerializeField]
    private InputActionReference ControllerTriggerAction;

    public bool HelpON = false;
    private GameObject calloutObject;  
    private GameObject calloutObjectLeft;

    private void Awake()
    {
        calloutObject = GameObject.FindGameObjectWithTag("Callouts");
        calloutObjectLeft = GameObject.FindGameObjectWithTag("CalloutsLeft");
    }

    private void OnEnable()
    {
        if (ControllerTriggerAction != null)
        {
            ControllerTriggerAction.action.performed += OnUndoButtonPressed;
        }
    }

    private void OnDisable()
    {
        if (ControllerTriggerAction != null)
        {
            ControllerTriggerAction.action.performed -= OnUndoButtonPressed;
        }
    }

    private void OnUndoButtonPressed(InputAction.CallbackContext context)
    {
        ToggleHelp();
    }

    void ToggleHelp()
    {
        if (calloutObject != null)
        {
            HelpON = !HelpON;
            calloutObject.SetActive(HelpON);
            calloutObjectLeft.SetActive(HelpON);
            //Debug.Log("Helpers " + (HelpON ? "on" : "off"));
        }
    }
}