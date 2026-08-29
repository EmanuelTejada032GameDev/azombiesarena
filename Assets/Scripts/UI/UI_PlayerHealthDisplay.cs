using System;
using UnityEngine;

public class UI_PlayerHealthDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _healthContainer;
    [SerializeField] private HealthSystem _playerHealth;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject _fullHealthContainerPrefab; 
    [SerializeField] private GameObject _emptyHealthContainerPrefab; 

    private void Awake()
    {
        Hide();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
            EvaluateVisibility(GameManager.Instance.State);
        }

        if (Player.Instance != null)
        {
            _playerHealth = Player.Instance.GetComponent<HealthSystem>();
            _playerHealth.OnDamaged += PlayerHealthOnDamaged;
            _playerHealth.OnHealed += PlayerHealthOnHealed;
            
            UpdateHealthUI();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged;
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnDamaged -= PlayerHealthOnDamaged;
            _playerHealth.OnHealed -= PlayerHealthOnHealed;
        }
    }

    private void GameManager_OnStateChanged(GameState newState)
    {
        EvaluateVisibility(newState);
    }

    private void EvaluateVisibility(GameState state)
    {
        if (state == GameState.Playing || state == GameState.Paused)
        {
            Show();
            UpdateHealthUI(); 
        }
        else
        {
            Hide();
        }
    }

    private void PlayerHealthOnHealed(object sender, EventArgs e)
    {
        UpdateHealthUI();
    }

    private void PlayerHealthOnDamaged(object sender, EventArgs e)
    {
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (_playerHealth == null || _healthContainer == null || _fullHealthContainerPrefab == null || _emptyHealthContainerPrefab == null) return;

        foreach (Transform child in _healthContainer)
        {
            Destroy(child.gameObject);
        }

        int currentHealth = _playerHealth.GetHealth(); 
        int maxHealth = _playerHealth.MaxHealth;

        for (int i = 0; i < maxHealth; i++)
        {
            if (i < currentHealth)
            {
                Instantiate(_fullHealthContainerPrefab, _healthContainer);
            }
            else
            {
                Instantiate(_emptyHealthContainerPrefab, _healthContainer);
            }
        }
    }

    public void Show() => _healthContainer.gameObject.SetActive(true);
    public void Hide() => _healthContainer.gameObject.SetActive(false);
}
