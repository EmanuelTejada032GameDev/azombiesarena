using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UI_WeaponsHUD : MonoBehaviour
{
    [Header("UI Visual Component References")]
    [SerializeField] private Transform _weaponsHUDContainer;
    [SerializeField] private TextMeshProUGUI _magazineText;
    [SerializeField] private TextMeshProUGUI _reserveText;
    [SerializeField] private Image _weaponIconImage;

    private Weapon _trackedWeaponInstance;

    private void Awake()
    {
        Hide();
    }

    private void Start()
    {
        if (PlayerWeaponHandler.Instance != null)
        {
            PlayerWeaponHandler.Instance.OnWeaponSwapped += PlayerWeaponHandler_OnWeaponSwapped;
        }

        if(GameManager.Instance != null)
        {
            GameManager.Instance.OnNewMatch += GameManager_OnNewMatch;
        }
    }

    private void GameManager_OnNewMatch(object sender, EventArgs e)
    {
        Show();
    }

    private void PlayerWeaponHandler_OnWeaponSwapped(Weapon weapon)
    {
        UpdateActiveWeaponListener(weapon);
    }

    public void UpdateActiveWeaponListener(Weapon newWeapon)
    {
        UnsubscribeFromCurrentWeapon();

        _trackedWeaponInstance = newWeapon;

        if (_trackedWeaponInstance == null)
        {
            ClearHUDVisuals();
            return;
        }

        _trackedWeaponInstance.OnAmmoChanged += Weapon_OnAmmoChanged;

        RefreshHUDVisualsComplete();
    }

    private void Weapon_OnAmmoChanged(object sender, EventArgs e)
    {
        RefreshAmmoDisplay();
    }

    private void RefreshAmmoDisplay()
    {
        if (_trackedWeaponInstance == null || _trackedWeaponInstance.State == null) return;

        _magazineText.text = _trackedWeaponInstance.State.CurrentMagazineAmmo.ToString();
        _reserveText.text = _trackedWeaponInstance.State.CurrentReserveAmmo.ToString();
    }

    private void RefreshHUDVisualsComplete()
    {
        if (_trackedWeaponInstance == null || _trackedWeaponInstance.State == null) return;

        RefreshAmmoDisplay();

        Sprite iconSprite = _trackedWeaponInstance.Config.WeaponIconSprite;

        if (iconSprite != null)
        {
            _weaponIconImage.sprite = iconSprite;
            _weaponIconImage.enabled = true; 
        }
        else
        {
            _weaponIconImage.enabled = false; 
        }
    }

    private void ClearHUDVisuals()
    {
        _magazineText.text = "0";
        _reserveText.text = "0";
        _weaponIconImage.enabled = false;
    }

    private void UnsubscribeFromCurrentWeapon()
    {
        if (_trackedWeaponInstance != null)
        {
            _trackedWeaponInstance.OnAmmoChanged -= Weapon_OnAmmoChanged;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromCurrentWeapon();

        if (PlayerWeaponHandler.Instance != null)
        {
            PlayerWeaponHandler.Instance.OnWeaponSwapped -= PlayerWeaponHandler_OnWeaponSwapped;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewMatch -= GameManager_OnNewMatch;
        }

    }

    private void Show()
    {
        _weaponsHUDContainer.gameObject.SetActive(true);
    }

    private void Hide()
    {
        _weaponsHUDContainer.gameObject.SetActive(false);
    }
}
