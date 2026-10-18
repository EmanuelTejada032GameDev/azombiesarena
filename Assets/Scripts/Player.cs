using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    private PlayerInput _inputs;
    private HealthSystem _healthSystem;
    private PlayerStats _playerStats;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _inputs = new PlayerInput();
        _healthSystem = GetComponent<HealthSystem>();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;

        _playerStats = PlayerStats.Instance;
        if (_playerStats != null)
            _playerStats.OnStatChanged += HandlePlayerStatChanged;
    }

    private void GameManager_OnStateChanged(GameState state)
    {
        if (state == GameState.Playing)
            _inputs.Enable();
        else
            _inputs.Disable();
    }

    private void HandlePlayerStatChanged(PlayerStats.StatType statType)
    {
        if (statType == PlayerStats.StatType.MaxHealth && _healthSystem != null)
            _healthSystem.RefreshMaxHealthFromStats();
    }

    private void OnDisable()
    {
        _inputs.Disable();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged;

        if (_playerStats != null)
            _playerStats.OnStatChanged -= HandlePlayerStatChanged;

        if (Instance == this)
            Instance = null;
    }

    /// <summary>Returns the input actions instance owned by this player.</summary>
    public PlayerInput GetInputInstance() => _inputs;
}
