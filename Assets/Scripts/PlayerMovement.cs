using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerInput _inputs;
    private Transform _mainCamera;
    [SerializeField] private CharacterController _characterController;

    [SerializeField] private float _currentPlayerSpeed = 5f;
    [SerializeField] private float _walkPlayerSpeed = 5f;
    [SerializeField] private float _sprintPlayerSpeed = 8f;
    [SerializeField] private float _crouchPlayerSpeed = 3f;
    [SerializeField] private float defaultGravity = 9.81f;
    private float _verticalVelocity;

    [SerializeField] private LayerMask _floorLayer;
    [SerializeField] private float _rotationSpeed = 15f;

    private void Awake()
    {
        _inputs = new PlayerInput();    
    }

    private void Start()
    {
        _mainCamera = Camera.main.transform;
    }

    private void OnEnable()
    {
        _inputs.Enable();
    }

    private void OnDisable()
    {
        _inputs.Disable();
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        Vector2 inputVector = _inputs.Player.Move.ReadValue<Vector2>();
        Vector3 cameraForward = _mainCamera.forward;
        Vector3 cameraRight = _mainCamera.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * inputVector.y) + (cameraRight * inputVector.x);

        if (_characterController.isGrounded)
        {
            _verticalVelocity = -2f;
            if (_inputs.Player.Jump.WasPressedThisFrame())
            {
                _verticalVelocity = 5.5f;
            }
        }
        else
        {
            _verticalVelocity -= defaultGravity * Time.deltaTime;
        }

        bool isCrouchPressed = _inputs.Player.Crouch.IsPressed();
        bool isSprintPressed = _inputs.Player.Sprint.IsPressed();

        if (isCrouchPressed)
        {
            _currentPlayerSpeed = _crouchPlayerSpeed;
        }
        else if (isSprintPressed && inputVector.y > 0f) //Temporarily only run forward
        {
            _currentPlayerSpeed = _sprintPlayerSpeed;
        }
        else
        {
            _currentPlayerSpeed = _walkPlayerSpeed;
        }

        Vector3 finalMovementVector = moveDirection * _currentPlayerSpeed;
        finalMovementVector.y = _verticalVelocity;

        _characterController.Move(finalMovementVector * Time.deltaTime);
    }

    private void HandleRotation()
    {
        // STEP 1: Shoot a physical ray from the mouse position through the camera lens
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // STEP 2: Ask Unity if that ray hit our specific floor layer
        // 'out RaycastHit hit' is just a container where Unity saves the information of the collision
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _floorLayer))
        {
            // STEP 3: Find the direction from the player's current position to the floor hit point
            // (Target position minus Current position)
            Vector3 targetDirection = hit.point - transform.position;

            // STEP 4: The Sanitization! Force the Y axis to zero so the capsule never tilts down
            targetDirection.y = 0f;

            // STEP 5: Check if the direction is valid (to avoid errors if the mouse is exactly on the player)
            if (targetDirection.sqrMagnitude > 0.001f)
            {
                // Turn our flat direction vector into a target rotation state
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

                // Smoothly rotate from our current rotation to the target rotation over time
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
            }
        }
    }

}
