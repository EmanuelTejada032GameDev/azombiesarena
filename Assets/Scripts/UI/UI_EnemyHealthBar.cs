using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private bool _showHealthBar;
    [SerializeField] private GameObject _healthBar;
    [SerializeField] private Image _barImage;
    [SerializeField] private HealthSystem _enemyHealth;
    [SerializeField] private bool _useHideOnFull = false;
    [SerializeField] private bool _useHideOnEmpty = false;

    private void Start()
    {
        _enemyHealth.OnDamaged += HandleDamaged;
        _enemyHealth.OnHealed += HandleHealed;
        if (_showHealthBar)
        {
            Show();
            Refresh();
        }
        else
        {
            Hide();
        }
    }

    private void HandleHealed(object sender, EventArgs e)
    {
        Refresh();
    }

    private void HandleDamaged(object sender, System.EventArgs e)
    {
        Refresh();
    }

    private void OnDestroy()
    {
        _enemyHealth.OnDamaged -= HandleDamaged;
        _enemyHealth.OnHealed -= HandleHealed;

    }

    private void Refresh()
    {
        _barImage.fillAmount = _enemyHealth.NormalizedHealthAmount;
        if (_useHideOnFull)
        {
            if (_enemyHealth.NormalizedHealthAmount >= 1f)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        if (_useHideOnEmpty)
        {
            if (_enemyHealth.NormalizedHealthAmount <= 0f)
            {
                Hide();
            }
        }
    }

    private void Show()
    {
        _healthBar.SetActive(true);
    }

    private void Hide()
    {
        _healthBar.SetActive(false);
    }

}
