using UnityEngine;
using UnityEngine.InputSystem;

public class UndoLine : MonoBehaviour
{
    public Design designContainer;
    public RadiiEditorGrip radiiEditor;

    [Header("Controller Input")]
    [SerializeField]
    private InputActionReference rightControllerTriggerActionUndo;

    private void OnEnable()
    {
     
        if (rightControllerTriggerActionUndo != null)
        {
            rightControllerTriggerActionUndo.action.performed += OnUndoButtonPressed;
        }
    }

    private void OnDisable()
    {
       
        if (rightControllerTriggerActionUndo != null)
        {
            rightControllerTriggerActionUndo.action.performed -= OnUndoButtonPressed;
        }
    }


    private void OnUndoButtonPressed(InputAction.CallbackContext context)
    {
        enable();
    }

    private void enable()
    {
        if (radiiEditor != null && radiiEditor.TryResetAndRemoveLastChangedWidth())
        {

            Debug.Log("Width reset to default for the last changed line.");
            return;
        }


        // If no width change was foundn -> to undo the last line
        designContainer.UndoLastLine();
        Debug.Log("Undo Last Line.");
    }
}