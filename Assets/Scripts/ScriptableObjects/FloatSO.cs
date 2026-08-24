using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "FloatSO", menuName = "Scriptable Objects/FloatSO")]
public class FloatSO : ScriptableObject
{
    [SerializeField] private float value;

    public UnityAction<float> OnValueChanged;

    public float Value
    {
        get => value;
        set
        {
            if (Mathf.Approximately(this.value, value)) return;
            this.value = value;
            OnValueChanged?.Invoke(this.value);
        }
    }
}


