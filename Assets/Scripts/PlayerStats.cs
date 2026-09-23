using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    public enum StatType
    {
        MoveSpeed,
        MaxStamina,
        StaminaRegen,
        MaxHealth,
        XpPerKill,
        CoinsPerKill
    }

    public enum ModifierKind
    {
        Flat,
        Percent
    }

    

    private readonly Dictionary<StatType, float> _flatBonus = new Dictionary<StatType, float>();
    private readonly Dictionary<StatType, float> _percentBonus = new Dictionary<StatType, float>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public float Modify(StatType type, float baseValue)
    {
        float flat = _flatBonus.TryGetValue(type, out float f) ? f : 0f;
        float percent = _percentBonus.TryGetValue(type, out float p) ? p : 0f;
        return (baseValue + flat) * (1f + percent);
    }

    public static float Get(StatType type, float baseValue)
        => Instance != null ? Instance.Modify(type, baseValue) : baseValue;

    public void ApplyModifier(StatType type, ModifierKind kind, float amount)
    {
        if (kind == ModifierKind.Flat)
            _flatBonus[type] = (_flatBonus.TryGetValue(type, out float f) ? f : 0f) + amount;
        else
            _percentBonus[type] = (_percentBonus.TryGetValue(type, out float p) ? p : 0f) + amount;
    }

    public void ResetStats()
    {
        _flatBonus.Clear();
        _percentBonus.Clear();
    }

    [ContextMenu("Log Current Stats")]
    private void LogCurrentStats()
    {
        foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
        {
            float flat = _flatBonus.TryGetValue(type, out float f) ? f : 0f;
            float percent = _percentBonus.TryGetValue(type, out float p) ? p : 0f;
            Debug.Log($"{type}: +{flat} flat, +{percent * 100f}% percent");
        }
    }
}