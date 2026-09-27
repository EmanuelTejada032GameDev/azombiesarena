using System;
using UnityEngine;

[Serializable]
public class WeaponInstanceState
{
    private WeaponDataConfig _blueprintConfig;

    private int _currentMagazineAmmo;
    private int _currentReserveAmmo;

    public WeaponDataConfig BlueprintConfig => _blueprintConfig;
    public int CurrentMagazineAmmo { get => _currentMagazineAmmo; set => _currentMagazineAmmo = value; }
    public int CurrentReserveAmmo { get => _currentReserveAmmo; set => _currentReserveAmmo = value; }

    public WeaponInstanceState(WeaponDataConfig config)
    {
        _blueprintConfig = config;

        if (_blueprintConfig != null)
        {
            _currentMagazineAmmo = Mathf.RoundToInt(WeaponStatsHub.Get(WeaponStatsHub.WeaponStatType.MagSize, _blueprintConfig.Category, _blueprintConfig.MaxMagazineSize));
            _currentReserveAmmo = Mathf.RoundToInt(WeaponStatsHub.Get(WeaponStatsHub.WeaponStatType.ReserveAmmo, _blueprintConfig.Category, _blueprintConfig.MaxReserveAmmo));
        }
    }

    public WeaponInstanceState(WeaponDataConfig config, int magazineAmmo, int reserveAmmo)
    {
        _blueprintConfig = config;
        _currentMagazineAmmo = magazineAmmo;
        _currentReserveAmmo = reserveAmmo;
    }
}