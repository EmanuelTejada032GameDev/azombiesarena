using UnityEngine;

public class PlayerHandIK : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _rightHandTarget;
    [SerializeField] private Transform _weaponSocket;
    [SerializeField] private string _twoHandedLayer = "UpperBody";
    [SerializeField] private string _oneHandedLayer = "RightArm";
    [SerializeField] private float _blendSpeed = 10f;
    [SerializeField] private float _layerBlendSpeed = 8f;

    [Header("Procedural Recoil")]
    [SerializeField] private float _recoilDuration = 0.18f;
    [SerializeField] private float _recoilBackOffset = 0.06f;
    [SerializeField] private float _recoilUpOffset = 0.015f;
    [SerializeField] private float _recoilPitch = 4f;

    private int _twoHandedIndex;
    private int _oneHandedIndex;
    private int _finalIkLayerIndex;
    private Weapon _currentWeapon;
    private Transform _leftHandGrip;
    private float _weight;
    private bool _isAiming;
    private float _recoilStartTime = -1f;
    private float _recoilStrength;
    private Vector3 _rightHandBaseLocalPosition;
    private Quaternion _rightHandBaseLocalRotation;

    private bool IsOneHanded => _currentWeapon != null && _leftHandGrip == null;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        _twoHandedIndex = _animator.GetLayerIndex(_twoHandedLayer);
        _oneHandedIndex = _animator.GetLayerIndex(_oneHandedLayer);
        _finalIkLayerIndex = _animator.GetLayerIndex("Recoil");
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
        _recoilStrength = Mathf.Clamp01(recoilForce);
        _recoilStartTime = Time.time;
    }

    private void Update()
    {
        PlayerMovement movement = PlayerMovement.Instance;

        if (movement == null)
            return;

        RefreshWeaponTargets();
        UpdateRecoilTarget();

        _isAiming =
            movement.GetLocomotionState() != PlayerMovement.LocomotionState.Sprinting &&
            movement.GetManeuverState() == PlayerMovement.ManeuverState.None;

        _weight = Mathf.MoveTowards(_weight, _isAiming ? 1f : 0f, _blendSpeed * Time.deltaTime);

        float step = _layerBlendSpeed * Time.deltaTime;

        BlendLayer(_twoHandedIndex, IsOneHanded ? 0f : 1f, step);
        BlendLayer(_oneHandedIndex, IsOneHanded ? 1f : 0f, step);
    }

    private void BlendLayer(int index, float target, float step)
    {
        float current = _animator.GetLayerWeight(index);
        _animator.SetLayerWeight(index, Mathf.MoveTowards(current, target, step));
    }

    private void RefreshWeaponTargets()
    {
        Weapon weapon = _weaponSocket.GetComponentInChildren<Weapon>();

        if (weapon == _currentWeapon)
            return;

        _currentWeapon = weapon;
        _leftHandGrip = null;

        if (weapon == null)
            return;

        foreach (Transform child in weapon.GetComponentsInChildren<Transform>())
        {
            if (child.name == "LeftHandGrip")
            {
                _leftHandGrip = child;
            }
            else if (child.name == "RightHandAim")
            {
                _rightHandTarget.localPosition = child.localPosition;
                _rightHandTarget.localRotation = child.localRotation;
                _rightHandBaseLocalPosition = _rightHandTarget.localPosition;
                _rightHandBaseLocalRotation = _rightHandTarget.localRotation;
                _recoilStartTime = -1f;
            }
        }
    }

    private void UpdateRecoilTarget()
    {
        if (_rightHandTarget == null || _recoilStartTime < 0f)
            return;

        float normalizedTime = Mathf.Clamp01((Time.time - _recoilStartTime) / Mathf.Max(_recoilDuration, 0.01f));
        float remaining = 1f - normalizedTime;
        float recoil = remaining * remaining * (3f - 2f * remaining) * _recoilStrength;

        _rightHandTarget.localPosition = _rightHandBaseLocalPosition +
            new Vector3(0f, _recoilUpOffset * recoil, -_recoilBackOffset * recoil);
        _rightHandTarget.localRotation = _rightHandBaseLocalRotation *
            Quaternion.Euler(-_recoilPitch * recoil, 0f, 0f);

        if (normalizedTime >= 1f)
        {
            _rightHandTarget.localPosition = _rightHandBaseLocalPosition;
            _rightHandTarget.localRotation = _rightHandBaseLocalRotation;
            _recoilStartTime = -1f;
            _recoilStrength = 0f;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (layerIndex != _finalIkLayerIndex)
            return;

        ApplyHand(AvatarIKGoal.RightHand, _rightHandTarget, _weight);
        ApplyHand(
            AvatarIKGoal.LeftHand,
            _leftHandGrip,
            _isAiming && _leftHandGrip != null ? 1f : 0f);
    }

    private void ApplyHand(AvatarIKGoal goal, Transform target, float requestedWeight)
    {
        float weight = target != null ? requestedWeight : 0f;

        _animator.SetIKPositionWeight(goal, weight);
        _animator.SetIKRotationWeight(goal, weight);

        if (target == null)
            return;

        _animator.SetIKPosition(goal, target.position);
        _animator.SetIKRotation(goal, target.rotation);
    }
}