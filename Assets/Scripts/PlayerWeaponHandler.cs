using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponHandler : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("Set to -1 for completely infinite weapon storage capacity.")]
    [SerializeField] private int _maxWeaponLimit = 2;
    [SerializeField] private List<WeaponDataConfig> _playerCarryInventorySlots = new List<WeaponDataConfig>();

    [Header("Attachment Anchor points")]
    [SerializeField] private Transform _weaponHoldAnchor; 
    [SerializeField] private ObjectPooler _defaultBulletPool; 


    [Header("References")]
    [SerializeField] private Weapon _activeWeaponInstance;
    private int _currentWeaponIndex = 0;

    private PlayerInput _inputs;
    private bool _isTriggerHeld;

    private void Start()
    {
        _inputs = Player.Instance.GetInputInstance();

        _inputs.Player.Shoot.started += OnShootStarted;
        _inputs.Player.Shoot.canceled += OnShootCanceled;

        _inputs.Player.NextWeapon.performed += OnNextWeaponPerformed;
        _inputs.Player.PreviousWeapon.performed += OnPreviousWeaponPerformed;


        if (_playerCarryInventorySlots.Count > 0)
        {
            EquipWeaponAtIndex(_currentWeaponIndex);
        }
    }

    private void OnPreviousWeaponPerformed(InputAction.CallbackContext context)
    {
        if (_playerCarryInventorySlots.Count <= 1) return;

        _currentWeaponIndex = (_currentWeaponIndex - 1 + _playerCarryInventorySlots.Count) % _playerCarryInventorySlots.Count;
        EquipWeaponAtIndex(_currentWeaponIndex);
    }

    private void OnNextWeaponPerformed(InputAction.CallbackContext context)
    {
        if (_playerCarryInventorySlots.Count <= 1) return;

        _currentWeaponIndex = (_currentWeaponIndex + 1) % _playerCarryInventorySlots.Count;
        EquipWeaponAtIndex(_currentWeaponIndex);
    }

    private void OnDestroy()
    {
        if (_inputs != null)
        {
            _inputs.Player.Shoot.started -= OnShootStarted;
            _inputs.Player.Shoot.canceled -= OnShootCanceled;
            _inputs.Player.NextWeapon.performed -= OnNextWeaponPerformed;
            _inputs.Player.PreviousWeapon.performed -= OnPreviousWeaponPerformed;
        }
    }

    private void Update()
    {
        if (_activeWeaponInstance != null && _isTriggerHeld)
        {
            _activeWeaponInstance.ProcessFireRequest(_isTriggerHeld);
        }
    }

    private void OnShootStarted(InputAction.CallbackContext context)
    {
        _isTriggerHeld = true;

        if (_activeWeaponInstance != null && _activeWeaponInstance.Config.FiringMode != WeaponFiringMode.FullAutomatic)
        {
            _activeWeaponInstance.ProcessFireRequest(_isTriggerHeld);
        }
    }

    private void OnShootCanceled(InputAction.CallbackContext context)
    {
        _isTriggerHeld = false;
    }


    private void EquipWeaponAtIndex(int index)
    {
        if (_activeWeaponInstance != null)
        {
            Destroy(_activeWeaponInstance.gameObject);
        }

        if (index < 0 || index >= _playerCarryInventorySlots.Count || _playerCarryInventorySlots[index] == null) return;

        WeaponDataConfig activeConfig = _playerCarryInventorySlots[index];
        if (activeConfig.WeaponModelPrefab == null) return;

        GameObject spawnedWeaponObj = Instantiate(activeConfig.WeaponModelPrefab, _weaponHoldAnchor.position, _weaponHoldAnchor.rotation, _weaponHoldAnchor);
        _activeWeaponInstance = spawnedWeaponObj.GetComponent<Weapon>();

        if (_activeWeaponInstance != null)
        {
            _activeWeaponInstance.InitializeWeapon(activeConfig, _defaultBulletPool);
        }
        else
        {
            Debug.LogError($"Weapon prefab spawned for {activeConfig.WeaponName} lacks a Weapon.cs receiver script!");
        }
    }

 
    public void AddWeaponToInventory(WeaponDataConfig newGun)
    {
        if (_playerCarryInventorySlots.Contains(newGun)) return;

        if (_maxWeaponLimit == -1 || _playerCarryInventorySlots.Count < _maxWeaponLimit)
        {
            _playerCarryInventorySlots.Add(newGun);
            _currentWeaponIndex = _playerCarryInventorySlots.Count - 1;
            EquipWeaponAtIndex(_currentWeaponIndex);
        }
        else
        {
            _playerCarryInventorySlots[_currentWeaponIndex] = newGun;
            EquipWeaponAtIndex(_currentWeaponIndex);
        }
    }

}
