using System.Collections.Generic;
using UnityEngine;

public class WallBuy : MonoBehaviour, IInteractable
{
    [SerializeField] private WeaponDataConfig _weaponToGive;

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
                weaponHandler.AddWeaponToInventory(_weaponToGive);
            }
        }
        else
        {
            
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
