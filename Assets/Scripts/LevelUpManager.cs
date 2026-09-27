using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance { get; private set; }

    [SerializeField] private List<UpgradeCardSO> _pool = new List<UpgradeCardSO>();
    [SerializeField] private int _cardsPerLevel = 3;

    public event Action<List<UpgradeCardSO>> OnUpgradesOffered;

    [SerializeField] private HealthSystem _playerHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (XPManager.Instance != null)
            XPManager.Instance.OnLeveledUp += HandleLeveledUp;
    }

    private void OnDestroy()
    {
        if (XPManager.Instance != null)
            XPManager.Instance.OnLeveledUp -= HandleLeveledUp;
    }

    private void HandleLeveledUp(object sender, EventArgs e)
    {
        GameManager.Instance.ChangeState(GameState.LevelUp);
        OfferNext();
    }

    private void OfferNext()
    {
        List<UpgradeCardSO> trio = DrawUpgrades(_cardsPerLevel);
        OnUpgradesOffered?.Invoke(trio);
    }

    public void SelectUpgrade(UpgradeCardSO upgrade)
    {
        upgrade.Apply();

        if (upgrade is UpgradeSO playerCard && playerCard.StatType == PlayerStats.StatType.MaxHealth && _playerHealth != null)
            _playerHealth.RefreshMaxHealthFromStats();

        XPManager.Instance.ConsumePendingLevelUp();

        if (XPManager.Instance.PendingLevelUps > 0)
            OfferNext();
        else
            GameManager.Instance.ChangeState(GameState.Playing);
    }

    private List<UpgradeCardSO> DrawUpgrades(int count)
    {
        List<UpgradeCardSO> eligiblePool = _pool.Where(IsEligible).ToList();

        List<IGrouping<object, UpgradeCardSO>> byGroup = eligiblePool
            .GroupBy(upgrade => upgrade.GroupKey)
            .ToList();

        int drawCount = Mathf.Min(count, byGroup.Count);

        IEnumerable<IGrouping<object, UpgradeCardSO>> chosenGroups = byGroup
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(drawCount);

        List<UpgradeCardSO> result = new List<UpgradeCardSO>();
        foreach (IGrouping<object, UpgradeCardSO> group in chosenGroups)
        {
            List<UpgradeCardSO> options = group.ToList();
            result.Add(options[UnityEngine.Random.Range(0, options.Count)]);
        }

        return result;
    }

    private bool IsEligible(UpgradeCardSO card)
    {
        if (card is WeaponUpgradeSO weaponCard && weaponCard.Scope == WeaponStatsHub.ModifierScope.Category)
        {
            return PlayerWeaponHandler.Instance != null && PlayerWeaponHandler.Instance.HasCategoryInInventory(weaponCard.Category);
        }

        return true;
    }
}