using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxMoveSpeed = 5f;
    [SerializeField] private float reverseMaxSpeed = 3f;
    [SerializeField] private float increaseSpeed = 2f;
    [SerializeField] private float decreaseSpeed = 2f;

    [Header("Rotation")]
    [SerializeField] private float rotationIncreaseSpeed = 10f;
    [SerializeField] private float maxRotationSpeed = 20f;

    [Header("Current Speeds")]
    [SerializeField] private float forwardCurrentMoveSpeed = 0f;
    [SerializeField] private float backwardCurrentMoveSpeed = 0f;
    [SerializeField] private float currentRotationSpeed = 0f;
    private Vector3 moveDirection = Vector3.zero;

    [Header("Bobbing")]
    [SerializeField] private float bobAmplitude = 0.3f;    // How high the bob goes
    [SerializeField] private float bobFrequency = 2f;      // How fast it bobs
    [SerializeField] private float xBobAmplitude = 10f;       // X rotation bob amount
    [SerializeField] private float xBobFrequency = 1.5f;      // X rotation speed 
    //[SerializeField] private float xRotationSpeed = 30f;   // How fast it tilts up/down
    //[SerializeField] private float xMaxRotationSpeed = -70f;  
    //[SerializeField] private float xMinRotationSpeed = -90f;  
    //private float currentXRotation = -89.98f;  // Tracks current rotation
    [SerializeField] private float xnormalRotation = -80f;

    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem thrustParticles;
    [SerializeField] private CinemachineCamera boatCamera;

    private float baseYPosition;          // Remember starting Y height

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        baseYPosition = transform.position.y;

        if (thrustParticles != null)
        {
            thrustParticles = GetComponent<ParticleSystem>();
        }
    }

    private void Update()
    {
        //
        // --- Handle Forward and Backward acceleration ---
        //
        bool pressingForward = Input.GetKey(KeyCode.W);
        bool pressingBackward = Input.GetKey(KeyCode.S);

        // Forward movement
        if (pressingForward)
        {
            forwardCurrentMoveSpeed = Mathf.MoveTowards(
                forwardCurrentMoveSpeed, maxMoveSpeed, increaseSpeed * Time.deltaTime);
            backwardCurrentMoveSpeed = Mathf.MoveTowards(
                backwardCurrentMoveSpeed, 0f, decreaseSpeed * Time.deltaTime);
        }
        // Backward movement
        else if (pressingBackward)
        {
            backwardCurrentMoveSpeed = Mathf.MoveTowards(
                backwardCurrentMoveSpeed, reverseMaxSpeed, increaseSpeed * Time.deltaTime);
            forwardCurrentMoveSpeed = Mathf.MoveTowards(
                forwardCurrentMoveSpeed, 0f, decreaseSpeed * Time.deltaTime);
        }
        else
        {
            // Smoothly decelerate both when no input
            forwardCurrentMoveSpeed = Mathf.MoveTowards(
                forwardCurrentMoveSpeed, 0f, decreaseSpeed * Time.deltaTime);
            backwardCurrentMoveSpeed = Mathf.MoveTowards(
                backwardCurrentMoveSpeed, 0f, decreaseSpeed * Time.deltaTime);
        }

        // --- Combine forward and backward for total current speed ---
        float netMoveSpeed = forwardCurrentMoveSpeed - backwardCurrentMoveSpeed;

        // --- Movement direction ---
        moveDirection = -transform.up; // Assuming forward is negative Y

        // --- Apply movement ---
        rb.linearVelocity = moveDirection * netMoveSpeed;

        if (forwardCurrentMoveSpeed == 0f && backwardCurrentMoveSpeed == 0f)
        {
            boatCamera.Lens.FieldOfView = 64.5f;
            thrustParticles.Stop();
        }
        else 
        {
            boatCamera.Lens.FieldOfView = 97f;
            thrustParticles.Play();
        }

            //
            // --- Rotation controls ---
            //
            float targetRotationSpeed = (Input.GetKey(KeyCode.D) ||
    Input.GetKey(KeyCode.A)) ? maxRotationSpeed : 0f;

        float rotationRate = targetRotationSpeed > currentRotationSpeed 
            ? increaseSpeed : decreaseSpeed;

        currentRotationSpeed = Mathf.MoveTowards(currentRotationSpeed, targetRotationSpeed,
rotationIncreaseSpeed * Time.deltaTime);

        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.D)) rotationInput = 1f;
        if (Input.GetKey(KeyCode.A)) rotationInput = -1f;
        transform.Rotate(0f, 0f, rotationInput * currentRotationSpeed * Time.deltaTime);



        //
        // --- BOBBING ---
        //

        // --- X rotation (Pitch: tilt up/down based on W/S) ---
        //if (pressingForward)
        //    currentXRotation = Mathf.MoveTowards(currentXRotation, xMinRotationSpeed, xRotationSpeed * Time.deltaTime); // tilt up
        //else if (pressingBackward)
        //    currentXRotation = Mathf.MoveTowards(currentXRotation, xMaxRotationSpeed, xRotationSpeed * Time.deltaTime);  //down
        //else
        //    currentXRotation = Mathf.MoveTowards(currentXRotation, xnormalRotation, xRotationSpeed * Time.deltaTime);   // return to neutral

        // 1. Up/down position bobbing (sine wave)
        float yBobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        Vector3 newPosition = transform.position;
        newPosition.y = baseYPosition + yBobOffset;
        transform.position = newPosition;

        // 2. X rotation bobbing (cosine wave - offset from Y for natural feel)
        float xBobRotation = xnormalRotation + (Mathf.Cos(Time.time * xBobFrequency) * xBobAmplitude);

        // Apply only X rotation (preserve Y/Z rotation from controls)
        Vector3 eulerAngles = transform.localEulerAngles;
        eulerAngles.x = xBobRotation;
        transform.localEulerAngles = eulerAngles;
    }
}
