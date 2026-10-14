using UnityEngine;

/// <summary>
/// Tunable data for a procedural reload animation. Every curve is evaluated over normalized reload time (0 = start, 1 = end).
/// </summary>
[CreateAssetMenu(fileName = "NewReloadAnimation", menuName = "Weapons/Reload Animation Profile")]
public class WeaponReloadAnimationProfile : ScriptableObject
{
    [Header("Weapon Pose (tilt the gun while reloading)")]
    [Tooltip("0 = normal pose, 1 = full offset below.")]
    [SerializeField] private AnimationCurve _weaponPoseCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f));
    [Tooltip("Position offset in the weapon's parent space (x right, y up, z forward).")]
    [SerializeField] private Vector3 _weaponPositionOffset = new Vector3(0f, -0.04f, -0.06f);
    [Tooltip("Euler rotation offset in the weapon's local space (pitch, yaw, roll).")]
    [SerializeField] private Vector3 _weaponRotationOffset = new Vector3(15f, 0f, -25f);

    [Header("Left Hand (magazine swap)")]
    [Tooltip("0 = hand on the grip, 1 = hand at the magazine pouch offset.")]
    [SerializeField] private AnimationCurve _leftHandCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.15f, 0f), new Keyframe(0.35f, 1f),
        new Keyframe(0.5f, 1f), new Keyframe(0.7f, 0.05f), new Keyframe(1f, 0f));
    [Tooltip("Left hand offset from the grip in the weapon's local space (x right, y up, z forward).")]
    [SerializeField] private Vector3 _leftHandOffset = new Vector3(-0.05f, -0.35f, -0.15f);

    [Header("Seat Kick (magazine slap)")]
    [SerializeField] private AnimationCurve _seatKickCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.68f, 0f), new Keyframe(0.74f, 1f),
        new Keyframe(0.9f, 0f), new Keyframe(1f, 0f));
    [Tooltip("Euler rotation added in the weapon's local space at the peak of the kick.")]
    [SerializeField] private Vector3 _seatKickRotation = new Vector3(-6f, 0f, 0f);
    [Tooltip("Position offset added in the weapon's parent space at the peak of the kick.")]
    [SerializeField] private Vector3 _seatKickPosition = new Vector3(0f, 0.01f, -0.03f);

    /// <summary>
    /// Position offset to apply to the weapon at the given normalized reload time.
    /// </summary>
    public Vector3 EvaluateWeaponPosition(float normalizedTime)
    {
        return _weaponPositionOffset * _weaponPoseCurve.Evaluate(normalizedTime) +
               _seatKickPosition * _seatKickCurve.Evaluate(normalizedTime);
    }

    /// <summary>
    /// Local rotation offset to apply to the weapon at the given normalized reload time.
    /// </summary>
    public Quaternion EvaluateWeaponRotation(float normalizedTime)
    {
        Vector3 euler = _weaponRotationOffset * _weaponPoseCurve.Evaluate(normalizedTime) +
                        _seatKickRotation * _seatKickCurve.Evaluate(normalizedTime);
        return Quaternion.Euler(euler);
    }

    /// <summary>
    /// Left hand offset (weapon local space) at the given normalized reload time.
    /// </summary>
    public Vector3 EvaluateLeftHandOffset(float normalizedTime)
    {
        return _leftHandOffset * _leftHandCurve.Evaluate(normalizedTime);
    }
}
