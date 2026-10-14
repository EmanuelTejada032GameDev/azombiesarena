using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _pickupVisual;
    [SerializeField] private Floater _floater;

    [Header("Launch (Loot Pop)")]
    [SerializeField] private float _launchHorizontalForce = 4f;
    [SerializeField] private float _launchUpwardForce = 5f;
    [SerializeField] private float _launchSpreadDegrees = 20f;
    [SerializeField] private float _launchSpinTorque = 6f;
    [Range(0f, 0.9f)]
    [SerializeField] private float _bounciness = 0.4f;

    private const float MIN_AIR_TIME = 0.4f;
    private const float MAX_AIR_TIME = 3f;
    private const float REST_SPEED_SQR = 0.05f;

    private WeaponInstanceState _weaponState;
    private Rigidbody _rigidbody;
    private float _vanishTimer;
    private float _airTime;
    private bool _isInitialized = false;
    private bool _isLaunching = false;
    private bool _isFloating = false;

    public void InitializePickup(WeaponInstanceState state, float lifetime)
    {
        _weaponState = state;
        _vanishTimer = lifetime;
        _isInitialized = true;

        if (_weaponState == null || _weaponState.BlueprintConfig == null) return;

        if (_pickupVisual != null)
        {
            // Clean up old meshes
            foreach (Transform child in _pickupVisual)
            {
                Destroy(child.gameObject);
            }

            GameObject gunModelPrefab = _weaponState.BlueprintConfig.WeaponModelPrefab;
            if (gunModelPrefab != null)
            {
                // 1. Spawn your prefab shell directly under our anchor point
                GameObject spawnedVisual = Instantiate(gunModelPrefab, _pickupVisual.position, _pickupVisual.rotation, _pickupVisual);

                // 2. Safely remove or disable the runtime functional scripts 
                // so they don't look for components or play sounds while on the floor
                if (spawnedVisual.TryGetComponent(out Weapon weaponComponent))
                {
                    Destroy(weaponComponent);
                }
                if (spawnedVisual.TryGetComponent(out AudioSource audioSource))
                {
                    Destroy(audioSource);
                }
            }
        }
    }

    private void Update()
    {
        if (!_isInitialized) return;

        _vanishTimer -= Time.deltaTime;
        if (_vanishTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (_isLaunching)
        {
            _airTime += Time.deltaTime;

            bool hasRested = _airTime >= MIN_AIR_TIME && _rigidbody.linearVelocity.sqrMagnitude < REST_SPEED_SQR;
            if (hasRested || _airTime >= MAX_AIR_TIME)
            {
                SettleAndFloat();
            }
        }
        else if (!_isFloating)
        {
            // Placed without a launch: just float where it is
            SettleAndFloat();
        }
    }

    /// <summary>
    /// Pops the pickup out in the given horizontal direction with a random spread, spin and bounce.
    /// When it comes to rest it starts floating.
    /// </summary>
    public void Launch(Vector3 awayDirection)
    {
        if (!TryGetComponent(out _rigidbody)) return;

        Vector3 flatDirection = new Vector3(awayDirection.x, 0f, awayDirection.z);
        if (flatDirection.sqrMagnitude < 0.0001f)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            flatDirection = new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        float spreadAngle = Random.Range(-_launchSpreadDegrees, _launchSpreadDegrees);
        flatDirection = Quaternion.Euler(0f, spreadAngle, 0f) * flatDirection.normalized;

        if (_floater != null)
            _floater.StopFloating();

        ApplyBounceMaterial();

        _isFloating = false;
        _isLaunching = true;
        _airTime = 0f;

        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        _rigidbody.AddForce(flatDirection * _launchHorizontalForce + Vector3.up * _launchUpwardForce, ForceMode.Impulse);
        _rigidbody.AddTorque(Vector3.up * Random.Range(-1f, 1f) * _launchSpinTorque, ForceMode.Impulse);
    }

    private void ApplyBounceMaterial()
    {
        if (!TryGetComponent(out Collider pickupCollider)) return;

        PhysicsMaterial bounceMaterial = new PhysicsMaterial("PickupBounce")
        {
            bounciness = _bounciness,
            dynamicFriction = 0.6f,
            staticFriction = 0.6f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Average
        };
        pickupCollider.sharedMaterial = bounceMaterial;
    }

    private void SettleAndFloat()
    {
        _isLaunching = false;
        _isFloating = true;

        if (_rigidbody != null || TryGetComponent(out _rigidbody))
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }

        if (_floater != null)
            _floater.BeginFloating();
    }

    public bool CanDoInteractAction(IInteractable.InteractAction interactAction)
    {
        return interactAction == IInteractable.InteractAction.Primary && _isInitialized && _weaponState != null && !_isLaunching;
    }

    public void Interact(IInteractable.InteractAction interactAction, Transform interactorTransform)
    {
        if (interactAction != IInteractable.InteractAction.Primary) return;

        PlayerWeaponHandler weaponHandler = interactorTransform.GetComponent<PlayerWeaponHandler>();
        if (weaponHandler == null) return;

        WeaponInstanceState droppedState = weaponHandler.AddWeaponStateToInventory(_weaponState);

        if (droppedState != null)
        {
            if (_floater != null)
                _floater.RestoreBasePose();

            // Spawn at chest height in front of the player and pop it out away from them
            Vector3 popDirection = interactorTransform.forward;
            Vector3 spawnPosition = interactorTransform.position + Vector3.up + popDirection * 0.5f;

            GameObject newPickupObj = Instantiate(gameObject, spawnPosition, Quaternion.identity);
            WeaponPickup newPickupScript = newPickupObj.GetComponent<WeaponPickup>();

            newPickupScript.InitializePickup(droppedState, 15f);
            newPickupScript.Launch(popDirection);
        }

        Destroy(gameObject);
    }

    public Dictionary<IInteractable.InteractAction, string> GetInteractTextDictionary()
    {
        var textMap = new Dictionary<IInteractable.InteractAction, string>();
        if (_weaponState != null && _weaponState.BlueprintConfig != null)
        {
            textMap.Add(IInteractable.InteractAction.Primary, $"Press [F] to pick up {_weaponState.BlueprintConfig.WeaponName}");
        }
        return textMap;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
