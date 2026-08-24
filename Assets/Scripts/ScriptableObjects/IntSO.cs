using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "IntSO", menuName = "Scriptable Objects/IntSO")]
public class IntSO : ScriptableObject
{
    [SerializeField] private int value;

    public UnityAction<int> OnValueChanged;

    public int Value
    {
        get => value;
        set
        {
            if (this.value == value) return;
            this.value = value;

            OnValueChanged?.Invoke(this.value);
        }
    }
}
