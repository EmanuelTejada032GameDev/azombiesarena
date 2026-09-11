using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerGrenadeController : MonoBehaviour
{
    [SerializeField] private int _currentGrenades = 31;
    [SerializeField] private int _maxGrenades = 31;

    [SerializeField] private GameObject _grenadePrefab;
    [SerializeField] private Transform _throwPoint;

    [SerializeField] private float _throwForce = 30f;
    [SerializeField] private float _upwardForce = 3f;

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
        _currentGrenades--;

        GameObject spawnedGrenade = Instantiate(_grenadePrefab, _throwPoint.position, _throwPoint.rotation);
        Rigidbody rb = spawnedGrenade.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // 1. Fire the mouse ray from the camera
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // 2. FORCE the calculation plane onto the true arena ground floor (Y = 0)
            // This strips out perspective projection errors caused by the 63-degree camera lift
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            Vector3 throwDirection = transform.forward; // Reliable default safety fallback

            if (groundPlane.Raycast(ray, out float enterDistance))
            {
                Vector3 worldFloorPoint = ray.GetPoint(enterDistance);

                // 3. Flatten the player position to the same exact ground height floor level (Y = 0)
                Vector3 playerFloorBase = transform.position;
                playerFloorBase.y = 0f;

                // 4. Calculate direction from ground base to the ground target point
                throwDirection = worldFloorPoint - playerFloorBase;
                throwDirection.y = 0f;
                throwDirection.Normalize();
            }

            // 5. Inject the clean vector forces straight to the rigidbody
            Vector3 finalForce = (throwDirection * _throwForce) + (Vector3.up * _upwardForce);
            rb.AddForce(finalForce, ForceMode.Impulse);
        }
    }

    public void RefillGrenades(int amount)
    {
        _currentGrenades = Mathf.Clamp(_currentGrenades + amount, 0, _maxGrenades);
    }


    [Header("Gizmo Debugging")]
    [SerializeField] private bool _showDebugGizmos = true;
    [SerializeField] private int _trajectoryResolution = 30; // Number of points along the line
    [SerializeField] private float _timeStepBetweenPoints = 0.05f; // Time interval between points

    private void OnDrawGizmos()
    {
        if (!_showDebugGizmos || _throwPoint == null)
            return;

        // FIXED: Use the player's actual physical direction instead of the live mouse position.
        // This ensures the Gizmo line locks perfectly to your player even when the game is paused!
        Vector3 throwDirection = transform.forward;
        throwDirection.y = 0f;
        throwDirection.Normalize();

        // Calculate the exact initial Velocity vector matching your ThrowGrenade method
        Vector3 startingVelocity = (throwDirection * _throwForce) + (Vector3.up * _upwardForce);

        Vector3 previousPoint = _throwPoint.position;
        Vector3 gravity = Physics.gravity; // Grabs your scene's global gravity settings

        Gizmos.color = Color.cyan;

        // Plot and draw the parabolic arc frame by frame
        for (int i = 1; i <= _trajectoryResolution; i++)
        {
            float time = i * _timeStepBetweenPoints;

            // Kinematic Equation: Displacement = (Velocity * t) + (0.5 * Gravity * t^2)
            Vector3 displacement = (startingVelocity * time) + (0.5f * gravity * time * time);
            Vector3 currentPoint = _throwPoint.position + displacement;

            // Draw a line connecting the previous calculated point to the new one
            Gizmos.DrawLine(previousPoint, currentPoint);

            // Draw a tiny marker at each step point
            Gizmos.DrawSphere(currentPoint, 0.05f);

            previousPoint = currentPoint;
        }

        // Draw a wire sphere at the final predicted point to represent the grenade size
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(previousPoint, 0.2f);
    }


}
