using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [SerializeField] private IntSO pointsVariable;

    [SerializeField] private int startingPoints = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ResetPoints();
    }

    public void ResetPoints()
    {
        if (pointsVariable != null)
        {
            pointsVariable.Value = startingPoints;
        }
    }

    public void AddPoints(int amount)
    {
        if (pointsVariable != null)
        {
            pointsVariable.Value += amount;
        }
    }

    public bool TrySpendPoints(int amount)
    {
        if (pointsVariable == null || pointsVariable.Value < amount)
        {
            return false; 
        }

        pointsVariable.Value -= amount;
        return true;
    }
}
