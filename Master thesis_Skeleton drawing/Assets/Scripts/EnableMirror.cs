using UnityEngine;
using UnityEngine.InputSystem;

public class EnableMirror : MonoBehaviour
{
    public Design designContainer;

    [Header("Controller Input")]
    [SerializeField]
    private InputActionReference rightControllerTriggerActionMirror;

    public static int MirrorEnabledNum = 0;

    private void OnEnable()
    {
        if (rightControllerTriggerActionMirror != null)
        {
            rightControllerTriggerActionMirror.action.performed += OnMirrorButtonPressed;
        }
    }

    private void OnDisable()
    {
        if (rightControllerTriggerActionMirror != null)
        {
            rightControllerTriggerActionMirror.action.performed -= OnMirrorButtonPressed;
        }
    }

    private void OnMirrorButtonPressed(InputAction.CallbackContext context)
    {
        MirrorEnabledNum++;
        designContainer.Mirror();
    }

}