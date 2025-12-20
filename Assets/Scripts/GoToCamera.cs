using UnityEngine;

public class GoToCamera : MonoBehaviour
{
    [Header("Camera Preferences")]
    [SerializeField] private Camera mainCamera; // Assign your camera in inspector or find in Start
    [SerializeField] private float smoothSpeed = 5f; // Speed of the smooth movement 
    [SerializeField] public float distanceInFront = 1.3f; // Distance to keep in front of the camera
    [SerializeField] private float rotationX = 0f; // Speed of the smooth movement 
    [SerializeField] private float rotationY = -90f; // Speed of the smooth movement 
    [SerializeField] private float rotationZ = 0.5f; // Speed of the smooth movement 

    //Move with click
    [SerializeField] private float rotationSpeed = 5f; // sensitivity for mouse drag rotation
    private Vector3 lastMousePosition;
    private bool isDragging = false;

    //if is paper
    [Header("canScroll")]
    [SerializeField] private bool canScroll = false;
    [SerializeField] private float scrollSpeed = 0.1f;
    [SerializeField] private float yAxisModif = 0f;

    private void Awake()
    {


        // Initialize camera in Awake to ensure it's set before the script might be disabled
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void Start()
    {
        // Double-check camera assignment
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Safety check - exit if no camera
        if (mainCamera == null)
        {
            return;
        }

        // Handle mouse drag rotation input
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            // Update rotations based on mouse delta
            rotationX += mouseDelta.y * rotationSpeed * Time.deltaTime; // Invert Y if needed
            rotationY += mouseDelta.x * rotationSpeed * Time.deltaTime;
            rotationZ += mouseDelta.x * rotationSpeed * Time.deltaTime * 0.5f; // adjust factor to suit rotationZ control
        }

        // Move object toward camera
        Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * distanceInFront + mainCamera.transform.up * yAxisModif;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // Calculate rotation to face camera plus offsets
        Quaternion cameraRotation = mainCamera.transform.rotation;
        Quaternion editMovement = Quaternion.Euler(rotationX, rotationY, rotationZ);
        Quaternion targetRotation = cameraRotation * editMovement;

        // Smoothly rotate
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);

        //IF PAPER SCROLL DOWN
        if (canScroll)
        {
            Vector3 mousePos = Input.mousePosition;
            if (mousePos.y <= 50)
            {
                yAxisModif += scrollSpeed;
            }
            if (mousePos.y >= Screen.height - 50)
            {
                yAxisModif -= scrollSpeed;
            }
            yAxisModif = Mathf.Clamp(yAxisModif, -0.264f, 0.192f);
        }
    }
}
