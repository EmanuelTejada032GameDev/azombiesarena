using UnityEngine;
using UnityEngine.Animations.Rigging;
using static PlayerStats;

public class MyTestingScript : MonoBehaviour
{
    /*
     * QUICK SETUP GUIDE — NewCharacter test rig
     *
     * 1. This script belongs on SM_Chr_Hunter_Male_01 (the same object as its Animator and RigBuilder).
     *    The two constraints must reference the real Shoulder -> Elbow -> Hand bones for each arm.
     *
     * 2. WeaponMount places the spawned gun. WeaponDataConfig supplies its prefab and recoil values.
     *    This test only spawns the visual prefab and simulates recoil; it does not fire bullets or audio.
     *
     * 3. Choose how hand targets are driven with Target Mode:
     *    - WeaponGrips: uses the spawned gun's LeftHandGrip and RightHandAim transforms.
     *      To fix hand placement while leaving the gun where it is, edit those markers in the gun prefab
     *      (RiflePf: LeftHandGrip is under Visual; RightHandAim is under the rifle root).
     *    - ManualTargets: uses LeftHandIKTarget and RightHandIKTarget under HandIKRig. They are
     *      standalone transforms; move them to the desired hand positions. They do NOT follow the gun.
     *    - MouseCursor: moves the manual targets to the mouse and clamps them to arm reach.
     *
     * 4. T cycles WeaponGrips -> MouseCursor -> ManualTargets. Tab switches the selected hand in
     *    MouseCursor mode. Mouse0 triggers the test recoil. Both hands are enabled by default.
     *
     * 5. Animation Rigging binds the constraint target when RigBuilder builds its graph. Assign the
     *    target first, then rebuild the graph if switching between gun grips and standalone targets.
     *    In WeaponGrips mode the IK uses target position AND rotation from the gun markers. Keep both
     *    markers within arm reach. For elbow direction control, assign a Two Bone IK Hint per arm.
     *
     * The in-game overlay shows the active mode. T cycles modes; Mouse0 tests recoil.
     */
    public enum HandToControl
    {
        Left,
        Right,
        Both
    }

    public enum IKTargetMode
    {
        WeaponGrips,
        MouseCursor,
        ManualTargets
    }

    [Header("Weapon Test Setup")]
    [SerializeField] private WeaponDataConfig _weaponConfig;
    [SerializeField] private Transform _weaponMount;
    [SerializeField] private KeyCode _shootKey = KeyCode.Mouse0;
    [SerializeField, Min(0.01f)] private float _recoilDuration = 0.2f;
    [SerializeField, Min(0f)] private float _recoilDistance = 0.08f;
    [SerializeField, Min(0f)] private float _recoilPitch = 6f;

    [Header("Hand IK Test")]
    [SerializeField] private IKTargetMode _targetMode = IKTargetMode.WeaponGrips;
    [SerializeField] private HandToControl _handToControl = HandToControl.Both;
    [SerializeField] private KeyCode _switchTargetModeKey = KeyCode.T;
    [SerializeField] private KeyCode _switchHandKey = KeyCode.Tab;
    [SerializeField] private Camera _cursorCamera;
    [SerializeField, Min(0f)] private float _reachPadding = 0.02f;

    [SerializeField] private Animator _animator;
    private RigBuilder _rigBuilder;
    private Rig _handRig;
    private TwoBoneIKConstraint _leftHandConstraint;
    private TwoBoneIKConstraint _rightHandConstraint;
    private Transform _leftManualTarget;
    private Transform _rightManualTarget;
    private Transform _leftShoulder;
    private Transform _leftElbow;
    private Transform _leftHand;
    private Transform _rightShoulder;
    private Transform _rightElbow;
    private Transform _rightHand;
    private Transform _leftWeaponGrip;
    private Transform _rightWeaponGrip;
    private GameObject _spawnedWeapon;
    private Vector3 _weaponMountBasePosition;
    private Quaternion _weaponMountBaseRotation;
    private float _nextTestShotTime;
    private float _recoilStartTime = -1f;
    private float _activeRecoilStrength;
    private bool _rigReady;

    private void Start()
    {
        InitializeHandRig();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L) && PlayerStats.Instance != null)
        {
            Debug.Log("L key was pressed.");
            PlayerStats.Instance.ApplyModifier(StatType.MoveSpeed, ModifierKind.Percent, 1f);
        }

        if (!_rigReady)
            return;

        if (Input.GetKeyDown(_switchTargetModeKey))
            CycleTargetMode();

        if (_targetMode == IKTargetMode.MouseCursor)
        {
            if (Input.GetKeyDown(_switchHandKey))
                _handToControl = _handToControl == HandToControl.Left ? HandToControl.Right : HandToControl.Left;

            UpdateCursorTargets();
        }

        UpdateConstraintWeights();
        TryTriggerTestShot();
        UpdateWeaponRecoil();
    }

    private void OnGUI()
    {
        if (_rigReady)
        {
            GUI.Label(new Rect(12f, 12f, 560f, 24f),
                $"Test gun: {_weaponConfig?.WeaponName ?? "None"} | {_targetMode} | Mouse0: recoil | {_switchTargetModeKey}: target mode | Tab: hand");
        }
    }

    private void InitializeHandRig()
    {
        
        if (_animator == null)
        {
            Debug.LogError("MyTestingScript must be attached to the character GameObject that has its Animator.", this);
            return;
        }

        _rigBuilder = GetComponent<RigBuilder>();
        _handRig = FindComponentInChildren<Rig>("HandIKRig");
        _leftHandConstraint = FindComponentInChildren<TwoBoneIKConstraint>("LeftHandIKConstraint");
        _rightHandConstraint = FindComponentInChildren<TwoBoneIKConstraint>("RightHandIKConstraint");
        _leftManualTarget = FindTransformInChildren("LeftHandIKTarget");
        _rightManualTarget = FindTransformInChildren("RightHandIKTarget");
        _leftShoulder = FindTransformInChildren("Shoulder_L");
        _leftElbow = FindTransformInChildren("Elbow_L");
        _leftHand = FindTransformInChildren("Hand_L");
        _rightShoulder = FindTransformInChildren("Shoulder_R");
        _rightElbow = FindTransformInChildren("Elbow_R");
        _rightHand = FindTransformInChildren("Hand_R");

        if (_rigBuilder == null || _handRig == null ||
            _leftHandConstraint == null || _rightHandConstraint == null ||
            _leftManualTarget == null || _rightManualTarget == null ||
            _leftShoulder == null || _leftElbow == null || _leftHand == null ||
            _rightShoulder == null || _rightElbow == null || _rightHand == null)
        {
            Debug.LogError("MyTestingScript: the character is missing its RigBuilder, arm rig, IK targets, or arm bones.", this);
            return;
        }

        if (_weaponMount == null)
            _weaponMount = FindTransformInChildren("WeaponMount");

        SpawnConfiguredWeapon();
        ApplyIKTargets();
        UpdateConstraintWeights();
        RebuildRigGraph();
        _rigReady = true;
    }

    private void SpawnConfiguredWeapon()
    {
        if (_weaponConfig == null || _weaponConfig.WeaponModelPrefab == null)
        {
            Debug.LogWarning("MyTestingScript: assign a WeaponDataConfig with a weapon model prefab to spawn the test gun.", this);
            return;
        }

        if (_weaponMount == null)
        {
            Debug.LogError("MyTestingScript: assign the WeaponMount transform before spawning the test gun.", this);
            return;
        }

        _spawnedWeapon = Instantiate(_weaponConfig.WeaponModelPrefab, _weaponMount, false);
        _spawnedWeapon.name = $"TestWeapon_{_weaponConfig.WeaponName}";
        _leftWeaponGrip = FindTransformInChildren(_spawnedWeapon.transform, "LeftHandGrip");
        _rightWeaponGrip = FindTransformInChildren(_spawnedWeapon.transform, "RightHandAim");

        _weaponMountBasePosition = _weaponMount.localPosition;
        _weaponMountBaseRotation = _weaponMount.localRotation;

        if (_leftWeaponGrip == null)
            Debug.LogWarning("MyTestingScript: the configured gun has no child named LeftHandGrip; left hand can use its manual target.", this);
        if (_rightWeaponGrip == null)
            Debug.LogWarning("MyTestingScript: the configured gun has no child named RightHandAim; right hand can use its manual target.", this);
    }

    private void CycleTargetMode()
    {
        _targetMode = _targetMode switch
        {
            IKTargetMode.WeaponGrips => IKTargetMode.MouseCursor,
            IKTargetMode.MouseCursor => IKTargetMode.ManualTargets,
            _ => IKTargetMode.WeaponGrips
        };

        ApplyIKTargets();
        RebuildRigGraph();
    }

    private void RebuildRigGraph()
    {
        // Animation Rigging binds target transforms when the graph is built, so assign targets first.
        _rigBuilder.layers.Clear();
        _rigBuilder.layers.Add(new RigLayer(_handRig));
        _rigBuilder.Build();
    }

    private void ApplyIKTargets()
    {
        Transform leftTarget = _targetMode == IKTargetMode.WeaponGrips && _leftWeaponGrip != null
            ? _leftWeaponGrip
            : _leftManualTarget;
        Transform rightTarget = _targetMode == IKTargetMode.WeaponGrips && _rightWeaponGrip != null
            ? _rightWeaponGrip
            : _rightManualTarget;

        SetConstraintTarget(_leftHandConstraint, leftTarget, _targetMode == IKTargetMode.WeaponGrips);
        SetConstraintTarget(_rightHandConstraint, rightTarget, _targetMode == IKTargetMode.WeaponGrips);
    }

    private void SetConstraintTarget(TwoBoneIKConstraint constraint, Transform target, bool useTargetRotation)
    {
        TwoBoneIKConstraintData data = constraint.data;
        data.target = target;
        data.targetPositionWeight = target != null ? 1f : 0f;
        data.targetRotationWeight = target != null && useTargetRotation ? 1f : 0f;
        constraint.data = data;
    }

    private void UpdateConstraintWeights()
    {
        bool controlLeft = _handToControl == HandToControl.Left || _handToControl == HandToControl.Both;
        bool controlRight = _handToControl == HandToControl.Right || _handToControl == HandToControl.Both;
        _leftHandConstraint.weight = controlLeft ? 1f : 0f;
        _rightHandConstraint.weight = controlRight ? 1f : 0f;
    }

    private void UpdateCursorTargets()
    {
        Camera activeCamera = _cursorCamera != null ? _cursorCamera : Camera.main;
        if (activeCamera == null)
            return;

        Vector3 shoulderCenter = (_leftShoulder.position + _rightShoulder.position) * 0.5f;
        Plane cursorPlane = new Plane(activeCamera.transform.forward, shoulderCenter);
        Ray cursorRay = activeCamera.ScreenPointToRay(Input.mousePosition);

        if (!cursorPlane.Raycast(cursorRay, out float distance))
            return;

        Vector3 cursorWorldPosition = cursorRay.GetPoint(distance);
        _leftManualTarget.position = ClampToArmReach(
            cursorWorldPosition, _leftShoulder.position, _leftElbow.position, _leftHand.position);
        _rightManualTarget.position = ClampToArmReach(
            cursorWorldPosition, _rightShoulder.position, _rightElbow.position, _rightHand.position);
    }

    private void TryTriggerTestShot()
    {
        bool fireOnHold = _weaponConfig != null && _weaponConfig.FireOnHold;
        bool shootPressed = fireOnHold ? Input.GetKey(_shootKey) : Input.GetKeyDown(_shootKey);

        if (!shootPressed || Time.time < _nextTestShotTime)
            return;

        float cooldown = _weaponConfig != null ? Mathf.Max(0.01f, _weaponConfig.FireCooldown) : 0.25f;
        _nextTestShotTime = Time.time + cooldown;
        _activeRecoilStrength = _weaponConfig != null ? Mathf.Max(0f, _weaponConfig.RecoilForce) : 1f;
        _recoilStartTime = Time.time;
    }

    private void UpdateWeaponRecoil()
    {
        if (_weaponMount == null)
            return;

        if (_recoilStartTime < 0f)
        {
            _weaponMount.localPosition = _weaponMountBasePosition;
            _weaponMount.localRotation = _weaponMountBaseRotation;
            return;
        }

        float normalizedTime = Mathf.Clamp01((Time.time - _recoilStartTime) / _recoilDuration);
        float remaining = 1f - normalizedTime;
        float recoil = remaining * remaining * (3f - 2f * remaining) * _activeRecoilStrength;
        _weaponMount.localPosition = _weaponMountBasePosition + Vector3.back * (_recoilDistance * recoil);
        _weaponMount.localRotation = _weaponMountBaseRotation * Quaternion.Euler(-_recoilPitch * recoil, 0f, 0f);

        if (normalizedTime >= 1f)
            _recoilStartTime = -1f;
    }

    private T FindComponentInChildren<T>(string objectName) where T : Component
    {
        foreach (T component in GetComponentsInChildren<T>(true))
        {
            if (component.gameObject.name == objectName)
                return component;
        }

        return null;
    }

    private Transform FindTransformInChildren(string objectName)
    {
        return FindTransformInChildren(transform, objectName);
    }

    private Transform FindTransformInChildren(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child;
        }

        return null;
    }

    private Vector3 ClampToArmReach(Vector3 target, Vector3 shoulder, Vector3 elbow, Vector3 hand)
    {
        float upperArmLength = Vector3.Distance(shoulder, elbow);
        float forearmLength = Vector3.Distance(elbow, hand);
        float maximumReach = Mathf.Max(0.01f, upperArmLength + forearmLength - _reachPadding);
        Vector3 shoulderToTarget = target - shoulder;

        if (shoulderToTarget.sqrMagnitude > maximumReach * maximumReach)
            return shoulder + shoulderToTarget.normalized * maximumReach;

        return target;
    }
}
