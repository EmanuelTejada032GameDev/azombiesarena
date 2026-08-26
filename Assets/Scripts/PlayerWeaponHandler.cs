using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Weapon _currentWeapon;

    private PlayerInput _inputs;
    private bool _isTriggerHeld;

    private void Start()
    {
        _inputs = Player.Instance.GetInputInstance();

        _inputs.Player.Shoot.started += OnShootStarted;
        _inputs.Player.Shoot.canceled += OnShootCanceled;
    }

    private void OnDestroy()
    {
        if (_inputs != null)
        {
            _inputs.Player.Shoot.started -= OnShootStarted;
            _inputs.Player.Shoot.canceled -= OnShootCanceled;
        }
    }

    private void Update()
    {
        if (_currentWeapon != null && _isTriggerHeld)
        {
            _currentWeapon.ProcessFireRequest(_isTriggerHeld);
        }
    }

    private void OnShootStarted(InputAction.CallbackContext context)
    {
        _isTriggerHeld = true;

        if (_currentWeapon != null && _currentWeapon.Config.FiringMode != WeaponFiringMode.FullAutomatic)
        {
            _currentWeapon.ProcessFireRequest(_isTriggerHeld);
        }
    }

    private void OnShootCanceled(InputAction.CallbackContext context)
    {
        _isTriggerHeld = false;
    }
}
