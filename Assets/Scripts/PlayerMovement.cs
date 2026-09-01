using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    private PlayerInput _inputs;
    private Transform _mainCamera;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _playerVisual;
    [SerializeField] private Transform _weaponHoldAnchor;


    [Header("Movement Settings")]
    [SerializeField] private bool _canSprint = true;
    [SerializeField] private float _currentPlayerSpeed = 5f;
    [SerializeField] private float _walkPlayerSpeed = 5f;
    [SerializeField] private float _sprintPlayerSpeed = 8f;
    [SerializeField] private float _crouchPlayerSpeed = 3f;

    [SerializeField] private bool _canJump = false;
    [SerializeField] private float defaultGravity = 9.81f;
    private float _verticalVelocity;

    [Header("Rotation Settings")]
    [SerializeField] private LayerMask _floorLayer;
    [SerializeField] private float _rotationSpeed = 15f;

    [Header("Crouch Settings")]
    [SerializeField] private bool _canCrouch = false;
    [SerializeField] private float _crouchTransitionSpeed = 10f; 
    private float _standingHeight = 2f;
    private float _crouchingHeight = 1f;
    private Vector3 _standingCenter = new Vector3(0f, 0f, 0f);
    private Vector3 _crouchingCenter = new Vector3(0f, -0.5f, 0f); // Shifts down so feet don't float

    [Header("Stamina Settings")]
    [SerializeField] private bool _useStamina = true;
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _sprintStaminaDrain = 10f;
    [SerializeField] private float _staminaRegenRate = 15f;
    [SerializeField] private float _crouchStaminaRegenRate = 25f;
    [SerializeField] private float _staminaRegenDelay = 0.75f;

    private float _currentStamina;
    private float _staminaRegenTimer;


    [Header("Ceiling Detection")]
    [SerializeField] private LayerMask _ceilingLayer;
    private float _ceilingCheckRadius = 0.4f;
    private Vector3 _lastCeilingCheckPos;

    [SerializeField] private PlayerMovementState _movementState;

    public enum PlayerMovementState
    {
        Idle,
        Walking,
        Sprinting,
        Crouching
    }


    private void Awake()
    {
    }

    private void Start()
    {
        _mainCamera = Camera.main.transform;
        _currentStamina = _maxStamina;

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

        _inputs = Player.Instance.GetInputInstance();
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void Update()
    {

        if(GameManager.Instance.State == GameState.Playing)
        {
            Vector3 moveDirection = HandleMovement();
            HandleRotation(moveDirection);
        }
    }

    private Vector3 HandleMovement()
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
            if (_inputs.Player.Jump.WasPressedThisFrame() && _canJump)
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

        if (shouldBeCrouched && _canCrouch)
        {
            _movementState = PlayerMovementState.Crouching;
            _currentPlayerSpeed = _crouchPlayerSpeed;

            targetHeight = _crouchingHeight;
            targetCenter = _crouchingCenter;

            targetVisualScale = new Vector3(1f, 0.5f, 1f);
            targetVisualPosition = new Vector3(0f, -0.5f, 0f);
        }
        else if (isSprintPressed && _canSprint &&  inputVector.sqrMagnitude > 0.01f && (!_useStamina || _currentStamina > 0f))
        {
            _movementState = PlayerMovementState.Sprinting;
            _currentPlayerSpeed = _sprintPlayerSpeed;
        }
        else if (inputVector.sqrMagnitude > 0.01f)
        {
            _movementState = PlayerMovementState.Walking;
            _currentPlayerSpeed = _walkPlayerSpeed;
        }
        else
        {
            _movementState = PlayerMovementState.Idle;
            _currentPlayerSpeed = 0f;
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

        if (_useStamina && _movementState == PlayerMovementState.Sprinting)
        {
            ConsumeStamina(_sprintStaminaDrain * Time.deltaTime);
        }

        HandleStaminaRegeneration();

        _characterController.Move(finalMovementVector * Time.deltaTime);

        return moveDirection;
    }

    private void HandleRotation(Vector3 moveDirection)
    {
        // Rotate towards player input direction when sprinting, otherwise rotate towards mouse position
        if (_movementState == PlayerMovementState.Sprinting &&
        moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * _rotationSpeed
            );

            return;
        }

        // STEP 1: Shoot a physical ray from the mouse position through the camera lens
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // STEP 2: Determine our target aiming height dynamically based on the active weapon anchor point
        // If no anchor point is found, default to a standard chest height of 1.2 units.
        float weaponAimHeight = _weaponHoldAnchor != null ? _weaponHoldAnchor.position.y : 1.2f;

        // STEP 3: Create an invisible mathematical plane facing upwards, located at our weapon's exact height
        Plane aimingPlane = new Plane(Vector3.up, new Vector3(0f, weaponAimHeight, 0f));

        // STEP 4: Calculate exactly where the screen ray crosses this invisible height plane
        if (aimingPlane.Raycast(ray, out float enterDistance))
        {
            // Get the precise 3D point on the weapon-height plane where the ray hit
            Vector3 worldHitPoint = ray.GetPoint(enterDistance);

            // STEP 5: Find the direction from the player's current position to our height-aligned point
            Vector3 targetDirection = worldHitPoint - transform.position;

            // STEP 6: The Sanitization! Force the Y axis to zero so the capsule never tilts up or down
            targetDirection.y = 0f;

            // STEP 7: Check if the direction is valid to avoid errors if aiming straight down at yourself
            if (targetDirection.sqrMagnitude > 0.001f)
            {
                // Turn our flat direction vector into a target rotation state
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

                // Smoothly rotate from our current rotation to the target rotation over time
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
            }
        }
    }

    private void ConsumeStamina(float amount)
    {
        _currentStamina -= amount;
        _currentStamina = Mathf.Clamp(_currentStamina, 0f, _maxStamina);

        _staminaRegenTimer = _staminaRegenDelay;
    }

    private void HandleStaminaRegeneration()
    {
        if (!_useStamina)
            return;

        if (_staminaRegenTimer > 0f)
        {
            _staminaRegenTimer -= Time.deltaTime;
            return;
        }

        float regenRate = _movementState == PlayerMovementState.Crouching
            ? _crouchStaminaRegenRate
            : _staminaRegenRate;

        _currentStamina += regenRate * Time.deltaTime;
        _currentStamina = Mathf.Clamp(_currentStamina, 0f, _maxStamina);
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
