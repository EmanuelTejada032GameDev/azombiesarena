using UnityEngine;

[CreateAssetMenu(fileName = "WeaponUpgradeSO", menuName = "Scriptable Objects/Upgrades/Weapon Stat Upgrade")]
public class WeaponUpgradeSO : UpgradeCardSO
{
    [Header("Effect")]
    [SerializeField] private WeaponStatsHub.WeaponStatType _statType;
    [SerializeField] private WeaponStatsHub.ModifierScope _scope = WeaponStatsHub.ModifierScope.Global;
    [Tooltip("Only used when Scope is Category.")]
    [SerializeField] private WeaponCategory _category;
    [SerializeField] private WeaponStatsHub.ModifierKind _modifierKind;
    [SerializeField] private float _amount = 0.1f;

    public WeaponStatsHub.WeaponStatType StatType => _statType;
    public WeaponStatsHub.ModifierScope Scope => _scope;
    public WeaponCategory Category => _category;
    public WeaponStatsHub.ModifierKind ModifierKind => _modifierKind;
    public float Amount => _amount;

    public override object GroupKey => _statType;

    public override void Apply()
    {
        WeaponStatsHub.Instance.ApplyModifier(_scope, _category, _statType, _modifierKind, _amount);
    }
}