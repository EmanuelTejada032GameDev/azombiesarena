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
    [SerializeField] private float _jumpStaminaCost = 10f;

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
    private Vector3 _crouchingCenter = new Vector3(0f, -0.5f, 0f);


    [Header("Slide Settings")]
    [SerializeField] private bool _canSlide = true;
    [SerializeField] private float _slideSpeed = 10f;
    [SerializeField] private float _slideDuration = 0.7f;
    [SerializeField] private float _slideCooldown = 1f;
    [SerializeField] private float _slideStaminaCost = 20f;

    private float _slideTimer;
    private float _slideCooldownTimer;
    private Vector3 _slideDirection;


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
        Crouching,
        Sliding
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

            _crouchingHeight = _standingHeight * 0.5f;

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
        if (GameManager.Instance.State == GameState.Playing)
        {
            Vector3 moveDirection = HandleMovement();

            HandleRotation(moveDirection);

            if (_slideCooldownTimer > 0f)
            {
                _slideCooldownTimer -= Time.deltaTime;
            }
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

        Vector3 moveDirection =
            (cameraForward * inputVector.y) +
            (cameraRight * inputVector.x);


        bool isCrouchPressed = _inputs.Player.Crouch.IsPressed();
        bool isSprintPressed = _inputs.Player.Sprint.IsPressed();

        bool wantsToSlide = isCrouchPressed && isSprintPressed;


        // --------------------------------------------------
        // SLIDE START
        // --------------------------------------------------

        if (_movementState == PlayerMovementState.Sprinting &&
            wantsToSlide &&
            _canSlide &&
            _characterController.isGrounded &&
            _slideCooldownTimer <= 0f &&
            (!_useStamina || _currentStamina >= _slideStaminaCost))
        {
            StartSlide(moveDirection);
        }


        // --------------------------------------------------
        // VERTICAL MOVEMENT
        // --------------------------------------------------

        if (_characterController.isGrounded)
        {
            _verticalVelocity = -2f;

            if (_inputs.Player.Jump.WasPressedThisFrame() && _canJump)
            {
                if (!_useStamina || _currentStamina >= _jumpStaminaCost)
                {
                    if (_useStamina)
                    {
                        ConsumeStamina(_jumpStaminaCost);
                    }

                    _verticalVelocity = 5.5f;
                }
            }
        }
        else
        {
            _verticalVelocity -= defaultGravity * Time.deltaTime;
        }


        // --------------------------------------------------
        // CEILING DETECTION
        // --------------------------------------------------

        float distanceToStandingTop = _standingHeight * 0.5f;

        _lastCeilingCheckPos =
            transform.position +
            (Vector3.up * (distanceToStandingTop - _ceilingCheckRadius));

        int playerLayerIndex = LayerMask.NameToLayer("Player");

        int crossWorldCeilingMask = ~(1 << playerLayerIndex);

        bool isCeilingAbove = Physics.CheckSphere(
            _lastCeilingCheckPos,
            _ceilingCheckRadius,
            crossWorldCeilingMask
        );


        bool shouldBeCrouched =
            isCrouchPressed ||
            isCeilingAbove;


        // --------------------------------------------------
        // CHARACTER CONTROLLER TARGET
        // --------------------------------------------------

        float targetHeight = _standingHeight;
        Vector3 targetCenter = _standingCenter;

        Vector3 targetVisualScale = new Vector3(1f, 1f, 1f);
        Vector3 targetVisualPosition = Vector3.zero;


        // --------------------------------------------------
        // MOVEMENT STATE
        // --------------------------------------------------

        if (_movementState == PlayerMovementState.Sliding)
        {
            // While sliding, don't allow the normal movement
            // state logic to overwrite the Sliding state.

            HandleSlide();

            _currentPlayerSpeed = _slideSpeed;

            targetHeight = _crouchingHeight;
            targetCenter = _crouchingCenter;

            targetVisualScale = new Vector3(1f, 0.5f, 1f);
            targetVisualPosition = new Vector3(0f, -0.5f, 0f);
        }
        else if (shouldBeCrouched && _canCrouch)
        {
            _movementState = PlayerMovementState.Crouching;

            _currentPlayerSpeed = _crouchPlayerSpeed;

            targetHeight = _crouchingHeight;
            targetCenter = _crouchingCenter;

            targetVisualScale = new Vector3(1f, 0.5f, 1f);
            targetVisualPosition = new Vector3(0f, -0.5f, 0f);
        }
        else if (isSprintPressed &&
                 _canSprint &&
                 inputVector.sqrMagnitude > 0.01f &&
                 (!_useStamina || _currentStamina > 0f))
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


        // --------------------------------------------------
        // CROUCH TRANSITION
        // --------------------------------------------------

        _characterController.height = Mathf.Lerp(
            _characterController.height,
            targetHeight,
            Time.deltaTime * _crouchTransitionSpeed
        );

        _characterController.center = Vector3.Lerp(
            _characterController.center,
            targetCenter,
            Time.deltaTime * _crouchTransitionSpeed
        );


        if (_playerVisual != null)
        {
            _playerVisual.localScale = Vector3.Lerp(
                _playerVisual.localScale,
                targetVisualScale,
                Time.deltaTime * _crouchTransitionSpeed
            );

            _playerVisual.localPosition = Vector3.Lerp(
                _playerVisual.localPosition,
                targetVisualPosition,
                Time.deltaTime * _crouchTransitionSpeed
            );
        }


        // --------------------------------------------------
        // MOVEMENT VECTOR
        // --------------------------------------------------

        Vector3 finalMovementVector;

        if (_movementState == PlayerMovementState.Sliding)
        {
            finalMovementVector = _slideDirection * _slideSpeed;
        }
        else
        {
            finalMovementVector = moveDirection * _currentPlayerSpeed;
        }

        finalMovementVector.y = _verticalVelocity;


        // --------------------------------------------------
        // STAMINA
        // --------------------------------------------------

        if (_useStamina &&
            _movementState == PlayerMovementState.Sprinting)
        {
            ConsumeStamina(
                _sprintStaminaDrain * Time.deltaTime
            );
        }

        HandleStaminaRegeneration();


        // --------------------------------------------------
        // MOVE
        // --------------------------------------------------

        _characterController.Move(
            finalMovementVector * Time.deltaTime
        );


        return moveDirection;
    }


    private void HandleRotation(Vector3 moveDirection)
    {
        Vector3 targetDirection;

        if (_movementState == PlayerMovementState.Sliding)
        {
            targetDirection = _slideDirection;
        }
        else if (_movementState == PlayerMovementState.Sprinting)
        {
            targetDirection = moveDirection;
        }
        else
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float weaponAimHeight =
                _weaponHoldAnchor != null
                    ? _weaponHoldAnchor.position.y
                    : 1.2f;

            Plane aimingPlane = new Plane(
                Vector3.up,
                new Vector3(0f, weaponAimHeight, 0f)
            );

            if (!aimingPlane.Raycast(
                ray,
                out float enterDistance))
            {
                return;
            }

            Vector3 worldHitPoint =
                ray.GetPoint(enterDistance);

            targetDirection =
                worldHitPoint - transform.position;

            targetDirection.y = 0f;
        }


        if (targetDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(targetDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * _rotationSpeed
            );
        }
    }


    private void ConsumeStamina(float amount)
    {
        _currentStamina -= amount;

        _currentStamina = Mathf.Clamp(
            _currentStamina,
            0f,
            _maxStamina
        );

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


        float regenRate =
            _movementState == PlayerMovementState.Crouching
                ? _crouchStaminaRegenRate
                : _staminaRegenRate;


        _currentStamina +=
            regenRate * Time.deltaTime;

        _currentStamina = Mathf.Clamp(
            _currentStamina,
            0f,
            _maxStamina
        );
    }


    private void StartSlide(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.001f)
            return;


        _movementState = PlayerMovementState.Sliding;

        _slideDirection = moveDirection.normalized;

        _slideTimer = _slideDuration;

        _slideCooldownTimer = _slideCooldown;


        if (_useStamina)
        {
            ConsumeStamina(_slideStaminaCost);
        }
    }


    private void HandleSlide()
    {
        _slideTimer -= Time.deltaTime;


        if (_slideTimer <= 0f)
        {
            _movementState = PlayerMovementState.Crouching;
        }
    }


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            _lastCeilingCheckPos,
            _ceilingCheckRadius
        );
    }
}