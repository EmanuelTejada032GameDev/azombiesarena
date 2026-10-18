using System;
using System.Collections;
using UnityEngine;

public class PlayerLevelUpTransition : MonoBehaviour
{
    [Header("Level-Up VFX")]
    [SerializeField] private GameObject _playerLevelUpVfxPrefab;
    [SerializeField] private Vector3 _playerLevelUpVfxOffset = new Vector3(0f, 1f, 0f);
    [SerializeField, Min(0f)] private float _playerLevelUpVfxLifetime = 3f;

    [Header("Zombie Knockback")]
    [SerializeField, Min(0f)] private float _zombieKnockbackRadius = 8f;
    [SerializeField, Min(0f)] private float _zombieKnockbackDistance = 2f;

    [Header("Slow Motion")]
    [SerializeField, Range(0.01f, 1f)] private float _slowMotionScale = 0.2f;
    [SerializeField, Min(0f)] private float _slowMotionBlendDuration = 0.2f;
    [SerializeField, Min(0f)] private float _slowMotionDuration = 1f;

    private Coroutine _levelUpTransitionCoroutine;

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
    }

    private void HandleLeveledUp(object sender, EventArgs e)
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing || _levelUpTransitionCoroutine != null)
            return;

        _levelUpTransitionCoroutine = StartCoroutine(LevelUpTransitionRoutine());
    }

    private IEnumerator LevelUpTransitionRoutine()
    {
        SpawnPlayerLevelUpVfx();
        PushNearbyZombies();

        float slowScale = Mathf.Clamp(_slowMotionScale, 0.01f, 1f);
        if (_slowMotionBlendDuration > 0f)
            yield return BlendTimeScale(Time.timeScale, slowScale, _slowMotionBlendDuration);
        else
            Time.timeScale = slowScale;

        if (_slowMotionDuration > 0f)
            yield return new WaitForSecondsRealtime(_slowMotionDuration);

        if (!IsGamePlaying())
        {
            _levelUpTransitionCoroutine = null;
            yield break;
        }

        if (_slowMotionBlendDuration > 0f)
            yield return BlendTimeScale(Time.timeScale, 1f, _slowMotionBlendDuration);
        else
            Time.timeScale = 1f;

        if (!IsGamePlaying())
        {
            _levelUpTransitionCoroutine = null;
            yield break;
        }

        _levelUpTransitionCoroutine = null;
        GameManager.Instance.ChangeState(GameState.LevelUp);
    }

    private bool IsGamePlaying()
    {
        return GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
    }

    private void SpawnPlayerLevelUpVfx()
    {
        if (_playerLevelUpVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(_playerLevelUpVfxPrefab, transform);
        vfx.transform.localPosition = _playerLevelUpVfxOffset;
        vfx.transform.localRotation = Quaternion.identity;

        ParticleSystem[] particleSystems = vfx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystem.MainModule mainModule = particleSystem.main;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
        }

        if (_playerLevelUpVfxLifetime > 0f)
            StartCoroutine(DestroyVfxAfterRealtime(vfx, _playerLevelUpVfxLifetime));
    }

    private IEnumerator DestroyVfxAfterRealtime(GameObject vfx, float lifetime)
    {
        yield return new WaitForSecondsRealtime(lifetime);
        if (vfx != null)
            Destroy(vfx);
    }

    private void PushNearbyZombies()
    {
        float radiusSquared = _zombieKnockbackRadius * _zombieKnockbackRadius;
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
        Vector3 sourcePosition = transform.position;

        foreach (Zombie zombie in zombies)
        {
            if (zombie == null || (zombie.transform.position - sourcePosition).sqrMagnitude > radiusSquared)
                continue;

            zombie.ApplyLevelUpKnockback(sourcePosition, _zombieKnockbackDistance);
        }
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

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Playing || _levelUpTransitionCoroutine == null)
            return;

        StopCoroutine(_levelUpTransitionCoroutine);
        _levelUpTransitionCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _zombieKnockbackRadius);
        Gizmos.DrawWireSphere(transform.position, _zombieKnockbackDistance);
    }
}
