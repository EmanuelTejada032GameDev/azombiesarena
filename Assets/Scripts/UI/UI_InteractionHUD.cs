using UnityEngine;
using TMPro; // Ensure TextMeshPro packages are loaded into the project environment

public class UI_InteractionHUD : MonoBehaviour
{
    public static UI_InteractionHUD Instance { get; private set; }

    [Header("UI Render Elements")]
    [SerializeField] private TextMeshProUGUI _promptTextField;
    [SerializeField] private GameObject _promptPanelWrapper;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ensure UI panel element defaults to completely invisible on initial game load
        HidePrompt();
    }

    public void DisplayPrompt(string displayMessage)
    {
        if (_promptTextField == null || _promptPanelWrapper == null) return;

        _promptTextField.text = displayMessage;
        _promptPanelWrapper.SetActive(true);
    }

    public void HidePrompt()
    {
        if (_promptPanelWrapper == null) return;
        _promptPanelWrapper.SetActive(false);
    }
}