using UnityEngine;
using TMPro;

public class UI_PointsDisplay : MonoBehaviour
{
    [SerializeField] private IntSO pointsVariable;
    [SerializeField] private TextMeshProUGUI pointsText;

    private void OnEnable()
    {
        if (pointsVariable != null)
        {
            pointsVariable.OnValueChanged += UpdateVisuals;
            UpdateVisuals(pointsVariable.Value);
        }
    }

    private void OnDisable()
    {
        if (pointsVariable != null)
        {
            pointsVariable.OnValueChanged -= UpdateVisuals;
        }
    }

    private void UpdateVisuals(int newValue)
    {
        if (pointsText != null)
        {
            pointsText.text = newValue.ToString();
            // UI polish logic and visual effects  
        }
    }
}
