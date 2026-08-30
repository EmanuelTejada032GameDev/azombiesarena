using System;

[Serializable]
public class WeaponInstanceState
{
    private WeaponDataConfig _blueprintConfig;

    private int _currentMagazineAmmo;
    private int _currentReserveAmmo;

    public WeaponDataConfig BlueprintConfig => _blueprintConfig;
    public int CurrentMagazineAmmo { get => _currentMagazineAmmo; set => _currentMagazineAmmo = value; }
    public int CurrentReserveAmmo { get => _currentReserveAmmo; set => _currentReserveAmmo = value; }

    /// <summary>
    /// Constructor to create a brand-new weapon instance state with full ammo.
    /// Used when buying a fresh gun from a wall-buy or pulling it from the Mystery Box.
    /// </summary>
    public WeaponInstanceState(WeaponDataConfig config)
    {
        _blueprintConfig = config;

        if (_blueprintConfig != null)
        {
            _currentMagazineAmmo = _blueprintConfig.MaxMagazineSize;
            _currentReserveAmmo = _blueprintConfig.MaxReserveAmmo;
        }
    }

    /// <summary>
    /// Constructor to create a weapon instance state with specific pre-existing ammo values.
    /// Useful for loading saved games or generating dropped weapons from dead players.
    /// </summary>
    public WeaponInstanceState(WeaponDataConfig config, int magazineAmmo, int reserveAmmo)
    {
        _blueprintConfig = config;
        _currentMagazineAmmo = magazineAmmo;
        _currentReserveAmmo = reserveAmmo;
    }
}
