using UnityEngine;
using UnityEngine.UI;

    public class MenuZoom : MonoBehaviour
    {
        public Camera vrCamera;  
        public Transform panel;  
        public Button zoomButton; 

        public float zoomDistance = 1.5f;  
        public float normalDistance = 3f;  
        public float lowerAmount = 0.5f;   
        public float transitionSpeed = 5f;  
        public float zoomedScaleFactor = 1.5f;

        private bool isZoomed = false;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;

    void Start()
        {
            zoomButton.onClick.AddListener(ToggleZoom);

            // initial position & rotation
            originalPosition = panel.position;
            originalRotation = panel.rotation;
        originalScale = panel.localScale;
        }

    void ToggleZoom()
    {
        isZoomed = !isZoomed;

        if (isZoomed)
        {
           
            Vector3 targetPosition = vrCamera.transform.position
                                   + vrCamera.transform.forward * zoomDistance 
                                   - vrCamera.transform.up * lowerAmount; 

            StartCoroutine(SmoothMove(panel, targetPosition, Quaternion.LookRotation(vrCamera.transform.forward), originalScale * zoomedScaleFactor));
        }
        else
        {
            StartCoroutine(SmoothMove(panel, originalPosition, originalRotation, originalScale));
        }
    }

    private System.Collections.IEnumerator SmoothMove(Transform obj, Vector3 targetPosition, Quaternion targetRotation, Vector3 targetScale)
    {
        float elapsedTime = 0;
        Vector3 startPos = obj.position;
        Quaternion startRot = obj.rotation;
        Vector3 startScale = obj.localScale;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * transitionSpeed;
            obj.position = Vector3.Lerp(startPos, targetPosition, elapsedTime);
            obj.rotation = Quaternion.Slerp(startRot, targetRotation, elapsedTime);
            obj.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime);
            yield return null;
        }

        obj.position = targetPosition;
        obj.rotation = targetRotation;
        obj.localScale = targetScale;
    }
}