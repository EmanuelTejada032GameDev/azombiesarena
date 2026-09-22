using System;
using UnityEngine;

public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    public event EventHandler OnXPChanged;
    public event EventHandler OnLeveledUp;

    [Header("Curve")]
    [SerializeField] private int _baseXP = 100;
    [SerializeField] private int _xpStep = 75;

    private int _currentXP;
    private int _currentLevel = 1;
    private int _pendingLevelUps;

    public int CurrentXP => _currentXP;
    public int CurrentLevel => _currentLevel;
    public int XPForNextLevel => XPRequiredForLevel(_currentLevel);
    public int PendingLevelUps => _pendingLevelUps;
    public float NormalizedXP => (float)_currentXP / XPForNextLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private int XPRequiredForLevel(int level)
    {
        return _baseXP + (level - 1) * _xpStep;
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        _currentXP += amount;

        bool leveled = false;
        while (_currentXP >= XPForNextLevel)
        {
            _currentXP -= XPForNextLevel;
            _currentLevel++;
            _pendingLevelUps++;
            leveled = true;
        }

        OnXPChanged?.Invoke(this, EventArgs.Empty);

        if (leveled)
            OnLeveledUp?.Invoke(this, EventArgs.Empty);
    }

    public void ConsumePendingLevelUp()
    {
        if (_pendingLevelUps > 0)
            _pendingLevelUps--;
    }

    public void ResetXP()
    {
        _currentXP = 0;
        _currentLevel = 1;
        _pendingLevelUps = 0;
        OnXPChanged?.Invoke(this, EventArgs.Empty);
    }
}