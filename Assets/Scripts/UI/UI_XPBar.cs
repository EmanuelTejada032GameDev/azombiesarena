using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_XPBar : MonoBehaviour
{
    [SerializeField] private Image _barImage;
    [SerializeField] private TMP_Text _levelText;

    private void Start()
    {
        if (XPManager.Instance != null)
        {
            XPManager.Instance.OnXPChanged += HandleXPChanged;
            Refresh();
        }
    }

    private void OnDestroy()
    {
        if (XPManager.Instance != null)
            XPManager.Instance.OnXPChanged -= HandleXPChanged;
    }

    private void HandleXPChanged(object sender, System.EventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        _barImage.fillAmount = XPManager.Instance.NormalizedXP;

        if (_levelText != null)
            _levelText.text = "Lvl " + XPManager.Instance.CurrentLevel;
    }
}