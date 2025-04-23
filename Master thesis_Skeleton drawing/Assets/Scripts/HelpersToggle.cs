using UnityEngine;
using UnityEngine.InputSystem;

public class HelpersToggle : MonoBehaviour
{
    [Header("Controller Input")]
    [SerializeField]
    private InputActionReference ControllerTriggerAction;

    public bool HelpON = false;
    public GameObject calloutObject;  
    public GameObject calloutObjectLeft;


    private void OnEnable()
    {
        if (ControllerTriggerAction != null)
        {
            ControllerTriggerAction.action.performed += OnUndoButtonPressed;
            //Debug.Log("Helpers ");
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