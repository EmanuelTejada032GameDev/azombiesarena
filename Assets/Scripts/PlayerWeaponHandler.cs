using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponHandler : MonoBehaviour
{
    public static PlayerWeaponHandler Instance { get; private set; }

    [Header("Inventory Settings")]
    [Tooltip("Set to -1 for completely infinite weapon storage capacity.")]
    [SerializeField] private int _maxWeaponLimit = 2;

    [SerializeField] private List<WeaponInstanceState> _playerCarryInventorySlots = new List<WeaponInstanceState>();

    [Header("Attachment Anchor points")]
    [SerializeField] private Transform _weaponHoldAnchor;
    [SerializeField] private ObjectPooler _defaultBulletPool;

    [Header("References")]
    [SerializeField] private WeaponDataConfig DefaultStartingWeapon;
    [SerializeField] private Weapon _activeWeaponInstance;
    private int _currentWeaponIndex = 0;

    private PlayerInput _inputs;
    private bool _isTriggerHeld;

    public Action<Weapon> OnWeaponSwapped;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _inputs = Player.Instance.GetInputInstance();

        _inputs.Player.Shoot.started += OnShootStarted;
        _inputs.Player.Shoot.canceled += OnShootCanceled;

        _inputs.Player.NextWeapon.performed += OnNextWeaponPerformed;
        _inputs.Player.PreviousWeapon.performed += OnPreviousWeaponPerformed;

        _inputs.Player.Reload.performed += OnReloadPerformed;

        if (_playerCarryInventorySlots.Count > 0)
        {
            EquipWeaponAtIndex(_currentWeaponIndex);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewMatch += GameManager_OnNewMatch;
        }
    }

    private void OnReloadPerformed(InputAction.CallbackContext context)
    {
        if (_activeWeaponInstance != null)
        {
            _activeWeaponInstance.ProcessReloadRequest();
        }
    }

    private void GameManager_OnNewMatch(object sender, EventArgs e)
    {
        SetDefaultWeapons();
    }

    private void SetDefaultWeapons()
    {
        _playerCarryInventorySlots.Clear();
        _currentWeaponIndex = 0;
        if (DefaultStartingWeapon != null)
        {
            WeaponInstanceState startingGunState = new WeaponInstanceState(DefaultStartingWeapon);
            _playerCarryInventorySlots.Add(startingGunState);
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
            _inputs.Player.Reload.performed -= OnReloadPerformed;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewMatch -= GameManager_OnNewMatch;
        }
    }

    private void Update()
    {
        if (_activeWeaponInstance != null && _isTriggerHeld && _activeWeaponInstance.Config.FiringMode == WeaponFiringMode.FullAutomatic)
        {
            _activeWeaponInstance.ProcessFireRequest(_isTriggerHeld);
        }
    }

    private void OnShootStarted(InputAction.CallbackContext context)
    {
        if (PlayerMovement.Instance.GetManeuverState() != PlayerMovement.ManeuverState.None || PlayerMovement.Instance.GetLocomotionState() == PlayerMovement.LocomotionState.Sprinting) return;

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

        WeaponInstanceState targetState = _playerCarryInventorySlots[index];
        WeaponDataConfig activeConfig = targetState.BlueprintConfig;

        if (activeConfig == null || activeConfig.WeaponModelPrefab == null) return;

        GameObject spawnedWeaponObj = Instantiate(activeConfig.WeaponModelPrefab, _weaponHoldAnchor.position, _weaponHoldAnchor.rotation, _weaponHoldAnchor);
        _activeWeaponInstance = spawnedWeaponObj.GetComponent<Weapon>();

        if (_activeWeaponInstance != null)
        {
            _activeWeaponInstance.InitializeWeapon(targetState, _defaultBulletPool);
            OnWeaponSwapped?.Invoke(_activeWeaponInstance);
        }
        else
        {
            Debug.LogError($"Weapon prefab spawned for {activeConfig.WeaponName} lacks a Weapon.cs receiver script!");
        }
    }

    public void AddWeaponToInventory(WeaponDataConfig newGunBlueprint)
    {
        if (newGunBlueprint == null) return;

        foreach (var slot in _playerCarryInventorySlots)
        {
            if (slot.BlueprintConfig == newGunBlueprint) return;
        }

        WeaponInstanceState freshWeaponInstance = new WeaponInstanceState(newGunBlueprint);

        if (_maxWeaponLimit == -1 || _playerCarryInventorySlots.Count < _maxWeaponLimit)
        {
            _playerCarryInventorySlots.Add(freshWeaponInstance);
            _currentWeaponIndex = _playerCarryInventorySlots.Count - 1;
            EquipWeaponAtIndex(_currentWeaponIndex);
        }
        else
        {
            _playerCarryInventorySlots[_currentWeaponIndex] = freshWeaponInstance;
            EquipWeaponAtIndex(_currentWeaponIndex);
        }
    }

    public bool HasWeaponInInventory(WeaponDataConfig config)
    {
        if (config == null) return false;

        foreach (var slot in _playerCarryInventorySlots)
        {
            if (slot.BlueprintConfig == config)
            {
                return true;
            }
        }
        return false;
    }


    public void ReplenishWeaponAmmo(WeaponDataConfig config)
    {
        if (config == null) return;

        foreach (var slot in _playerCarryInventorySlots)
        {
            if (slot.BlueprintConfig == config)
            {
               
                slot.CurrentReserveAmmo = config.MaxReserveAmmo;

                if (_activeWeaponInstance != null && _activeWeaponInstance.Config == config)
                {
                    _activeWeaponInstance.InitializeWeapon(slot, _defaultBulletPool);

                    OnWeaponSwapped?.Invoke(_activeWeaponInstance);
                }
                return;
            }
        }
    }
}
