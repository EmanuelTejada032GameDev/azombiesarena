using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

public class TimeDebugger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float slowMotionScale = 0.15f; // 15% of normal speed

    private bool _isSlowMotionActive = false;
    private PlayerInput _inputs;

    private void Start()
    {
        _inputs = Player.Instance.GetInputInstance();

        _inputs.Player.Test.performed += OnTestPerformed;
    }

    private void OnDestroy()
    {
        if (_inputs != null)
        {
            _inputs.Player.Test.performed -= OnTestPerformed;
        }
    }

    private void OnTestPerformed(InputAction.CallbackContext context)
    {
        _isSlowMotionActive = !_isSlowMotionActive;

        if (_isSlowMotionActive)
        {
            Time.timeScale = slowMotionScale;

            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
        else
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f; 
        }
    }
}
