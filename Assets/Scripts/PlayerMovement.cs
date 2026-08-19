using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerInput _inputs;
    private Transform _mainCamera;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _playerVisual;

    [Header("Movement Settings")]
    [SerializeField] private float _currentPlayerSpeed = 5f;
    [SerializeField] private float _walkPlayerSpeed = 5f;
    [SerializeField] private float _sprintPlayerSpeed = 8f;
    [SerializeField] private float _crouchPlayerSpeed = 3f;
    [SerializeField] private float defaultGravity = 9.81f;
    private float _verticalVelocity;

    [Header("Rotation Settings")]
    [SerializeField] private LayerMask _floorLayer;
    [SerializeField] private float _rotationSpeed = 15f;

    [Header("Crouch Settings")]
    [SerializeField] private float _crouchTransitionSpeed = 10f; 
    private float _standingHeight = 2f;
    private float _crouchingHeight = 1f;
    private Vector3 _standingCenter = new Vector3(0f, 0f, 0f);
    private Vector3 _crouchingCenter = new Vector3(0f, -0.5f, 0f); // Shifts down so feet don't float


    [Header("Ceiling Detection")]
    [SerializeField] private LayerMask _ceilingLayer;
    private float _ceilingCheckRadius = 0.4f;
    private Vector3 _lastCeilingCheckPos;


    private void Awake()
    {
        _inputs = new PlayerInput();    
    }

    private void Start()
    {
        _mainCamera = Camera.main.transform;

        if (_characterController != null)
        {
            _standingHeight = _characterController.height;
            _standingCenter = _characterController.center;

            // Calculate the perfect crouch values dynamically based on your standing setup
            _crouchingHeight = _standingHeight * 0.5f; // 50% scale
            _crouchingCenter = new Vector3(
                _standingCenter.x,
                _standingCenter.y - (_standingHeight - _crouchingHeight) * 0.5f,
                _standingCenter.z
            );
        }
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

      
        float distanceToStandingTop = _standingHeight * 0.5f; 

        _lastCeilingCheckPos = transform.position + (Vector3.up * (distanceToStandingTop - _ceilingCheckRadius));

        // Get the layer index for the Player layer
        int playerLayerIndex = LayerMask.NameToLayer("Player");

        // Create a mask that selects EVERYTHING except the Player layer
        // The '~' operator inverts the bits, meaning "Not Player"
        int crossWorldCeilingMask = ~(1 << playerLayerIndex);

        // Execute the check using the inverted mask
        bool isCeilingAbove = Physics.CheckSphere(_lastCeilingCheckPos, _ceilingCheckRadius, crossWorldCeilingMask);

        bool shouldBeCrouched = isCrouchPressed || isCeilingAbove;

        float targetHeight = _standingHeight;
        Vector3 targetCenter = _standingCenter;

        Vector3 targetVisualScale = new Vector3(1f, 1f, 1f);
        Vector3 targetVisualPosition = Vector3.zero; 

        if (shouldBeCrouched)
        {
            _currentPlayerSpeed = _crouchPlayerSpeed;
            targetHeight = _crouchingHeight;
            targetCenter = _crouchingCenter;

            targetVisualScale = new Vector3(1f, 0.5f, 1f);
            targetVisualPosition = new Vector3(0f, -0.5f, 0f);
        }
        else if (isSprintPressed && inputVector.y > 0f) //Temporarily only sprint forward
        {
            _currentPlayerSpeed = _sprintPlayerSpeed;
        }
        else
        {
            _currentPlayerSpeed = _walkPlayerSpeed;
        }

        _characterController.height = Mathf.Lerp(_characterController.height, targetHeight, Time.deltaTime * _crouchTransitionSpeed);
        _characterController.center = Vector3.Lerp(_characterController.center, targetCenter, Time.deltaTime * _crouchTransitionSpeed);

        if (_playerVisual != null)
        {
            _playerVisual.localScale = Vector3.Lerp(_playerVisual.localScale, targetVisualScale, Time.deltaTime * _crouchTransitionSpeed);
            _playerVisual.localPosition = Vector3.Lerp(_playerVisual.localPosition, targetVisualPosition, Time.deltaTime * _crouchTransitionSpeed);
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


    private void OnDrawGizmos()
    {
        // Ensure the gizmo only draws if the application is playing and running the math
        if (!Application.isPlaying) return;

        // Change color depending on whether it's hitting something or clear
        // We can use a simple check here just for visual coloring
        Gizmos.color = Color.red;

        // Draw the invisible check sphere as a wireframe ball in the Scene view
        Gizmos.DrawWireSphere(_lastCeilingCheckPos, _ceilingCheckRadius);
    }

}
