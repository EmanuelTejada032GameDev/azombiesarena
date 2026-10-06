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

    private int _twoHandedIndex;
    private int _oneHandedIndex;
    private Weapon _currentWeapon;
    private Transform _leftHandGrip;
    private float _weight;

    private bool IsOneHanded => _currentWeapon != null && _leftHandGrip == null;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        _twoHandedIndex = _animator.GetLayerIndex(_twoHandedLayer);
        _oneHandedIndex = _animator.GetLayerIndex(_oneHandedLayer);
    }

    private void Update()
    {
        PlayerMovement movement = PlayerMovement.Instance;

        if (movement == null)
            return;

        RefreshWeaponTargets();

        bool aiming =
            movement.GetLocomotionState() != PlayerMovement.LocomotionState.Sprinting &&
            movement.GetManeuverState() == PlayerMovement.ManeuverState.None;

        _weight = Mathf.MoveTowards(_weight, aiming ? 1f : 0f, _blendSpeed * Time.deltaTime);

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
            }
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        int activeLayer = IsOneHanded ? _oneHandedIndex : _twoHandedIndex;

        if (layerIndex != activeLayer)
            return;

        ApplyHand(AvatarIKGoal.RightHand, _rightHandTarget);
        ApplyHand(AvatarIKGoal.LeftHand, _leftHandGrip);
    }

    private void ApplyHand(AvatarIKGoal goal, Transform target)
    {
        float weight = target != null ? _weight : 0f;

        _animator.SetIKPositionWeight(goal, weight);
        _animator.SetIKRotationWeight(goal, weight);

        if (target == null)
            return;

        _animator.SetIKPosition(goal, target.position);
        _animator.SetIKRotation(goal, target.rotation);
    }
}