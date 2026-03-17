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

    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem thrustParticles;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (thrustParticles == null)
        {
            thrustParticles = GetComponent<ParticleSystem>();
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
    }

    private void Update()
    {
        HandleInput();
        HandleParticles();
    }

    private void HandleInput()
    {
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
            forwardCurrentMoveSpeed = Mathf.MoveTowards(
                forwardCurrentMoveSpeed, 0f, decreaseSpeed * Time.deltaTime);
            backwardCurrentMoveSpeed = Mathf.MoveTowards(
                backwardCurrentMoveSpeed, 0f, decreaseSpeed * Time.deltaTime);
        }

        // Rotation input
        float targetRotationSpeed = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.A)) ? maxRotationSpeed : 0f;
        currentRotationSpeed = Mathf.MoveTowards(currentRotationSpeed, targetRotationSpeed, rotationIncreaseSpeed * Time.deltaTime);
    }

    private void HandleMovement()
    {
        float netMoveSpeed = forwardCurrentMoveSpeed - backwardCurrentMoveSpeed;
        moveDirection = -transform.up; // Forward is negative Y (water surface)
        rb.linearVelocity = moveDirection * netMoveSpeed;
    }

    private void HandleRotation()
    {
        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.D)) rotationInput = 1f;   // Right = positive Z rotation
        if (Input.GetKey(KeyCode.A)) rotationInput = -1f;  // Left = negative Z rotation

        // Z rotation (yaw/turning left/right on water surface)
        Vector3 rotationTorque = transform.forward * rotationInput * currentRotationSpeed;
        rb.angularVelocity = rotationTorque;
    }

    private void HandleParticles()
    {
        if (forwardCurrentMoveSpeed == 0f && backwardCurrentMoveSpeed == 0f && thrustParticles != null)
        {
            thrustParticles.Stop();
        }
        else if (thrustParticles != null)
        {
            thrustParticles.Play();
        }
    }
}
