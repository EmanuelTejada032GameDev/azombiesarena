using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerGrenadeController : MonoBehaviour
{
    [SerializeField] private int _currentGrenades = 31;
    [SerializeField] private int _maxGrenades = 31;

    [SerializeField] private GameObject _grenadePrefab;
    [SerializeField] private Transform _throwPoint;

    [SerializeField, Min(0f), Tooltip("Maximum launch speed for aimed throws; farther aim points clamp to the reachable range.")]
    private float _throwForce = 30f;
    [SerializeField] private float _upwardForce = 3f;
    [SerializeField, Range(10f, 80f), Tooltip("Launch arc angle used when aiming at the ground.")]
    private float _aimArcAngleDegrees = 45f;

    private PlayerInput _inputs;


    private void Start()
    {
        _inputs = Player.Instance.GetInputInstance();

        _inputs.Player.Grenade.performed += OnGrenadePerformed;
    }

    private void OnDestroy()
    {
        if (_inputs != null)
        {
            _inputs.Player.Grenade.performed -= OnGrenadePerformed;
        }
    }

    private void OnGrenadePerformed(InputAction.CallbackContext context)
    {
        if (_currentGrenades <= 0)
        {
            return;
        }

        ThrowGrenade();
    }
    private void ThrowGrenade()
    {
        if (_grenadePrefab == null || _throwPoint == null)
            return;

        _currentGrenades--;

        GameObject spawnedGrenade = Instantiate(_grenadePrefab, _throwPoint.position, _throwPoint.rotation);
        Rigidbody rb = spawnedGrenade.GetComponent<Rigidbody>();

        if (rb == null)
            return;

        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null && playerMovement.HasAimPoint &&
            TryCalculateAimedVelocity(_throwPoint.position, playerMovement.AimPoint, out Vector3 aimedVelocity))
        {
            rb.AddForce(aimedVelocity * rb.mass, ForceMode.Impulse);
            return;
        }

        Vector3 fallbackDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (fallbackDirection.sqrMagnitude < 0.001f)
            fallbackDirection = Vector3.forward;

        Vector3 fallbackForce = fallbackDirection * _throwForce + Vector3.up * _upwardForce;
        rb.AddForce(fallbackForce, ForceMode.Impulse);
    }

    private bool TryCalculateAimedVelocity(Vector3 launchPosition, Vector3 aimPoint, out Vector3 launchVelocity)
    {
        launchVelocity = Vector3.zero;
        Vector3 horizontalOffset = aimPoint - launchPosition;
        horizontalOffset.y = 0f;

        if (horizontalOffset.sqrMagnitude < 0.001f)
        {
            horizontalOffset = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (horizontalOffset.sqrMagnitude < 0.001f)
                horizontalOffset = Vector3.forward;
        }

        float targetHeightDifference = -launchPosition.y;
        float horizontalDistance = horizontalOffset.magnitude;
        float gravity = Mathf.Abs(Physics.gravity.y);
        float angleRadians = Mathf.Clamp(_aimArcAngleDegrees, 10f, 80f) * Mathf.Deg2Rad;
        float cosAngle = Mathf.Cos(angleRadians);
        float tanAngle = Mathf.Tan(angleRadians);
        float maximumSpeed = Mathf.Max(0f, _throwForce);

        float requiredSpeed = GetRequiredLaunchSpeed(horizontalDistance, targetHeightDifference, gravity, cosAngle, tanAngle);
        if (requiredSpeed > maximumSpeed)
        {
            float minimumReach = 0f;
            float maximumReach = horizontalDistance;

            for (int iteration = 0; iteration < 16; iteration++)
            {
                float candidateDistance = (minimumReach + maximumReach) * 0.5f;
                float candidateSpeed = GetRequiredLaunchSpeed(candidateDistance, targetHeightDifference, gravity, cosAngle, tanAngle);

                if (candidateSpeed <= maximumSpeed)
                    minimumReach = candidateDistance;
                else
                    maximumReach = candidateDistance;
            }

            horizontalDistance = minimumReach;
            requiredSpeed = GetRequiredLaunchSpeed(horizontalDistance, targetHeightDifference, gravity, cosAngle, tanAngle);
        }

        if (requiredSpeed <= 0f || requiredSpeed > maximumSpeed || float.IsInfinity(requiredSpeed) || float.IsNaN(requiredSpeed))
            return false;

        Vector3 horizontalDirection = horizontalOffset.normalized;
        launchVelocity = horizontalDirection * (requiredSpeed * cosAngle) + Vector3.up * (requiredSpeed * Mathf.Sin(angleRadians));
        return true;
    }

    private static float GetRequiredLaunchSpeed(float horizontalDistance, float verticalDistance, float gravity, float cosAngle, float tanAngle)
    {
        if (horizontalDistance <= 0f || gravity <= 0f)
            return float.PositiveInfinity;

        float denominator = 2f * cosAngle * cosAngle * (horizontalDistance * tanAngle - verticalDistance);
        if (denominator <= 0f)
            return float.PositiveInfinity;

        return Mathf.Sqrt(gravity * horizontalDistance * horizontalDistance / denominator);
    }

    /// <summary>Restores grenades up to the configured maximum inventory.</summary>
    public void RefillGrenades(int amount)
    {
        _currentGrenades = Mathf.Clamp(_currentGrenades + amount, 0, _maxGrenades);
    }


    [Header("Gizmo Debugging")]
    [SerializeField] private bool _showDebugGizmos = true;
    [SerializeField] private int _trajectoryResolution = 30; // Number of points along the line
    [SerializeField] private float _timeStepBetweenPoints = 0.05f; // Time interval between points

    //private void OnDrawGizmos()
    //{
    //    if (!_showDebugGizmos || _throwPoint == null)
    //        return;

    //    // FIXED: Use the player's actual physical direction instead of the live mouse position.
    //    // This ensures the Gizmo line locks perfectly to your player even when the game is paused!
    //    Vector3 throwDirection = transform.forward;
    //    throwDirection.y = 0f;
    //    throwDirection.Normalize();

    //    // Calculate the exact initial Velocity vector matching your ThrowGrenade method
    //    Vector3 startingVelocity = (throwDirection * _throwForce) + (Vector3.up * _upwardForce);

    //    Vector3 previousPoint = _throwPoint.position;
    //    Vector3 gravity = Physics.gravity; // Grabs your scene's global gravity settings

    //    Gizmos.color = Color.cyan;

    //    // Plot and draw the parabolic arc frame by frame
    //    for (int i = 1; i <= _trajectoryResolution; i++)
    //    {
    //        float time = i * _timeStepBetweenPoints;

    //        // Kinematic Equation: Displacement = (Velocity * t) + (0.5 * Gravity * t^2)
    //        Vector3 displacement = (startingVelocity * time) + (0.5f * gravity * time * time);
    //        Vector3 currentPoint = _throwPoint.position + displacement;

    //        // Draw a line connecting the previous calculated point to the new one
    //        Gizmos.DrawLine(previousPoint, currentPoint);

    //        // Draw a tiny marker at each step point
    //        Gizmos.DrawSphere(currentPoint, 0.05f);

    //        previousPoint = currentPoint;
    //    }

    //    // Draw a wire sphere at the final predicted point to represent the grenade size
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(previousPoint, 0.2f);
    //}


}
