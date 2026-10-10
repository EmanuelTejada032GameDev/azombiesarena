using UnityEngine;
using UnityEngine.Animations.Rigging;

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

    private int _twoHandedIndex;
    private int _oneHandedIndex;
    private Rig _handRig;
    private TwoBoneIKConstraint _leftHandConstraint;
    private TwoBoneIKConstraint _rightHandConstraint;
    private RigBuilder _rigBuilder;
    private Weapon _currentWeapon;
    private Transform _leftHandGrip;
    private Transform _rightHandAim;
    private Transform _leftHandTargetProxy;
    private Transform _rightHandTargetProxy;
    private float _weight;
    private bool _isAiming;
    private float _recoilStartTime = -1f;
    private float _recoilStrength;
    private Vector3 _weaponSocketBaseLocalPosition;

    private bool IsOneHanded => _currentWeapon != null && _leftHandGrip == null;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (_animator == null)
        {
            Debug.LogError("PlayerHandIK requires an Animator on the same GameObject.", this);
            enabled = false;
            return;
        }

        _twoHandedIndex = _animator.GetLayerIndex(_twoHandedLayer);
        _oneHandedIndex = _animator.GetLayerIndex(_oneHandedLayer);
        _rigBuilder = GetComponent<RigBuilder>();
        _handRig = GetComponentInChildren<Rig>(true);
        _leftHandConstraint = FindConstraint("LeftHandConstraint");
        _rightHandConstraint = FindConstraint("RightHandConstraint");
    }

    private void Start()
    {
        if (_rigBuilder == null || _handRig == null ||
            _leftHandConstraint == null || _rightHandConstraint == null || _weaponSocket == null)
        {
            Debug.LogError("PlayerHandIK needs a RigBuilder, a rig with left/right TwoBone IK constraints, and a torso-mounted weapon socket.", this);
            enabled = false;
            return;
        }

        _weaponSocketBaseLocalPosition = _weaponSocket.localPosition;

        _rigBuilder.layers.Clear();
        _rigBuilder.layers.Add(new RigLayer(_handRig));

        CreateHandTargetProxies();
        SetConstraintTarget(_leftHandConstraint, _leftHandTargetProxy);
        SetConstraintTarget(_rightHandConstraint, _rightHandTargetProxy);
        RefreshWeaponTargets();
        SyncHandTargetProxies();

        if (!_rigBuilder.Build())
            Debug.LogError("PlayerHandIK could not build the Animation Rigging graph.", this);
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
        if (_weaponSocket == null)
            return;

        RefreshWeaponTargets();
        UpdateWeaponRecoil();
        SyncHandTargetProxies();

        PlayerMovement movement = PlayerMovement.Instance;
        _isAiming = movement == null ||
            (movement.GetLocomotionState() != PlayerMovement.LocomotionState.Sprinting &&
             movement.GetManeuverState() == PlayerMovement.ManeuverState.None);

        _weight = Mathf.MoveTowards(_weight, _isAiming ? 1f : 0f, _blendSpeed * Time.deltaTime);

        if (_handRig != null)
            _handRig.weight = _weight;

        if (_leftHandConstraint != null)
            _leftHandConstraint.weight = _leftHandGrip != null ? 1f : 0f;

        if (_rightHandConstraint != null)
            _rightHandConstraint.weight = _rightHandAim != null ? 1f : 0f;

        float step = _layerBlendSpeed * Time.deltaTime;
        BlendLayer(_twoHandedIndex, IsOneHanded ? 0f : 1f, step);
        BlendLayer(_oneHandedIndex, IsOneHanded ? 1f : 0f, step);
    }

    private void LateUpdate()
    {
        SyncHandTargetProxies();
    }

    private void CreateHandTargetProxies()
    {
        Transform targetParent = transform.parent;
        string targetPrefix = targetParent != null ? targetParent.name : gameObject.name;

        _leftHandTargetProxy = CreateHandTargetProxy($"{targetPrefix}_LeftHandIKTarget", targetParent);
        _rightHandTargetProxy = CreateHandTargetProxy($"{targetPrefix}_RightHandIKTarget", targetParent);
    }

    private Transform CreateHandTargetProxy(string targetName, Transform targetParent)
    {
        GameObject targetObject = new GameObject(targetName);
        Transform targetTransform = targetObject.transform;

        if (targetParent != null)
            targetTransform.SetParent(targetParent, false);

        return targetTransform;
    }

    private void SyncHandTargetProxies()
    {
        if (_leftHandGrip != null && _leftHandTargetProxy != null)
            _leftHandTargetProxy.SetPositionAndRotation(_leftHandGrip.position, _leftHandGrip.rotation);

        if (_rightHandAim != null && _rightHandTargetProxy != null)
            _rightHandTargetProxy.SetPositionAndRotation(_rightHandAim.position, _rightHandAim.rotation);
    }

    private void RefreshWeaponTargets()
    {
        Weapon weapon = _weaponSocket.GetComponentInChildren<Weapon>();
        if (weapon == _currentWeapon)
            return;

        _currentWeapon = weapon;
        _leftHandGrip = weapon != null ? FindNamedChild(weapon.transform, "LeftHandGrip") : null;
        _rightHandAim = weapon != null ? FindNamedChild(weapon.transform, "RightHandAim") : null;

        SetConstraintTarget(_leftHandConstraint, _leftHandTargetProxy);
        SetConstraintTarget(_rightHandConstraint, _rightHandTargetProxy);
        SyncHandTargetProxies();

        if (weapon == null)
        {
            _recoilStartTime = -1f;
            _weaponSocket.localPosition = _weaponSocketBaseLocalPosition;
        }

        // Bind scene-space proxy targets so runtime weapon instances remain live IK controls.
        if (_rigBuilder != null && _handRig != null && _rigBuilder.layers.Count > 0 && !_rigBuilder.Build())
            Debug.LogError("PlayerHandIK could not rebuild the Animation Rigging graph for the equipped weapon.", this);
    }

    private void OnDestroy()
    {
        if (_leftHandTargetProxy != null)
            Destroy(_leftHandTargetProxy.gameObject);

        if (_rightHandTargetProxy != null)
            Destroy(_rightHandTargetProxy.gameObject);
    }

    private void SetConstraintTarget(TwoBoneIKConstraint constraint, Transform target)
    {
        if (constraint == null)
            return;

        TwoBoneIKConstraintData data = constraint.data;
        data.target = target;
        data.targetPositionWeight = target != null ? 1f : 0f;
        data.targetRotationWeight = target != null ? 1f : 0f;
        constraint.data = data;
    }

    private void UpdateWeaponRecoil()
    {
        if (_recoilStartTime < 0f)
            return;

        float normalizedTime = Mathf.Clamp01((Time.time - _recoilStartTime) / Mathf.Max(_recoilDuration, 0.01f));
        float remaining = 1f - normalizedTime;
        float recoil = remaining * remaining * (3f - 2f * remaining) * _recoilStrength;

        if (_currentWeapon != null)
        {
            Vector3 recoilBackDirection = -_currentWeapon.transform.forward;
            if (_weaponSocket.parent != null)
                recoilBackDirection = _weaponSocket.parent.InverseTransformDirection(recoilBackDirection);

            _weaponSocket.localPosition = _weaponSocketBaseLocalPosition +
                recoilBackDirection * (_recoilBackOffset * recoil);
        }

        if (normalizedTime >= 1f)
        {
            _weaponSocket.localPosition = _weaponSocketBaseLocalPosition;
            _recoilStartTime = -1f;
            _recoilStrength = 0f;
        }
    }

    private void BlendLayer(int index, float target, float step)
    {
        if (index < 0)
            return;

        float current = _animator.GetLayerWeight(index);
        _animator.SetLayerWeight(index, Mathf.MoveTowards(current, target, step));
    }

    private TwoBoneIKConstraint FindConstraint(string objectName)
    {
        foreach (TwoBoneIKConstraint constraint in GetComponentsInChildren<TwoBoneIKConstraint>(true))
        {
            if (constraint.gameObject.name == objectName)
                return constraint;
        }

        return null;
    }

    private Transform FindNamedChild(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child;
        }

        return null;
    }
}
