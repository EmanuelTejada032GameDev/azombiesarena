using UnityEngine;
using TMPro;

public class UI_ZombieCounter : MonoBehaviour
{
    [SerializeField] private bool _showUI = true;

    [Header("Data References")]
    [SerializeField] private IntSO _zombiesRemainingVariable;
    [SerializeField] private TextMeshProUGUI _counterText;

    private void Start()
    {
        if (!_showUI)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (!_showUI) return;

        if (_zombiesRemainingVariable != null)
        {
            _zombiesRemainingVariable.OnValueChanged += UpdateCounterText;
            UpdateCounterText(_zombiesRemainingVariable.Value);
        }
    }

    private void OnDisable()
    {
        if (_zombiesRemainingVariable != null) _zombiesRemainingVariable.OnValueChanged -= UpdateCounterText;
    }

    private void UpdateCounterText(int count)
    {
        if (_counterText != null)
        {
            _counterText.text = $"EI: {count}";
        }
    }
}