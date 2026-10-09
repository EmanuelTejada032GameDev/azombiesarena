using UnityEngine;

public class PlayerAnimationHandler : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    [Header("Blend Tree Rings")]
    [SerializeField] private float _walkRing = 0.5f;
    [SerializeField] private float _sprintRing = 1f;

    [Header("Smoothing")]
    [SerializeField] private float _dampTime = 0.1f;
    [SerializeField] private float _minSpeed = 0.1f;

    private static readonly int XDirHash = Animator.StringToHash("XDir");
    private static readonly int ZDirHash = Animator.StringToHash("ZDir");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
    private static readonly int IsSlidingHash = Animator.StringToHash("IsSliding");

    private const float RecoilLayerActiveDuration = 0.8f;

    private int _recoilLayerIndex;
    private float _recoilLayerEndTime;
    private static readonly int ShootHash = Animator.StringToHash("Shoot");

    private CharacterController _controller;
    private Transform _root;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        _controller = GetComponentInParent<CharacterController>();
        _root = _controller.transform;
    }

    private void Start()
    {
        _recoilLayerIndex = _animator.GetLayerIndex("Recoil");
    }

    private void OnEnable()
    {
        Weapon.OnShoot += HandleShoot;
    }

    private void OnDisable()
    {
        Weapon.OnShoot -= HandleShoot;
    }

    private void HandleShoot(float recoilForce)
    {
        if (_recoilLayerIndex >= 0)
        {
            _animator.SetLayerWeight(_recoilLayerIndex, Mathf.Clamp01(recoilForce));
            _recoilLayerEndTime = Time.time + RecoilLayerActiveDuration;
        }

        _animator.SetTrigger(ShootHash);
    }

    private void Update()
    {
        if (_recoilLayerIndex >= 0 &&
            _recoilLayerEndTime > 0f &&
            Time.time >= _recoilLayerEndTime)
        {
            _animator.SetLayerWeight(_recoilLayerIndex, 0f);
            _recoilLayerEndTime = 0f;
        }

        PlayerMovement movement = PlayerMovement.Instance;

        if (movement == null)
            return;

        PlayerMovement.LocomotionState state = movement.GetLocomotionState();

        bool isSliding =
            movement.GetManeuverState() == PlayerMovement.ManeuverState.Sliding;

        bool isSprinting =
            state == PlayerMovement.LocomotionState.Sprinting &&
            !isSliding;

        Vector3 velocity = _controller.velocity;
        velocity.y = 0f;

        Vector2 target = Vector2.zero;

        if (state != PlayerMovement.LocomotionState.Idle &&
            velocity.magnitude > _minSpeed)
        {
            Vector3 local = _root.InverseTransformDirection(velocity.normalized);

            float ring =
                state == PlayerMovement.LocomotionState.Sprinting
                    ? _sprintRing
                    : _walkRing;

            target = new Vector2(local.x, local.z) * ring;
        }

        _animator.SetFloat(XDirHash, target.x, _dampTime, Time.deltaTime);
        _animator.SetFloat(ZDirHash, target.y, _dampTime, Time.deltaTime);
        _animator.SetBool(IsSprintingHash, isSprinting);
        _animator.SetBool(IsSlidingHash, isSliding);
    }
}