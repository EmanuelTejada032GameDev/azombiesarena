using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeSO", menuName = "Scriptable Objects/Upgrades/Player Stat Upgrade")]
public class UpgradeSO : UpgradeCardSO
{
    [Header("Effect")]
    [SerializeField] private PlayerStats.StatType _statType;
    [SerializeField] private PlayerStats.ModifierKind _modifierKind;
    [SerializeField] private float _amount = 0.1f;

    public PlayerStats.StatType StatType => _statType;
    public PlayerStats.ModifierKind ModifierKind => _modifierKind;
    public float Amount => _amount;

    public override object GroupKey => _statType;

    public override void Apply()
    {
        PlayerStats.Instance.ApplyModifier(_statType, _modifierKind, _amount);
    }
}