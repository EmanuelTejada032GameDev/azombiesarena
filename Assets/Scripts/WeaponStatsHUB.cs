using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponStatsHub : MonoBehaviour
{
    public static WeaponStatsHub Instance { get; private set; }

    public enum WeaponStatType
    {
        Damage,
        FireRate,
        ReloadSpeed,
        MagSize,
        ReserveAmmo,
        CritChance,
        CritDamage
    }

    public enum ModifierKind
    {
        Flat,
        Percent
    }

    public enum ModifierScope
    {
        Global,
        Category
    }

    [Tooltip("Lowest allowed fire cooldown / reload duration in seconds, regardless of how much FireRate/ReloadSpeed is stacked.")]
    [SerializeField] private float _minCooldownFloor = 0.05f;

    private readonly Dictionary<WeaponStatType, float> _globalFlatBonus = new Dictionary<WeaponStatType, float>();
    private readonly Dictionary<WeaponStatType, float> _globalPercentBonus = new Dictionary<WeaponStatType, float>();

    private readonly Dictionary<(WeaponCategory, WeaponStatType), float> _categoryFlatBonus = new Dictionary<(WeaponCategory, WeaponStatType), float>();
    private readonly Dictionary<(WeaponCategory, WeaponStatType), float> _categoryPercentBonus = new Dictionary<(WeaponCategory, WeaponStatType), float>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public float Modify(WeaponStatType type, WeaponCategory category, float baseValue)
    {
        float globalFlat = _globalFlatBonus.TryGetValue(type, out float gf) ? gf : 0f;
        float globalPercent = _globalPercentBonus.TryGetValue(type, out float gp) ? gp : 0f;

        float categoryFlat = _categoryFlatBonus.TryGetValue((category, type), out float cf) ? cf : 0f;
        float categoryPercent = _categoryPercentBonus.TryGetValue((category, type), out float cp) ? cp : 0f;

        float result = (baseValue + globalFlat + categoryFlat) * (1f + globalPercent + categoryPercent);

        if (type == WeaponStatType.FireRate || type == WeaponStatType.ReloadSpeed)
        {
            result = Mathf.Max(result, _minCooldownFloor);
        }

        return result;
    }

    public static float Get(WeaponStatType type, WeaponCategory category, float baseValue)
        => Instance != null ? Instance.Modify(type, category, baseValue) : baseValue;

    public void ApplyModifier(ModifierScope scope, WeaponCategory category, WeaponStatType type, ModifierKind kind, float amount)
    {
        if (scope == ModifierScope.Global)
        {
            if (kind == ModifierKind.Flat)
                _globalFlatBonus[type] = (_globalFlatBonus.TryGetValue(type, out float f) ? f : 0f) + amount;
            else
                _globalPercentBonus[type] = (_globalPercentBonus.TryGetValue(type, out float p) ? p : 0f) + amount;
        }
        else
        {
            var key = (category, type);
            if (kind == ModifierKind.Flat)
                _categoryFlatBonus[key] = (_categoryFlatBonus.TryGetValue(key, out float f) ? f : 0f) + amount;
            else
                _categoryPercentBonus[key] = (_categoryPercentBonus.TryGetValue(key, out float p) ? p : 0f) + amount;
        }
    }

    public void ResetStats()
    {
        _globalFlatBonus.Clear();
        _globalPercentBonus.Clear();
        _categoryFlatBonus.Clear();
        _categoryPercentBonus.Clear();
    }

    [ContextMenu("Log Current Weapon Stats")]
    private void LogCurrentStats()
    {
        foreach (WeaponStatType type in Enum.GetValues(typeof(WeaponStatType)))
        {
            float flat = _globalFlatBonus.TryGetValue(type, out float f) ? f : 0f;
            float percent = _globalPercentBonus.TryGetValue(type, out float p) ? p : 0f;
            Debug.Log($"[Global] {type}: +{flat} flat, +{percent * 100f}% percent");
        }

        foreach (WeaponCategory category in Enum.GetValues(typeof(WeaponCategory)))
        {
            foreach (WeaponStatType type in Enum.GetValues(typeof(WeaponStatType)))
            {
                var key = (category, type);
                float flat = _categoryFlatBonus.TryGetValue(key, out float f) ? f : 0f;
                float percent = _categoryPercentBonus.TryGetValue(key, out float p) ? p : 0f;

                if (flat != 0f || percent != 0f)
                    Debug.Log($"[{category}] {type}: +{flat} flat, +{percent * 100f}% percent");
            }
        }
    }
}