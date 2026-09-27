using UnityEngine;

public abstract class UpgradeCardSO : ScriptableObject
{
    [SerializeField] private string _displayName;
    [TextArea]
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;

    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;

    /// <summary>
    /// Used by LevelUpManager to keep the offered cards distinct by "kind" when drawing.
    /// Player-stat cards group by PlayerStats.StatType, weapon cards group by WeaponStatsHub.WeaponStatType
    /// (so e.g. "Shotgun Damage" and "AR Damage" count as the same group and won't both show up at once).
    /// </summary>
    public abstract object GroupKey { get; }

    public abstract void Apply();
}