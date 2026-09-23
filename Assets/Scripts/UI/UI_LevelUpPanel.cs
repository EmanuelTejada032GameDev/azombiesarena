using System.Collections.Generic;
using UnityEngine;

public class UI_LevelUpPanel : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private UI_UpgradeCard[] _cards;

    private void Start()
    {
        if (_panel != null)
            _panel.SetActive(false);

        if (LevelUpManager.Instance != null)
            LevelUpManager.Instance.OnUpgradesOffered += HandleUpgradesOffered;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (LevelUpManager.Instance != null)
            LevelUpManager.Instance.OnUpgradesOffered -= HandleUpgradesOffered;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleUpgradesOffered(List<UpgradeSO> upgrades)
    {
        _panel.SetActive(true);

        for (int i = 0; i < _cards.Length; i++)
        {
            if (i < upgrades.Count)
            {
                _cards[i].gameObject.SetActive(true);
                _cards[i].Setup(upgrades[i]);
            }
            else
            {
                _cards[i].gameObject.SetActive(false);
            }
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.LevelUp)
            _panel.SetActive(false);
    }
}