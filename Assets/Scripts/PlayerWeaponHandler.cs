using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Weapon _currentWeapon;

    private PlayerInput _inputs;

    private void Start()
    {
        PlayerMovement movementComponent = GetComponent<PlayerMovement>();

        if (movementComponent != null)
        {
          
            _inputs = movementComponent.GetInputInstance();
            _inputs.Player.Shoot.performed += OnShootPerformed;
        }
    }

    private void OnDestroy()
    {
        if (_inputs != null)
        {
            _inputs.Player.Shoot.performed -= OnShootPerformed;
        }
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        if (_currentWeapon != null)
        {
            _currentWeapon.FireTrigger();
        }
    }
}