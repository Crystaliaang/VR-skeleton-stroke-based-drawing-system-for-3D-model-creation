using UnityEngine;
using UnityEngine.InputSystem;

public class UndoLine2 : MonoBehaviour
{
    public FreeHandDrawing designContainer;
    //public RadiiEditorGrip radiiEditor;

    [Header("Controller Input")]
    [SerializeField]
    private InputActionReference rightControllerTriggerActionUndo;

    private void OnEnable()
    {
        // Subscribe to the performed event
        if (rightControllerTriggerActionUndo != null)
        {
            rightControllerTriggerActionUndo.action.performed += OnUndoButtonPressed;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the performed event to prevent memory leaks
        if (rightControllerTriggerActionUndo != null)
        {
            rightControllerTriggerActionUndo.action.performed -= OnUndoButtonPressed;
        }
    }

    //private void OnUndoButtonPressed(InputAction.CallbackContext context)
    //{
    //    enable();
    //}

    //void enable()
    //{
    //    designContainer.UndoLastLine();
    //    Debug.Log("Undo Line");
    //}

    private void OnUndoButtonPressed(InputAction.CallbackContext context)
    {
        enable();
    }

    private void enable()
    {

        // If no width change was found, proceed to undo the last line
        designContainer.UndoLastLine();
        Debug.Log("Undo Last Line.");
    }
}