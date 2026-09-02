using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour, IHasProgress
{
    public static PlayerMovement Instance { get; private set; }

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
    [SerializeField] private float _defaultGravity = 9.81f;
    [SerializeField] private float _jumpStaminaCost = 10f;

    private float _verticalVelocity;

    [Header("Locomotion State")]
    [SerializeField] private LocomotionState _locomotionState;
    public LocomotionState GetLocomotionState() => _locomotionState;


    public enum LocomotionState
    {
        Idle,
        Walking,
        Sprinting,
        Crouching
    }

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 15f;

    [Header("Crouch Settings")]
    [SerializeField] private bool _canCrouch = false;
    [SerializeField] private float _crouchTransitionSpeed = 10f;

    private float _standingHeight;
    private float _crouchingHeight;

    private Vector3 _standingCenter;
    private Vector3 _crouchingCenter;

    [Header("Maneuver")]
    [SerializeField] private ManeuverState _maneuverState;
    public ManeuverState GetManeuverState() => _maneuverState;

    public enum ManeuverState
    {
        None,
        Sliding
    }

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

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
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
                _standingCenter.y -
                (_standingHeight - _crouchingHeight) * 0.5f,
                _standingCenter.z
            );
        }

        _inputs = Player.Instance.GetInputInstance();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
            return;

        if (_slideCooldownTimer > 0f)
            _slideCooldownTimer -= Time.deltaTime;

        Vector3 moveDirection = CalculateMoveDirection();

        HandleManeuverInput(moveDirection);

        HandleMovement(moveDirection);

        HandleRotation(moveDirection);
    }

    private Vector3 CalculateMoveDirection()
    {
        Vector2 inputVector =
            _inputs.Player.Move.ReadValue<Vector2>();

        Vector3 cameraForward = _mainCamera.forward;
        Vector3 cameraRight = _mainCamera.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        return
            (cameraForward * inputVector.y) +
            (cameraRight * inputVector.x);
    }

    private void HandleManeuverInput(Vector3 moveDirection)
    {
        if (_maneuverState != ManeuverState.None)
            return;

        bool isCrouchPressed =
            _inputs.Player.Crouch.IsPressed();

        bool isSprintPressed =
            _inputs.Player.Sprint.IsPressed();

        bool wantsToSlide =
            isCrouchPressed &&
            isSprintPressed;

        if (!wantsToSlide)
            return;

        if (!_canSlide)
            return;

        if (!_characterController.isGrounded)
            return;

        if (_slideCooldownTimer > 0f)
            return;

        if (_locomotionState != LocomotionState.Sprinting)
            return;

        if (_useStamina &&
            _currentStamina < _slideStaminaCost)
            return;

        StartSlide(moveDirection);
    }

    private void HandleMovement(Vector3 moveDirection)
    {
        Vector2 inputVector =
            _inputs.Player.Move.ReadValue<Vector2>();

        if (_maneuverState == ManeuverState.Sliding)
        {
            HandleSlide();

            ApplySlidePosture();

            ApplySlideMovement();

            HandleStaminaRegeneration();

            return;
        }

        HandleVerticalMovement();

        bool isCeilingAbove = CheckCeiling();

        bool isCrouchPressed =
            _inputs.Player.Crouch.IsPressed();

        bool shouldBeCrouched =
            isCrouchPressed ||
            isCeilingAbove;

        UpdateLocomotionState(
            inputVector,
            shouldBeCrouched
        );

        HandleCrouchPosture(
            shouldBeCrouched
        );

        if (_useStamina &&
            _locomotionState == LocomotionState.Sprinting)
        {
            ConsumeStamina(
                _sprintStaminaDrain *
                Time.deltaTime
            );
        }

        HandleStaminaRegeneration();

        Vector3 movement =
            moveDirection *
            _currentPlayerSpeed;

        movement.y = _verticalVelocity;

        _characterController.Move(
            movement *
            Time.deltaTime
        );
    }

    private void UpdateLocomotionState(
        Vector2 inputVector,
        bool shouldBeCrouched)
    {
        bool isSprintPressed =
            _inputs.Player.Sprint.IsPressed();

        if (shouldBeCrouched && _canCrouch)
        {
            _locomotionState =
                LocomotionState.Crouching;

            _currentPlayerSpeed =
                _crouchPlayerSpeed;

            return;
        }

        if (isSprintPressed &&
            _canSprint &&
            inputVector.sqrMagnitude > 0.01f &&
            (!_useStamina || _currentStamina > 0f))
        {
            _locomotionState =
                LocomotionState.Sprinting;

            _currentPlayerSpeed =
                _sprintPlayerSpeed;

            return;
        }

        if (inputVector.sqrMagnitude > 0.01f)
        {
            _locomotionState =
                LocomotionState.Walking;

            _currentPlayerSpeed =
                _walkPlayerSpeed;

            return;
        }

        _locomotionState =
            LocomotionState.Idle;

        _currentPlayerSpeed = 0f;
    }

    private void HandleVerticalMovement()
    {
        if (_characterController.isGrounded)
        {
            _verticalVelocity = -2f;

            if (_inputs.Player.Jump.WasPressedThisFrame() &&
                _canJump)
            {
                if (!_useStamina ||
                    _currentStamina >= _jumpStaminaCost)
                {
                    if (_useStamina)
                        ConsumeStamina(_jumpStaminaCost);

                    _verticalVelocity = 5.5f;
                }
            }
        }
        else
        {
            _verticalVelocity -=
                _defaultGravity *
                Time.deltaTime;
        }
    }

    private bool CheckCeiling()
    {
        float distanceToStandingTop =
            _standingHeight * 0.5f;

        _lastCeilingCheckPos =
            transform.position +
            Vector3.up *
            (distanceToStandingTop -
             _ceilingCheckRadius);

        int playerLayerIndex =
            LayerMask.NameToLayer("Player");

        int crossWorldCeilingMask =
            ~(1 << playerLayerIndex);

        return Physics.CheckSphere(
            _lastCeilingCheckPos,
            _ceilingCheckRadius,
            crossWorldCeilingMask
        );
    }

    private void HandleCrouchPosture(
        bool shouldBeCrouched)
    {
        bool isActuallyCrouching =
            _locomotionState ==
            LocomotionState.Crouching;

        float targetHeight =
            _standingHeight;

        Vector3 targetCenter =
            _standingCenter;

        Vector3 targetVisualScale =
            Vector3.one;

        Vector3 targetVisualPosition =
            Vector3.zero;

        if (isActuallyCrouching)
        {
            targetHeight =
                _crouchingHeight;

            targetCenter =
                _crouchingCenter;

            targetVisualScale =
                new Vector3(
                    1f,
                    0.5f,
                    1f
                );

            targetVisualPosition =
                new Vector3(
                    0f,
                    -0.5f,
                    0f
                );
        }

        ApplyPosture(
            targetHeight,
            targetCenter,
            targetVisualScale,
            targetVisualPosition
        );
    }

    private void ApplySlidePosture()
    {
        float targetHeight =
            _crouchingHeight;

        Vector3 targetCenter =
            _crouchingCenter;

        Vector3 targetVisualScale =
            new Vector3(
                1f,
                0.5f,
                1f
            );

        Vector3 targetVisualPosition =
            new Vector3(
                0f,
                -0.5f,
                0f
            );

        ApplyPosture(
            targetHeight,
            targetCenter,
            targetVisualScale,
            targetVisualPosition
        );
    }

    private void ApplyPosture(
        float targetHeight,
        Vector3 targetCenter,
        Vector3 targetVisualScale,
        Vector3 targetVisualPosition)
    {
        _characterController.height =
            Mathf.Lerp(
                _characterController.height,
                targetHeight,
                Time.deltaTime *
                _crouchTransitionSpeed
            );

        _characterController.center =
            Vector3.Lerp(
                _characterController.center,
                targetCenter,
                Time.deltaTime *
                _crouchTransitionSpeed
            );

        if (_playerVisual != null)
        {
            _playerVisual.localScale =
                Vector3.Lerp(
                    _playerVisual.localScale,
                    targetVisualScale,
                    Time.deltaTime *
                    _crouchTransitionSpeed
                );

            _playerVisual.localPosition =
                Vector3.Lerp(
                    _playerVisual.localPosition,
                    targetVisualPosition,
                    Time.deltaTime *
                    _crouchTransitionSpeed
                );
        }
    }

    private void HandleRotation(Vector3 moveDirection)
    {
        Vector3 targetDirection;

        if (_maneuverState ==
            ManeuverState.Sliding)
        {
            targetDirection =
                _slideDirection;
        }
        else if (_locomotionState ==
                 LocomotionState.Sprinting)
        {
            targetDirection =
                moveDirection;
        }
        else
        {
            Ray ray =
                Camera.main.ScreenPointToRay(
                    Input.mousePosition
                );

            float weaponAimHeight =
                _weaponHoldAnchor != null
                    ? _weaponHoldAnchor.position.y
                    : 1.2f;

            Plane aimingPlane =
                new Plane(
                    Vector3.up,
                    new Vector3(
                        0f,
                        weaponAimHeight,
                        0f
                    )
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
                worldHitPoint -
                transform.position;

            targetDirection.y = 0f;
        }

        if (targetDirection.sqrMagnitude >
            0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    targetDirection
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime *
                    _rotationSpeed
                );
        }
    }

    private void StartSlide(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        _maneuverState =
            ManeuverState.Sliding;

        _slideDirection =
            moveDirection.normalized;

        _slideTimer =
            _slideDuration;

        _slideCooldownTimer =
            _slideCooldown;

        if (_useStamina)
        {
            ConsumeStamina(
                _slideStaminaCost
            );
        }
    }

    private void HandleSlide()
    {
        _slideTimer -=
            Time.deltaTime;

        if (_slideTimer <= 0f)
        {
            EndSlide();
        }
    }

    private void ApplySlideMovement()
    {
        Vector3 movement =
            _slideDirection *
            _slideSpeed;

        movement.y =
            _verticalVelocity;

        _characterController.Move(
            movement *
            Time.deltaTime
        );
    }

    private void EndSlide()
    {
        _maneuverState =
            ManeuverState.None;

        _slideTimer = 0f;
    }

    private void ConsumeStamina(float amount)
    {
        _currentStamina -= amount;

        _currentStamina =
            Mathf.Clamp(
                _currentStamina,
                0f,
                _maxStamina
            );

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = _currentStamina / _maxStamina
        });

        _staminaRegenTimer =
            _staminaRegenDelay;
    }

    private void HandleStaminaRegeneration()
    {
        if (!_useStamina)
            return;

        if (_staminaRegenTimer > 0f)
        {
            _staminaRegenTimer -=
                Time.deltaTime;

            return;
        }

        float regenRate =
            _locomotionState ==
            LocomotionState.Crouching
                ? _crouchStaminaRegenRate
                : _staminaRegenRate;

        _currentStamina +=
            regenRate *
            Time.deltaTime;

        _currentStamina =
            Mathf.Clamp(
                _currentStamina,
                0f,
                _maxStamina
            );


        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = _currentStamina / _maxStamina
        });
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
