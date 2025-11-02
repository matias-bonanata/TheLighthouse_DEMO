using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem.XInput;

public class ClickToInspect : MonoBehaviour
{
    [SerializeField] public MonoBehaviour scriptToActivate;
    [SerializeField] public MonoBehaviour ChecklistScript;
    [SerializeField] public GameObject backButton;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Vector3 savedPosition;
    private Quaternion savedRotation;

    private bool isReturning = false;
    [SerializeField] private float returnSpeed = 8f; // you can adjust speed as needed

    [SerializeField] private bool willDisableCameraMovement = false;
    [SerializeField] private CinemachineInputAxisController inputController;

    void Start()
    {
        if (scriptToActivate != null)
            scriptToActivate.enabled = false;

        if (backButton != null)
            backButton.SetActive(false);

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void OnMouseDown()
    {
        // Ignore input if returning
        if (isReturning) return;

        if (willDisableCameraMovement == true)
        {
            if (inputController != null)
                inputController.enabled = false;
        }

        if (scriptToActivate != null)
            scriptToActivate.enabled = true;

        if (ChecklistScript != null)
            ChecklistScript.enabled = true;

        if (backButton != null)
            backButton.SetActive(true);
    }

    public void DeactivateScript()
    {
        // Save current position and rotation before starting the return
        savedPosition = transform.position;
        savedRotation = transform.rotation;

        if (scriptToActivate != null)
            scriptToActivate.enabled = false;

        if (ChecklistScript != null)
            ChecklistScript.enabled = false;

        if (inputController != null)
            inputController.enabled = true;

        isReturning = true;

        if (backButton != null)
            backButton.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            DeactivateScript();
        }

        if (isReturning)
        {
            // Smoothly interpolate position and rotation back to initial
            transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * returnSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, Time.deltaTime * returnSpeed);

            // Check if close enough to the initial transform to stop returning
            if (Vector3.Distance(transform.position, initialPosition) < 0.01f &&
                Quaternion.Angle(transform.rotation, initialRotation) < 0.5f)
            {
                transform.position = initialPosition;
                transform.rotation = initialRotation;
                isReturning = false;
            }
        }
    }
}
