using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FastStudios.Demo
{
    public class PlayerMovement_DEMO : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] FirstPersonCameraOffline fpCam;
        public bool allowedToMove = true;
        public bool allowedToJump;
        public bool allowedToCrouch;
        [SerializeField] Transform orientation;
        [SerializeField] LayerMask ground;
        [SerializeField] float playerHeight;
        [SerializeField] float counterMovement = 2f;
        [SerializeField] float CameraFOV = 60f;
        private float threshold = 0.01f;

        //Private - General
        private float speed;
        private float horizontal;
        private float vertical;
        [SerializeField] private float groundDrag;
        [SerializeField] private bool grounded;
        private bool jumping;
        private bool crouching;
        private Vector3 direction;
        private Rigidbody rb;

        [Header("Walk")]
        [SerializeField] float walkSpeed;

        [Header("Sprint")]
        [SerializeField] float sprintSpeed;

        [Header("Crouch")]
        [SerializeField] float crouchSpeed;
        [SerializeField] float crouchSprintSpeed;
        [SerializeField] float crouchScale;
        [SerializeField] float startScale;

        [Header("Jump")]
        [SerializeField] float jumpForce;
        [SerializeField] float jumpCooldown;
        [SerializeField] float airMultiplier;
        [SerializeField] float fallMultiplier;
        private bool readyToJump = true;

        [Header("Slope")]
        [SerializeField] private float maxSlopeAngle = 35f;
        private RaycastHit slopeHit;
        private bool exitSlope;

        [Header("KeyCodes")]
        [SerializeField] KeyCode jumpKey = KeyCode.Space;
        [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] KeyCode crouchKey = KeyCode.LeftControl;

#if ENABLE_INPUT_SYSTEM
        [Header("New Input System (Optional)")]
        [SerializeField] InputActionReference moveAction;
        [SerializeField] InputActionReference jumpAction;
        [SerializeField] InputActionReference sprintAction;
        [SerializeField] InputActionReference crouchAction;
#endif

        private bool sprintHeld;
        private bool crouchWasHeld;
        private bool crouchDown;
        private bool crouchUp;

        #region Monobehaviour

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            startScale = transform.localScale.y;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 240;
        }

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            moveAction?.action?.Enable();
            jumpAction?.action?.Enable();
            sprintAction?.action?.Enable();
            crouchAction?.action?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.action?.Disable();
            jumpAction?.action?.Disable();
            sprintAction?.action?.Disable();
            crouchAction?.action?.Disable();
        }
#endif

        private void Update()
        {
            if (allowedToMove)
            {
                MyInput();
                LimitSpeed();
                StateHandler();

                CameraFOV_Effect(1.15f, 5);

                if (!OnSlope())
                {
#if UNITY_6000_0_OR_NEWER
                    if (grounded)
                        rb.linearDamping = groundDrag;
                    else
                        rb.linearDamping = 0;
#elif UNITY_2022_3_OR_NEWER
                    if (grounded)
                        rb.drag = groundDrag;
                    else
                        rb.drag = 0;
#endif
                }
            }
        }

        private void FixedUpdate()
        {
            if (allowedToMove)
            {
                Move();

                if (allowedToJump) Jump();

#if UNITY_6000_0_OR_NEWER
                if (rb.linearVelocity.y < 0)
                {
                    rb.linearVelocity += Vector3.up * Physics.gravity.y * fallMultiplier * Time.deltaTime;
                }
#elif UNITY_2022_3_OR_NEWER
                if (rb.velocity.y < 0)
                {
                    rb.velocity += Vector3.up * Physics.gravity.y * fallMultiplier * Time.deltaTime;
                }
#endif
            }
        }

        #endregion

        public void SetMovement(bool enabled) // Called by unity event
        {
            allowedToMove = enabled;
            allowedToJump = enabled;
            allowedToCrouch = enabled;
            fpCam.allowedToLook = enabled;
        }

        void StateHandler()
        {
            if (crouching)
            {
                speed = sprintHeld ? crouchSprintSpeed : crouchSpeed;
            }
            else if (grounded && sprintHeld)
            {
                speed = sprintSpeed;
            }
            else if (grounded)
            {
                speed = walkSpeed;
            }
        }

        #region Movement Logic

        void MyInput()
        {
            Vector2 move = Vector2.zero;
            bool jumpHeld = false;
            bool sprint = false;
            bool crouchHeld = false;

#if ENABLE_LEGACY_INPUT_MANAGER
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            jumpHeld = Input.GetButton("Jump") || Input.GetKey(jumpKey);
            sprint = Input.GetKey(sprintKey);
            crouchHeld = Input.GetKey(crouchKey);
#endif

#if ENABLE_INPUT_SYSTEM
            if (moveAction != null && moveAction.action != null)
            {
                move = moveAction.action.ReadValue<Vector2>();
            }
            else
            {
                if (Gamepad.current != null)
                    move += Gamepad.current.leftStick.ReadValue();

                if (Keyboard.current != null)
                {
                    float mx = 0f;
                    float my = 0f;
                    if (Keyboard.current.aKey.isPressed) mx -= 1f;
                    if (Keyboard.current.dKey.isPressed) mx += 1f;
                    if (Keyboard.current.sKey.isPressed) my -= 1f;
                    if (Keyboard.current.wKey.isPressed) my += 1f;
                    move += new Vector2(mx, my);
                }

                move = Vector2.ClampMagnitude(move, 1f);
            }

            // JUMP
            if (jumpAction != null && jumpAction.action != null) jumpHeld = jumpAction.action.IsPressed();
            else
            {
                if (Keyboard.current != null) jumpHeld |= Keyboard.current.spaceKey.isPressed;
                if (Gamepad.current != null) jumpHeld |= Gamepad.current.buttonSouth.isPressed;
            }

            // SPRINT
            if (sprintAction != null && sprintAction.action != null) sprint = sprintAction.action.IsPressed();
            else
            {
                if (Keyboard.current != null) sprint |= Keyboard.current.leftShiftKey.isPressed;
                if (Gamepad.current != null) sprint |= Gamepad.current.leftStickButton.isPressed;
            }

            // CROUCH
            if (crouchAction != null && crouchAction.action != null) crouchHeld = crouchAction.action.IsPressed();
            else
            {
                if (Keyboard.current != null) crouchHeld |= Keyboard.current.leftCtrlKey.isPressed;
                if (Gamepad.current != null) crouchHeld |= Gamepad.current.buttonEast.isPressed;
            }

#endif

            horizontal = move.x;
            vertical = move.y;

            jumping = jumpHeld;

            sprintHeld = sprint;

            if (allowedToCrouch) crouching = crouchHeld;
            else crouching = false;

            crouchDown = crouching && !crouchWasHeld;
            crouchUp = !crouching && crouchWasHeld;
            crouchWasHeld = crouching;

            if (allowedToCrouch) Crouch();
        }

        void Move()
        {
            direction = orientation.forward * vertical + orientation.right * horizontal;

            Vector2 mag = FindVelRelativeToLook();
            CounterMovement(horizontal, vertical, mag);

            if (OnSlope() && !exitSlope)
            {
                rb.AddForce(GetSlopeMoveDirection() * speed * 30f, ForceMode.Force);

#if UNITY_6000_0_OR_NEWER
                if (rb.linearVelocity.y > 0)
                {
                    rb.AddForce(Vector3.down * 80f, ForceMode.Force);
                }

                rb.linearDamping = 6f;
#elif UNITY_2022_3_OR_NEWER
                if (rb.velocity.y > 0)
                {
                    rb.AddForce(Vector3.down * 80f, ForceMode.Force);
                }

                rb.drag = 6f;
#endif
            }
            else if (grounded)
            {
                rb.AddForce(direction.normalized * speed * 10f, ForceMode.Force);
            }
            else if (!grounded)
            {
                rb.AddForce(direction.normalized * speed * 10f * airMultiplier, ForceMode.Force);
            }

            rb.useGravity = !OnSlope();
        }

        void LimitSpeed()
        {
            if (OnSlope() && !exitSlope)
            {
#if UNITY_6000_0_OR_NEWER
                if (rb.linearVelocity.magnitude > speed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * speed;
                }
#elif UNITY_2022_3_OR_NEWER
                if (rb.velocity.magnitude > speed)
                {
                    rb.velocity = rb.velocity.normalized * speed;
                }
#endif
            }
            else
            {
#if UNITY_6000_0_OR_NEWER
                Vector3 _2dVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                if (_2dVel.magnitude > speed)
                {
                    Vector3 limitedVel = _2dVel.normalized * speed;
                    rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
                }
#elif UNITY_2022_3_OR_NEWER
                Vector3 _2dVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

                if (_2dVel.magnitude > speed)
                {
                    Vector3 limitedVel = _2dVel.normalized * speed;
                    rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
                }
#endif
            }
        }

        #endregion

        #region Jump
        void Jump()
        {
            if (jumping && readyToJump && grounded)
            {
                readyToJump = false;
                exitSlope = true;

#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
#elif UNITY_2022_3_OR_NEWER
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
#endif

                rb.AddForce(Vector2.up * jumpForce, ForceMode.Impulse);
                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }

        void ResetJump()
        {
            readyToJump = true;
            exitSlope = false;
        }
        #endregion

        #region CounterMovement
        private void CounterMovement(float x, float y, Vector2 mag)
        {
            if (!grounded || jumping) return;

            if (crouching)
            {
#if UNITY_6000_0_OR_NEWER
                rb.AddForce(speed * Time.deltaTime * -rb.linearVelocity.normalized * 0.2f);
#elif UNITY_2022_3_OR_NEWER
                rb.AddForce(speed * Time.deltaTime * -rb.velocity.normalized * 0.2f);
#endif
                return;
            }

            if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
            {
                rb.AddForce(speed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
            }
            if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
            {
                rb.AddForce(speed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
            }

#if UNITY_6000_0_OR_NEWER
            if (Mathf.Sqrt(Mathf.Pow(rb.linearVelocity.x, 2) + Mathf.Pow(rb.linearVelocity.z, 2)) > speed)
            {
                float fallspeed = rb.linearVelocity.y;
                Vector3 n = rb.linearVelocity.normalized * speed;
                rb.linearVelocity = new Vector3(n.x, fallspeed, n.z);
            }
#elif UNITY_2022_3_OR_NEWER
            if (Mathf.Sqrt(Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2)) > speed)
            {
                float fallspeed = rb.velocity.y;
                Vector3 n = rb.velocity.normalized * speed;
                rb.velocity = new Vector3(n.x, fallspeed, n.z);
            }
#endif
        }

        public Vector2 FindVelRelativeToLook()
        {
            float lookAngle = orientation.transform.eulerAngles.y;
#if UNITY_6000_0_OR_NEWER
            float moveAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;
#elif UNITY_2022_3_OR_NEWER
            float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;
#endif

            float u = Mathf.DeltaAngle(lookAngle, moveAngle);
            float v = 90 - u;

#if UNITY_6000_0_OR_NEWER
            float magnitue = rb.linearVelocity.magnitude;
#elif UNITY_2022_3_OR_NEWER
            float magnitue = rb.velocity.magnitude;
#endif
            float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
            float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

            return new Vector2(xMag, yMag);
        }
        #endregion

        #region Crouch
        void Crouch()
        {
            if (crouchDown)
            {
                transform.localScale = new Vector3(transform.localScale.x, crouchScale, transform.localScale.z);
                rb.AddForce(Vector3.down * 10f, ForceMode.Impulse);
            }

            if (crouchUp)
            {
                transform.localScale = new Vector3(transform.localScale.x, startScale, transform.localScale.z);
                rb.AddForce(Vector3.up * 1f, ForceMode.Impulse);
            }
        }
        #endregion

        #region POLISH
        void CameraFOV_Effect(float sprintFovMultiplier, float time)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

#if UNITY_6000_0_OR_NEWER
            Vector3 vel = rb.linearVelocity;
#elif UNITY_2022_3_OR_NEWER
            Vector3 vel = rb.velocity;
#endif

            if (vel.magnitude > 4.5f && vertical > 0f && sprintHeld)
            {
                if (cam.fieldOfView != CameraFOV * sprintFovMultiplier)
                {
                    cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, CameraFOV * sprintFovMultiplier, Time.deltaTime * time);
                }
            }
            else
            {
                if (cam.fieldOfView != CameraFOV)
                {
                    cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, CameraFOV, Time.deltaTime * time);
                }
            }
        }
        #endregion

        #region GroundCheck
        private bool IsFloor(Vector3 v)
        {
            float angle = Vector3.Angle(Vector3.up, v);
            return angle < maxSlopeAngle;
        }

        private bool cancellingGrounded;

        private void OnCollisionStay(Collision other)
        {
            int layer = other.gameObject.layer;
            if (ground != (ground | (1 << layer))) return;

            for (int i = 0; i < other.contactCount; i++)
            {
                Vector3 normal = other.contacts[i].normal;

                if (IsFloor(normal))
                {
                    grounded = true;
                    cancellingGrounded = false;
                    CancelInvoke(nameof(StopGrounded));
                }
            }
        }

        private void OnCollisionExit(Collision other)
        {
            int layer = other.gameObject.layer;
            if (ground != (ground | (1 << layer))) return;

            float delay = .5f;
            if (!cancellingGrounded)
            {
                cancellingGrounded = true;
                Invoke(nameof(StopGrounded), Time.deltaTime * delay);
            }
        }

        private void StopGrounded()
        {
            grounded = false;
        }
        #endregion

        #region Slope
        private bool OnSlope()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                return angle < maxSlopeAngle && angle != 0;
            }

            return false;
        }

        private Vector3 GetSlopeMoveDirection()
        {
            return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
        }
        #endregion
    }
}
