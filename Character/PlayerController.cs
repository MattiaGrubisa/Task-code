using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace PlayerController
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        public Rigidbody rb;
        public Camera camera;
        public ParticleSystem ripple;
        public GameObject rippleCamera;


        [Header("Movement")]
        public float rotationSpeed;
        public float movementSpeed;
        public float walkSpeed;
        public float sprintSpeed;
        float horizontalInput;
        float verticalInput;
        private Vector3 movementVector;
        private Vector3 movementDirection;
        private Vector3 moveDirection;
        private Quaternion targetRotation;

        [Header("Drag")]
        public float playerHeight, playerWidth;
        bool grounded;
        private float raycastDistanceForJump;
        public float groundDrag;

        [Header("Jump")]
        public float jumpForce;
        public float jumpCD;
        public float airMultipler;
        bool readyToJump;

        [Header("Keybinds")]
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode sprintKey = KeyCode.LeftShift;

        [Header("States")]
        public LayerMask isGround, isWall, isStairs;
        private RaycastHit raycastForCrouch, raycastForWall;

        [Header("Crouching")]
        public float crouchSpeed;
        private float startYScale;
        private bool crouched;
        public GameObject crouchPos;
        public GameObject wallPos;

        [Header("Steps")]
        [SerializeField] GameObject stepRayUpper;
        [SerializeField] GameObject stepRayLower;
        [SerializeField] float stepHeight = 0.3f;
        [SerializeField] float stepSmooth = 2f;

        public MovementState state;
        public enum MovementState
        {
            walking,
            crouching,
            air,
            sprint,
            wall
        }

        private void MovementStateHandler()
        {
            if (grounded && Input.GetKey(sprintKey))
            {
                state = MovementState.sprint;
                movementSpeed = sprintSpeed;
            }
            else if (grounded)
            {
                state = MovementState.walking;
                movementSpeed = walkSpeed;
                transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            }
            else if (!grounded)
                state = MovementState.air;
            
            if (grounded && canCrouch() && nextToWall())
            {
                state = MovementState.crouching;
                movementSpeed = crouchSpeed;
                transform.localScale = new Vector3(transform.localScale.x, startYScale / 2, transform.localScale.z);
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            }
            if (grounded && nextToWall())
            {
                Debug.Log("wall");
                state = MovementState.wall;
                movementSpeed = walkSpeed - 1f;
            }
        }
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            readyToJump = true;

            startYScale = transform.localScale.y;
        }

        private void Update()
        {
            raycastDistanceForJump = GetComponent<CapsuleCollider>().height / 2 + 0.1f;
            grounded = Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), raycastDistanceForJump, isGround);

            Inputs();
            RotateTowardMovementVector(moveDirection);
            SpeedControl();
            stepClimb();
            canCrouch();
            nextToWall();
            MovementStateHandler();

            if (grounded)
                rb.drag = groundDrag;
            else
                rb.drag = 0;

            rippleCamera.transform.position = transform.position + Vector3.up * 10;
            Shader.SetGlobalVector("_Player", transform.position);

            if (Input.GetKeyDown(KeyCode.G))
            {
                SceneManager.LoadScene("Testing");
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                SceneManager.LoadScene("Sample Scene");
            }
        }

        private void FixedUpdate()
        {
            movingPlayer();
        }

        private void Inputs()
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");

            movementVector = new Vector3(horizontalInput, 0f, verticalInput).normalized;

            if (Input.GetKey(jumpKey) && readyToJump && grounded)
            {
                readyToJump = false;
                Jump();
                Invoke(nameof(ResetJump), jumpCD);
            }
        }

        private void movingPlayer()
        {
            moveDirection = Quaternion.Euler(0, camera.gameObject.transform.eulerAngles.y, 0) * movementVector;
            Vector3 movementDirection = moveDirection * movementSpeed;

            if (grounded)
                rb.AddForce(movementDirection.normalized * movementSpeed * 10f, ForceMode.Force);
            else if (!grounded)
                rb.AddForce(movementDirection.normalized * movementSpeed * 10f * airMultipler, ForceMode.Force);
        }
        private void RotateTowardMovementVector(Vector3 moveDirection)
        {
            if (moveDirection.magnitude == 0) return;

            targetRotation = Quaternion.LookRotation(moveDirection * camera.gameObject.transform.eulerAngles.y);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void SpeedControl()
        {
            Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVelocity.magnitude > movementSpeed)
            {
                Vector3 limitVelocity = flatVelocity.normalized * movementSpeed;
                rb.velocity = new Vector3(limitVelocity.x, rb.velocity.y, limitVelocity.z);
            }
        }

        private void Jump()
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }

        private void ResetJump()
        {
            readyToJump = true;
        }

        private bool canCrouch()
        {
            if (Physics.Raycast(player.Find("CrouchPos").transform.position, moveDirection, out raycastForCrouch, playerWidth))
            {
                return true;
            }
            return false;
        }
        private bool nextToWall()
        {
            if (Physics.Raycast(player.Find("WallPos").transform.position, moveDirection, out raycastForWall, playerWidth))
            {
                return true;
            }
            return false;
        }

        void stepClimb()
        {
            RaycastHit hitLower;
            if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitLower, 0.1f))
            {
                RaycastHit hitUpper;
                if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(Vector3.forward), out hitUpper, 0.2f))
                {
                    rb.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
                }
            }
        }

        void CreateRipple(int Start, int End, int Delta, float Size, int Speed, int Lifetime)
        {
            Vector3 forward = ripple.transform.eulerAngles;
            forward.y = Start;
            ripple.transform.eulerAngles = forward;

            for (int i = Start; i < End; i += Delta)
            {
                ripple.Emit(transform.position + ripple.transform.forward * 0.5f, ripple.transform.forward * Speed, Size, Lifetime, Color.white);
                ripple.transform.eulerAngles += Vector3.up * Delta;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == 4) CreateRipple(-180, 180, 3, 1, 2, 2);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == 4 )
            {
                int y = (int)transform.eulerAngles.y;
                CreateRipple(y - 90, y + 90, 3, 5, 1, 1);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == 4) CreateRipple(-180, 180, 3, 1, 2, 2);
        }
    }
}
