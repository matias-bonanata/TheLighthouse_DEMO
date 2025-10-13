using UnityEngine;

public class GoToCamera : MonoBehaviour
{
    [Header("Camera Preferences")]
    [SerializeField] private Camera mainCamera; // Assign your camera in inspector or find in Start
    [SerializeField] private float smoothSpeed = 5f; // Speed of the smooth movement 
    [SerializeField] private float distanceInFront = 1.3f; // Distance to keep in front of the camera
    [SerializeField] private float rotationX = 0f; // Speed of the smooth movement 
    [SerializeField] private float rotationY = -90f; // Speed of the smooth movement 
    [SerializeField] private float rotationZ = 0.5f; // Speed of the smooth movement 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // If no camera assigned, use the main camera by default
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Rotate Object
        Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * distanceInFront;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // Calculate the target rotation so the object faces the camera but offsets the camera's 16° X rotation
        Quaternion cameraRotation = mainCamera.transform.rotation;

        //Do movement
        Quaternion editMovement = Quaternion.Euler(rotationX, rotationY, rotationZ);

        // Apply the compensation so object rotation is camera rotation minus that 16 degrees on X-axis
        Quaternion targetRotation = cameraRotation * editMovement;

        // Smooth rotate to the adjusted target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }
}
