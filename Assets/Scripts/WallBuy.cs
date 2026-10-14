 using System.Collections.Generic;
using UnityEngine;

public class WallBuy : MonoBehaviour, IInteractable
{
    [SerializeField] private WeaponDataConfig _weaponToGive;
    [SerializeField] private GameObject _weaponPickupPrefabShell;

    public bool CanDoInteractAction(IInteractable.InteractAction interactAction)
    {
        if (interactAction == IInteractable.InteractAction.Primary)
        {
            return _weaponToGive != null;
        }
        return false;
    }

    public void Interact(IInteractable.InteractAction interactAction, Transform interactorTransform)
    {
        if (interactAction != IInteractable.InteractAction.Primary || _weaponToGive == null) return;

        PlayerWeaponHandler weaponHandler = interactorTransform.GetComponent<PlayerWeaponHandler>();
        if (weaponHandler == null) return;

        bool holdsWeapon = weaponHandler.HasWeaponInInventory(_weaponToGive);

        int activeCost = holdsWeapon ? _weaponToGive.NormalAmmoPrice : _weaponToGive.BasePurchaseCost;

        if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendPoints(activeCost))
        {
            if (holdsWeapon)
            {
                weaponHandler.ReplenishWeaponAmmo(_weaponToGive);
            }
            else
            {
                WeaponInstanceState droppedState = weaponHandler.AddWeaponStateToInventory(new WeaponInstanceState(_weaponToGive));

                if (droppedState != null && _weaponPickupPrefabShell != null)
                {
                    // Pop the old weapon away from the wall, toward the player's side
                    Vector3 awayFromWall = interactorTransform.position - transform.position;
                    awayFromWall.y = 0f;
                    awayFromWall = awayFromWall.sqrMagnitude > 0.0001f ? awayFromWall.normalized : interactorTransform.forward;

                    Vector3 spawnPosition = interactorTransform.position + Vector3.up + awayFromWall * 0.5f;
                    GameObject newPickupObj = Instantiate(_weaponPickupPrefabShell, spawnPosition, Quaternion.identity);
                    WeaponPickup newPickupScript = newPickupObj.GetComponent<WeaponPickup>();

                    newPickupScript.InitializePickup(droppedState, 15f);
                    newPickupScript.Launch(awayFromWall);
                }
            }
        }
       
    }

    public Dictionary<IInteractable.InteractAction, string> GetInteractTextDictionary()
    {
        Dictionary<IInteractable.InteractAction, string> textMap = new Dictionary<IInteractable.InteractAction, string>();

        if (_weaponToGive != null)
        {
            if (PlayerWeaponHandler.Instance != null && PlayerWeaponHandler.Instance.HasWeaponInInventory(_weaponToGive))
            {
                textMap.Add(IInteractable.InteractAction.Primary, $"Replenish ammo: [Cost: ${_weaponToGive.NormalAmmoPrice}]");
            }
            else
            {
                textMap.Add(IInteractable.InteractAction.Primary, $"Press [F] to buy {_weaponToGive.WeaponName} [Cost: ${_weaponToGive.BasePurchaseCost}]");
            }
        }

        return textMap;
    }


    public Transform GetTransform()
    {
        return transform;
    }
}
