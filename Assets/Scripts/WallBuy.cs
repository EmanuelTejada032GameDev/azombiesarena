using System.Collections.Generic;
using UnityEngine;

public class WallBuy : MonoBehaviour, IInteractable
{
    [SerializeField] private WeaponDataConfig _weaponToGive;
    [SerializeField] private int _purchaseCost = 500;

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

        if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendPoints(_purchaseCost))
        {
            weaponHandler.AddWeaponToInventory(_weaponToGive);
        }
        else
        {
            // cant get item feedback logic
        }
    }

    public Dictionary<IInteractable.InteractAction, string> GetInteractTextDictionary()
    {
        Dictionary<IInteractable.InteractAction, string> textMap = new Dictionary<IInteractable.InteractAction, string>();

        if (_weaponToGive != null)
        {
            textMap.Add(IInteractable.InteractAction.Primary, $"Press [F] to buy {_weaponToGive.WeaponName} [Cost: {_purchaseCost}]");
        }

        return textMap;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
