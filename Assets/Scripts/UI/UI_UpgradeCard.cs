using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_UpgradeCard : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Button _button;

    private UpgradeSO _upgrade;

    private void Awake()
    {
        _button.onClick.AddListener(HandleClick);
    }

    public void Setup(UpgradeSO upgrade)
    {
        _upgrade = upgrade;

        _nameText.text = upgrade.DisplayName;
        _descriptionText.text = upgrade.Description;

        if (_icon != null)
        {
            _icon.sprite = upgrade.Icon;
            _icon.enabled = upgrade.Icon != null;
        }
    }

    private void HandleClick()
    {
        if (_upgrade == null)
            return;

        LevelUpManager.Instance.SelectUpgrade(_upgrade);
    }
}