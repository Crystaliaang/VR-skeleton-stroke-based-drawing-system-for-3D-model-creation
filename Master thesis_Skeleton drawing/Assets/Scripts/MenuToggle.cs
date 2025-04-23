using UnityEngine;
using UnityEngine.InputSystem;

public class MenuToggle : MonoBehaviour
{
    [Header("Controller Input")]
    [SerializeField]
    private InputActionReference ControllerTriggerAction;

    public bool HelpON = false;
    public GameObject calloutObject;  

    private void Awake()
    {
        // Cache the GameObject reference at the start
        //calloutObject = GameObject.FindGameObjectWithTag("Menu");
     
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
           
            //Debug.Log("Helpers " + (HelpON ? "on" : "off"));
        }
    }
}