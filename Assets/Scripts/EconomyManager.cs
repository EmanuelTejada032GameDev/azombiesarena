using System;
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

        startingPoints = 5000;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewMatch += GameManager_OnNewMatch;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewMatch -= GameManager_OnNewMatch;
        }
    }

    private void GameManager_OnNewMatch(object sender, EventArgs e)
    {
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

    public bool CanAfford(int amount)
    {
        if (pointsVariable == null)
        {
            return false;
        }

        return pointsVariable.Value < amount;
    }


    // ==================== DEBUG / UTILITY ====================
    #region DebugUtility
    [ContextMenu("Set points to 0")]
    private void ResetScore() => ResetPoints();

    [ContextMenu("Add 1000")]
    private void Add1000() => AddPoints(1000);

    [ContextMenu("Add 5000")]
    private void Add5000() => AddPoints(5000);
    #endregion
}
