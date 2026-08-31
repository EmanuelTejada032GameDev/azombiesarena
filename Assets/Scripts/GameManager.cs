using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Current Status")]
    private GameState _state;

    public GameState State => _state;

    public event Action<GameState> OnStateChanged;
    public event EventHandler OnNewMatch;


    [Header("Scene References")]
    [SerializeField] private GameObject _gameOverUI;
    [SerializeField] private GameObject _mainMenuUI;
    [SerializeField] private GameObject _pauseMenuUI;

    [Header("Reset Target References")]
    [SerializeField] private HealthSystem _playerHealth;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private WaveSpawner _waveSpawner;


    private Vector3 _playerStartExposition;

    [Header("Cursor Settings")]
    [SerializeField] private Texture2D _uiMenuCursorTexture;
    [SerializeField] private Vector2 _uiCursorHotspot = Vector2.zero;

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

        if (_playerTransform != null)
        {
            _playerStartExposition = _playerTransform.position;
        }
    }

    private void Start()
    {
        ChangeState(GameState.MainMenu);
    }


    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDied += HandlePlayerDeath;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_state == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
            else if (_state == GameState.Paused)
            {
                ResumeGame();
            }
        }
    }

    public void ResumeGame()
    {
        if (_state == GameState.Paused)
        {
            ChangeState(GameState.Playing);
        }
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDied -= HandlePlayerDeath;
        }
    }

    public void ChangeState(GameState newState)
    {
        _state = newState;

        OnStateChanged?.Invoke(_state);

        switch (_state)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                _mainMenuUI.SetActive(true);
                _gameOverUI.SetActive(false);
                _pauseMenuUI.SetActive(false);

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.SetCursor(_uiMenuCursorTexture, _uiCursorHotspot, CursorMode.Auto);
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                _mainMenuUI.SetActive(false);
                _gameOverUI.SetActive(false);
                _pauseMenuUI.SetActive(false);
                _waveSpawner.gameObject.SetActive(true);

                Cursor.visible = false; 
                Cursor.lockState = CursorLockMode.Confined; 
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); 
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                _mainMenuUI.SetActive(false);
                _gameOverUI.SetActive(false);
                _pauseMenuUI.SetActive(true);

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.SetCursor(_uiMenuCursorTexture, _uiCursorHotspot, CursorMode.Auto);
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                _waveSpawner.gameObject.SetActive(false);
                _mainMenuUI.SetActive(false);
                _gameOverUI.SetActive(true);

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.SetCursor(_uiMenuCursorTexture, _uiCursorHotspot, CursorMode.Auto);
                break;
        }
    }

    public void StartGameFromMenu()
    {
        ChangeState(GameState.Playing);
        OnNewMatch?.Invoke(this, EventArgs.Empty);
        if (_waveSpawner != null)
        {
            _waveSpawner.gameObject.SetActive(true);
            _waveSpawner.enabled = true;

        }
    }

    public void RestartGame()
    {
        ResetMatchData();
        ChangeState(GameState.Playing);
    }

    public void GoToMainMenu()
    {
        ResetMatchData();
        ChangeState(GameState.MainMenu);
    }

    public void QuitGame()
    {
        // If playing inside the Unity Editor, stop the editor play mode
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif

        // If running a final built version of the game, close the application window
        Application.Quit();
    }

    private void ResetMatchData()
    {

        OnNewMatch?.Invoke(this, EventArgs.Empty);

        if (_playerTransform != null)
        {
            _playerTransform.position = _playerStartExposition;
        }

        if (_playerHealth != null)
        {
            _playerHealth.HealFull();
        }

        Zombie[] leftoverZombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        foreach (Zombie zombie in leftoverZombies)
        {
            Destroy(zombie.gameObject);
        }

        if (_waveSpawner != null)
        {
            _waveSpawner.gameObject.SetActive(false);
        }
    }

    private void HandlePlayerDeath(object sender, System.EventArgs e)
    {
        ChangeState(GameState.GameOver);
        if (_gameOverUI != null)
        {
            _gameOverUI.SetActive(true);
        }
    }
    
}

public enum GameState { Playing, GameOver, Paused, MainMenu }

