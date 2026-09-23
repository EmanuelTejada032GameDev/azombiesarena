using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeSO", menuName = "Scriptable Objects/UpgradeSO")]
public class UpgradeSO : ScriptableObject
{
    [SerializeField] private string _displayName;
    [TextArea]
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;

    [Header("Effect")]
    [SerializeField] private PlayerStats.StatType _statType;
    [SerializeField] private PlayerStats.ModifierKind _modifierKind;
    [SerializeField] private float _amount = 0.1f;

    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public PlayerStats.StatType StatType => _statType;
    public PlayerStats.ModifierKind ModifierKind => _modifierKind;
    public float Amount => _amount;
}