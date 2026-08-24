using TMPro;
using UnityEngine;

public class UI_WaveInfo : MonoBehaviour
{
    [SerializeField] private bool _showUI = true;

    [Header("Data References")]
    [SerializeField] private IntSO _waveVariable;
    [SerializeField] private FloatSO _timerVariable;

    [Header("UI Text Fields")]
    [SerializeField] private TextMeshProUGUI _waveText;
    [SerializeField] private TextMeshProUGUI _countdownText;

    private void Start()
    {
        if (!_showUI)
        {
            gameObject.SetActive(false);
        }
        _countdownText.SetText("");
    }

    private void OnEnable()
    {
        if (!_showUI) return;

        if (_waveVariable != null)
        {
            _waveVariable.OnValueChanged += UpdateWaveText;
            UpdateWaveText(_waveVariable.Value);
        }

        if (_timerVariable != null)
        {
            _timerVariable.OnValueChanged += UpdateTimerText;
        }
    }

    private void OnDisable()
    {
        if (_waveVariable != null) _waveVariable.OnValueChanged -= UpdateWaveText;
        if (_timerVariable != null) _timerVariable.OnValueChanged -= UpdateTimerText;
    }

    private void UpdateWaveText(int currentWave)
    {
        if (_waveText != null) _waveText.text = $"WAVE {currentWave}";
    }

    private void UpdateTimerText(float secondsLeft)
    {
        if (_countdownText == null) return;

        if (secondsLeft <= 0.01f)
        {
            _countdownText.text = "";
            return;
        }

        int displaySeconds = Mathf.CeilToInt(secondsLeft);
        _countdownText.text = $"Next Wave in: {displaySeconds}s";
    }
}


