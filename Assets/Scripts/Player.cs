using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    private PlayerInput _inputs;

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
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        }
    }

    private void GameManager_OnStateChanged(GameState state)
    {
        if (state == GameState.Playing)
        {
            _inputs.Enable();
        }
        else
        {
            _inputs.Disable();
        }
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        _inputs.Disable();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged;
        }
    }


    public PlayerInput GetInputInstance() => _inputs;

}
