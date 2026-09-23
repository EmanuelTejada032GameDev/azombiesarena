using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance { get; private set; }

    [SerializeField] private List<UpgradeSO> _pool = new List<UpgradeSO>();
    [SerializeField] private int _cardsPerLevel = 3;

    public event Action<List<UpgradeSO>> OnUpgradesOffered;

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
        List<UpgradeSO> trio = DrawUpgrades(_cardsPerLevel);
        OnUpgradesOffered?.Invoke(trio);
    }

    public void SelectUpgrade(UpgradeSO upgrade)
    {
        PlayerStats.Instance.ApplyModifier(upgrade.StatType, upgrade.ModifierKind, upgrade.Amount);

        if (upgrade.StatType == PlayerStats.StatType.MaxHealth && _playerHealth != null)
            _playerHealth.RefreshMaxHealthFromStats();

        XPManager.Instance.ConsumePendingLevelUp();

        if (XPManager.Instance.PendingLevelUps > 0)
            OfferNext();
        else
            GameManager.Instance.ChangeState(GameState.Playing);
    }

    private List<UpgradeSO> DrawUpgrades(int count)
    {
        List<IGrouping<PlayerStats.StatType, UpgradeSO>> byType = _pool
            .GroupBy(upgrade => upgrade.StatType)
            .ToList();

        int drawCount = Mathf.Min(count, byType.Count);

        IEnumerable<IGrouping<PlayerStats.StatType, UpgradeSO>> chosenTypes = byType
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(drawCount);

        List<UpgradeSO> result = new List<UpgradeSO>();
        foreach (IGrouping<PlayerStats.StatType, UpgradeSO> group in chosenTypes)
        {
            List<UpgradeSO> options = group.ToList();
            result.Add(options[UnityEngine.Random.Range(0, options.Count)]);
        }

        return result;
    }
}