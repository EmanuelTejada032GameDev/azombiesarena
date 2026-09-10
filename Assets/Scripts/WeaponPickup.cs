using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _pickupVisual;

    private WeaponInstanceState _weaponState;
    private float _vanishTimer;
    private bool _isInitialized = false;

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
        }
    }

    public bool CanDoInteractAction(IInteractable.InteractAction interactAction)
    {
        return interactAction == IInteractable.InteractAction.Primary && _isInitialized && _weaponState != null;
    }

    public void Interact(IInteractable.InteractAction interactAction, Transform interactorTransform)
    {
        if (interactAction != IInteractable.InteractAction.Primary) return;

        PlayerWeaponHandler weaponHandler = interactorTransform.GetComponent<PlayerWeaponHandler>();
        if (weaponHandler == null) return;

        WeaponInstanceState droppedState = weaponHandler.AddWeaponStateToInventory(_weaponState);

        if (droppedState != null)
        {
            GameObject newPickupObj = Instantiate(gameObject, transform.position, Quaternion.identity);
            WeaponPickup newPickupScript = newPickupObj.GetComponent<WeaponPickup>();

            newPickupScript.InitializePickup(droppedState, 15f);

            // Apply the physical pop-out force
            if (newPickupObj.TryGetComponent(out Rigidbody rb))
            {
                // Small upward lift vector so it gets off the ground
                Vector3 upwardForce = Vector3.up * 3f;

                // Random horizontal direction vector (X and Z axes)
                Vector3 randomHorizontalDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                Vector3 forwardForce = randomHorizontalDirection * 4f;

                // Combine them and apply an impulse force
                rb.AddForce(upwardForce + forwardForce, ForceMode.Impulse);
            }
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
