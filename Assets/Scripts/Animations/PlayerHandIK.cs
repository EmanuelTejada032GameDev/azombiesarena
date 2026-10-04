using UnityEngine;

public class PlayerHandIK : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _rightHandTarget;
    [SerializeField] private Transform _weaponSocket;
    [SerializeField] private string _upperBodyLayer = "UpperBody";
    [SerializeField] private float _blendSpeed = 10f;
    private Weapon _currentWeapon;

    private int _layerIndex;
    private Transform _leftHandGrip;
    private float _weight;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        _layerIndex = _animator.GetLayerIndex(_upperBodyLayer);
    }

    private void Update()
    {
        PlayerMovement movement = PlayerMovement.Instance;

        if (movement == null)
            return;

        bool aiming =
            movement.GetLocomotionState() != PlayerMovement.LocomotionState.Sprinting &&
            movement.GetManeuverState() == PlayerMovement.ManeuverState.None;

        _weight = Mathf.MoveTowards(_weight, aiming ? 1f : 0f, _blendSpeed * Time.deltaTime);

        RefreshWeaponTargets();
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
        if (layerIndex != _layerIndex)
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