using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour, IHasProgress
{
    public static PlayerMovement Instance { get; private set; }

    private PlayerInput _inputs;
    private Transform _mainCamera;
    private Vector3 _aimPoint;


    public Vector3 AimPoint => _aimPoint;
    public bool HasAimPoint { get; private set; }
    public bool IsAimDeadZone { get; private set; }
    public bool IsSprinting =>
        _locomotionState == LocomotionState.Sprinting &&
        _maneuverState != ManeuverState.Sliding;

    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _weaponHoldAnchor;
    private Weapon _currentWeapon;

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
    [SerializeField] private float _aimRotationSpeed = 200f;
    [SerializeField] private bool _snapAimRotation = true;
    [SerializeField, Min(0f)] private float _aimDistanceMargin = 0.5f;
    [SerializeField, Min(0f)] private float _fallbackMinimumAimDistance = 1.5f;

    private float _currentMinimumAimDistance;

    public float CurrentMinimumAimDistance =>
        _currentMinimumAimDistance > 0f
            ? _currentMinimumAimDistance
            : _fallbackMinimumAimDistance;


    [SerializeField] private bool _showAimGizmos = true;

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

    private float EffectiveMaxStamina =>
    PlayerStats.Get(PlayerStats.StatType.MaxStamina, _maxStamina);

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

        _currentStamina = EffectiveMaxStamina;

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

        PlayerWeaponHandler weaponHandler = PlayerWeaponHandler.Instance;
        if (weaponHandler != null)
        {
            weaponHandler.OnWeaponSwapped += HandleWeaponSwapped;
            MeasureMinimumAimDistance(weaponHandler.ActiveWeapon);
        }
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

        UpdateAimPoint();
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

            _currentPlayerSpeed = PlayerStats.Get(PlayerStats.StatType.MoveSpeed, _crouchPlayerSpeed);

            return;
        }

        if (isSprintPressed &&
            _canSprint &&
            inputVector.sqrMagnitude > 0.01f &&
            (!_useStamina || _currentStamina > 0f))
        {
            _locomotionState =
                LocomotionState.Sprinting;

            _currentPlayerSpeed = PlayerStats.Get(PlayerStats.StatType.MoveSpeed, _sprintPlayerSpeed);

            return;
        }

        if (inputVector.sqrMagnitude > 0.01f)
        {
            _locomotionState =
                LocomotionState.Walking;

            _currentPlayerSpeed = PlayerStats.Get(PlayerStats.StatType.MoveSpeed, _walkPlayerSpeed);

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

        if (isActuallyCrouching)
        {
            targetHeight =
                _crouchingHeight;

            targetCenter =
                _crouchingCenter;
        }

        ApplyPosture(
            targetHeight,
            targetCenter
        );
    }

    private void ApplySlidePosture()
    {
        ApplyPosture(
            _crouchingHeight,
            _crouchingCenter
        );
    }


    private void ApplyPosture(
        float targetHeight,
        Vector3 targetCenter)
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
    }

    private void UpdateAimPoint()
    {
        IsAimDeadZone = false;

        if (IsSprinting || _maneuverState == ManeuverState.Sliding)
        {
            HasAimPoint = false;
            return;
        }

        Camera aimCamera = Camera.main;
        if (aimCamera == null)
        {
            HasAimPoint = false;
            return;
        }

        Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
        float aimHeight = _weaponHoldAnchor != null
            ? _weaponHoldAnchor.position.y
            : transform.position.y + 1.2f;
        Plane aimingPlane = new Plane(Vector3.up, new Vector3(0f, aimHeight, 0f));

        if (!aimingPlane.Raycast(ray, out float enterDistance) || enterDistance < 0f)
        {
            HasAimPoint = false;
            return;
        }

        Vector3 center = new Vector3(transform.position.x, aimHeight, transform.position.z);
        Vector3 planarOffset = ray.GetPoint(enterDistance) - center;
        planarOffset.y = 0f;

        float minimumDistance = CurrentMinimumAimDistance;
        float distance = planarOffset.magnitude;
        IsAimDeadZone = distance < minimumDistance;

        Vector3 direction;

        if (IsAimDeadZone)
        {
            direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        }
        else
        {
            direction = planarOffset / distance;
        }

        _aimPoint = center + direction * Mathf.Max(distance, minimumDistance);
        HasAimPoint = true;
    }

    private const int MuzzleAlignmentIterations = 4;
    private const float MuzzleAlignmentFadeDistance = 1.5f;
    private const float MaxMuzzleAlignmentAngle = 35f;

    /// <summary>
    /// Returns the planar body facing direction that makes the weapon muzzle point at the aim point,
    /// so the whole body turns with the gun instead of only the gun swinging to the cursor.
    /// </summary>
    private Vector3 CalculateMuzzleAlignedBodyDirection()
    {
        Vector3 bodyToAim = _aimPoint - transform.position;
        bodyToAim.y = 0f;

        if (_currentWeapon == null || _currentWeapon.Muzzle == null || bodyToAim.sqrMagnitude < 0.001f)
            return bodyToAim;

        Vector3 muzzleLocalOffset = transform.InverseTransformPoint(_currentWeapon.Muzzle.position);
        muzzleLocalOffset.y = 0f;

        Vector3 solvedDirection = bodyToAim;

        for (int i = 0; i < MuzzleAlignmentIterations; i++)
        {
            Quaternion candidateRotation = Quaternion.LookRotation(solvedDirection);
            Vector3 muzzlePosition = transform.position + candidateRotation * muzzleLocalOffset;
            Vector3 muzzleToAim = _aimPoint - muzzlePosition;
            muzzleToAim.y = 0f;

            if (muzzleToAim.sqrMagnitude < 0.001f)
                break;

            solvedDirection = muzzleToAim;
        }

        // Fade the muzzle solve in as the cursor moves away from the dead zone edge.
        float blend = Mathf.InverseLerp(
            CurrentMinimumAimDistance,
            CurrentMinimumAimDistance + MuzzleAlignmentFadeDistance,
            bodyToAim.magnitude);

        float solvedOffsetAngle = Vector3.SignedAngle(bodyToAim, solvedDirection, Vector3.up);
        float limitedOffsetAngle = Mathf.Clamp(
            solvedOffsetAngle * blend,
            -MaxMuzzleAlignmentAngle,
            MaxMuzzleAlignmentAngle);

        return Quaternion.AngleAxis(limitedOffsetAngle, Vector3.up) * bodyToAim;
    }

    private void HandleRotation(Vector3 moveDirection)
    {
        Vector3 targetDirection;
        bool useTravelTurn;

        if (_maneuverState == ManeuverState.Sliding)
        {
            targetDirection = _slideDirection;
            useTravelTurn = true;
        }
        else if (IsSprinting)
        {
            targetDirection = moveDirection;
            useTravelTurn = true;
        }
        else if (HasAimPoint)
        {
            // Inside the dead zone there is no valid target, so hold the current facing.
            if (IsAimDeadZone)
                return;

            targetDirection = CalculateMuzzleAlignedBodyDirection();
            useTravelTurn = false;
        }
        else
        {
            return;
        }

        if (targetDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        if (useTravelTurn)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * _rotationSpeed
            );
        }
        else
        {
            transform.rotation = _snapAimRotation
                ? targetRotation
                : Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * _aimRotationSpeed
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

        Vector3 movement = _slideDirection * (_currentPlayerSpeed * 1.5f);

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

    public bool TrySpendStamina(float amount)
    {
        if (!_useStamina)
            return true;

        if (_currentStamina < amount)
            return false;

        ConsumeStamina(amount);
        return true;
    }


    private void ConsumeStamina(float amount)
    {
        _currentStamina -= amount;

        _currentStamina =
            Mathf.Clamp(
                _currentStamina,
                0f,
                EffectiveMaxStamina
            );

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = _currentStamina / EffectiveMaxStamina
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

        regenRate = PlayerStats.Get(PlayerStats.StatType.StaminaRegen, regenRate);

        _currentStamina +=
            regenRate *
            Time.deltaTime;

        _currentStamina =
            Mathf.Clamp(
                _currentStamina,
                0f,
                EffectiveMaxStamina
            );


        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = _currentStamina / EffectiveMaxStamina
        });
    }


    private void OnDestroy()
    {
        if (PlayerWeaponHandler.Instance != null)
            PlayerWeaponHandler.Instance.OnWeaponSwapped -= HandleWeaponSwapped;
    }

    private void HandleWeaponSwapped(Weapon weapon)
    {
        MeasureMinimumAimDistance(weapon);
    }

    private void MeasureMinimumAimDistance(Weapon weapon)
    {
        _currentWeapon = weapon;

        if (weapon == null || weapon.Muzzle == null)
        {
            _currentMinimumAimDistance = 0f;
            return;
        }

        Vector3 offset = weapon.Muzzle.position - transform.position;
        offset.y = 0f;
        _currentMinimumAimDistance = offset.magnitude + _aimDistanceMargin;
    }


    //private void OnDrawGizmos()
    //{
    //    if (!_showAimGizmos)
    //        return;

    //    float height = _weaponHoldAnchor != null
    //        ? _weaponHoldAnchor.position.y
    //        : transform.position.y + 1.2f;

    //    float radius = CurrentMinimumAimDistance;
    //    Vector3 center = new Vector3(transform.position.x, height, transform.position.z);

    //    Gizmos.color = Color.yellow;
    //    const int segments = 48;
    //    Vector3 previous = center + new Vector3(radius, 0f, 0f);
    //    for (int i = 1; i <= segments; i++)
    //    {
    //        float angle = i * Mathf.PI * 2f / segments;
    //        Vector3 next = center + new Vector3(
    //            Mathf.Cos(angle) * radius,
    //            0f,
    //            Mathf.Sin(angle) * radius
    //        );
    //        Gizmos.DrawLine(previous, next);
    //        previous = next;
    //    }

    //    if (Application.isPlaying && HasAimPoint)
    //    {
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawLine(center, _aimPoint);
    //        Gizmos.DrawWireSphere(_aimPoint, 0.1f);
    //    }
    //}

    //private void OnDrawGizmos()
    //{
    //    if (!Application.isPlaying)
    //        return;

    //    Gizmos.color = Color.red;

    //    Gizmos.DrawWireSphere(
    //        _lastCeilingCheckPos,
    //        _ceilingCheckRadius
    //    );
    //}
}
