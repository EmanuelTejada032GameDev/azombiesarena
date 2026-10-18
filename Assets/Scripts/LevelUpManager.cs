using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance { get; private set; }

    [SerializeField] private List<UpgradeCardSO> _pool = new List<UpgradeCardSO>();
    [SerializeField] private int _cardsPerLevel = 3;

    public event Action<List<UpgradeCardSO>> OnUpgradesOffered;

    [SerializeField] private HealthSystem _playerHealth;

    [Header("Level-Up Transition")]
    [SerializeField] private GameObject _playerLevelUpVfxPrefab;
    [SerializeField] private Vector3 _playerLevelUpVfxOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float _playerLevelUpVfxLifetime = 3f;
    [SerializeField] private float _zombieKnockbackRadius = 8f;
    [SerializeField] private float _zombieKnockbackDistance = 2f;
    [SerializeField, Range(0.01f, 1f)] private float _slowMotionScale = 0.2f;
    [SerializeField, Min(0f)] private float _slowMotionBlendDuration = 0.2f;
    [SerializeField, Min(0f)] private float _slowMotionDuration = 1f;

    private Coroutine _levelUpTransitionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (XPManager.Instance != null)
            XPManager.Instance.OnLeveledUp += HandleLeveledUp;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        if (XPManager.Instance != null)
            XPManager.Instance.OnLeveledUp -= HandleLeveledUp;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleGameStateChanged;

        if (_levelUpTransitionCoroutine != null)
            StopCoroutine(_levelUpTransitionCoroutine);

        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
            Time.timeScale = 1f;

        if (Instance == this)
            Instance = null;
    }

    private void HandleLeveledUp(object sender, EventArgs e)
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing || _levelUpTransitionCoroutine != null)
            return;

        _levelUpTransitionCoroutine = StartCoroutine(LevelUpTransitionRoutine());
    }

    private IEnumerator LevelUpTransitionRoutine()
    {
        Transform playerTransform = _playerHealth != null
            ? _playerHealth.transform
            : Player.Instance != null ? Player.Instance.transform : null;

        if (playerTransform != null)
        {
            if (_playerLevelUpVfxPrefab != null)
            {
                GameObject vfx = Instantiate(
                    _playerLevelUpVfxPrefab,
                    playerTransform.position + _playerLevelUpVfxOffset,
                    playerTransform.rotation);

                if (_playerLevelUpVfxLifetime > 0f)
                    StartCoroutine(DestroyVfxAfterRealtime(vfx, _playerLevelUpVfxLifetime));
            }

            PushNearbyZombies(playerTransform.position);
        }

        float slowScale = Mathf.Clamp(_slowMotionScale, 0.01f, 1f);
        if (_slowMotionBlendDuration > 0f)
            yield return BlendTimeScale(Time.timeScale, slowScale, _slowMotionBlendDuration);
        else
            Time.timeScale = slowScale;

        if (_slowMotionDuration > 0f)
            yield return new WaitForSecondsRealtime(_slowMotionDuration);

        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            _levelUpTransitionCoroutine = null;
            yield break;
        }

        if (_slowMotionBlendDuration > 0f)
            yield return BlendTimeScale(Time.timeScale, 1f, _slowMotionBlendDuration);
        else
            Time.timeScale = 1f;

        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            _levelUpTransitionCoroutine = null;
            yield break;
        }

        _levelUpTransitionCoroutine = null;
        GameManager.Instance.ChangeState(GameState.LevelUp);
        OfferNext();
    }

    private IEnumerator BlendTimeScale(float startScale, float targetScale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            Time.timeScale = Mathf.Lerp(startScale, targetScale, blend);
            yield return null;
        }

        Time.timeScale = targetScale;
    }

    private IEnumerator DestroyVfxAfterRealtime(GameObject vfx, float lifetime)
    {
        yield return new WaitForSecondsRealtime(lifetime);
        if (vfx != null)
            Destroy(vfx);
    }


    private void PushNearbyZombies(Vector3 origin)
    {
        float radiusSquared = _zombieKnockbackRadius * _zombieKnockbackRadius;
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);

        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || (zombie.transform.position - origin).sqrMagnitude > radiusSquared)
                continue;

            zombie.ApplyLevelUpKnockback(origin, _zombieKnockbackDistance);
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Playing || _levelUpTransitionCoroutine == null)
            return;

        StopCoroutine(_levelUpTransitionCoroutine);
        _levelUpTransitionCoroutine = null;
    }

    private void OfferNext()
    {
        List<UpgradeCardSO> trio = DrawUpgrades(_cardsPerLevel);
        OnUpgradesOffered?.Invoke(trio);
    }

    public void SelectUpgrade(UpgradeCardSO upgrade)
    {
        upgrade.Apply();

        if (upgrade is UpgradeSO playerCard && playerCard.StatType == PlayerStats.StatType.MaxHealth && _playerHealth != null)
            _playerHealth.RefreshMaxHealthFromStats();

        XPManager.Instance.ConsumePendingLevelUp();

        if (XPManager.Instance.PendingLevelUps > 0)
            OfferNext();
        else
            GameManager.Instance.ChangeState(GameState.Playing);
    }

    private List<UpgradeCardSO> DrawUpgrades(int count)
    {
        List<UpgradeCardSO> eligiblePool = _pool.Where(IsEligible).ToList();

        List<IGrouping<object, UpgradeCardSO>> byGroup = eligiblePool
            .GroupBy(upgrade => upgrade.GroupKey)
            .ToList();

        int drawCount = Mathf.Min(count, byGroup.Count);

        IEnumerable<IGrouping<object, UpgradeCardSO>> chosenGroups = byGroup
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(drawCount);

        List<UpgradeCardSO> result = new List<UpgradeCardSO>();
        foreach (IGrouping<object, UpgradeCardSO> group in chosenGroups)
        {
            List<UpgradeCardSO> options = group.ToList();
            result.Add(options[UnityEngine.Random.Range(0, options.Count)]);
        }

        return result;
    }

    private bool IsEligible(UpgradeCardSO card)
    {
        if (card is WeaponUpgradeSO weaponCard && weaponCard.Scope == WeaponStatsHub.ModifierScope.Category)
        {
            return PlayerWeaponHandler.Instance != null && PlayerWeaponHandler.Instance.HasCategoryInInventory(weaponCard.Category);
        }

        return true;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0,1f,0), _zombieKnockbackRadius);
        Gizmos.DrawWireSphere(transform.position + new Vector3(0,1f,0), _zombieKnockbackDistance);
    }
}
