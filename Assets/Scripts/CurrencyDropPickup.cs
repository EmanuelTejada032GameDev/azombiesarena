using UnityEngine;

public sealed class CurrencyDropPickup : MagnetCollectible
{
    protected override void GrantReward(int amount)
    {
        if (EconomyManager.Instance == null)
        {
            Debug.LogWarning("Cannot grant dropped currency because EconomyManager is unavailable.", this);
            return;
        }

        EconomyManager.Instance.AddPoints(amount);
    }
}
