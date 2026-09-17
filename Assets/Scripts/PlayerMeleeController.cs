using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeController : MonoBehaviour
{
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private MeleeHitbox _hitbox;

    [Header("Damage")]
    [SerializeField] private int _damage = 50;
    [SerializeField] private int _maxTargetsPerSwing = 3;
    [SerializeField] private LayerMask _hitLayers;

    [Header("Timing")]
    [SerializeField] private float _windupDuration = 0.05f;
    [SerializeField] private float _activeDuration = 0.12f;
    [SerializeField] private float _recoveryDuration = 0.4f;

    [Header("Stamina Cost")]
    [SerializeField] private float _staminaCost = 25f;

    [Header("VFX")]
    [SerializeField] private GameObject _slashVFXPrefab;
    [SerializeField] private Transform _vfxSpawnPoint;

    private PlayerInput _inputs;

    private enum MeleeState { Ready, Windup, Active, Recovery }
    private MeleeState _state = MeleeState.Ready;
    private float _stateTimer;

    private readonly HashSet<IDamagable> _damagedThisSwing = new HashSet<IDamagable>();
    private int _hitCountThisSwing;

    private void Start()
    {
        _inputs = Player.Instance.GetInputInstance();

        if (_hitbox != null)
            _hitbox.Bind(this);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
            return;

        switch (_state)
        {
            case MeleeState.Ready:
                HandleReady();
                break;
            case MeleeState.Windup:
                TickTimer(BeginActive);
                break;
            case MeleeState.Active:
                TickTimer(BeginRecovery);
                break;
            case MeleeState.Recovery:
                TickTimer(BeginReady);
                break;
        }
    }

    private void HandleReady()
    {
        if (!_inputs.Player.Melee.WasPressedThisFrame())
            return;

        if (!_playerMovement.TrySpendStamina(_staminaCost))
            return;

        BeginWindup();
    }

    private void TickTimer(System.Action onComplete)
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
            onComplete();
    }

    private void BeginWindup()
    {
        _state = MeleeState.Windup;
        _stateTimer = _windupDuration;
        SpawnSlashVFX();
    }

    private void BeginActive()
    {
        _state = MeleeState.Active;
        _stateTimer = _activeDuration;
        _damagedThisSwing.Clear();
        _hitCountThisSwing = 0;
        _hitbox.SetActive(true);
    }

    private void BeginRecovery()
    {
        _state = MeleeState.Recovery;
        _stateTimer = _recoveryDuration;
        _hitbox.SetActive(false);
    }

    private void BeginReady()
    {
        _state = MeleeState.Ready;
    }

    public void RegisterHit(Collider other)
    {
        if (_state != MeleeState.Active)
            return;

        if (_hitCountThisSwing >= _maxTargetsPerSwing)
            return;

        if (((1 << other.gameObject.layer) & _hitLayers) == 0)
            return;

        IDamagable damageable = other.GetComponentInParent<IDamagable>();
        if (damageable == null)
            return;

        if (_damagedThisSwing.Contains(damageable))
            return;

        _damagedThisSwing.Add(damageable);
        _hitCountThisSwing++;

        damageable.TakeDamage(_damage);
    }

    private void SpawnSlashVFX()
    {
        if (_slashVFXPrefab == null)
            return;

        Transform spawn = _vfxSpawnPoint != null ? _vfxSpawnPoint : transform;
        GameObject vfx = Instantiate(_slashVFXPrefab, spawn.position, spawn.rotation);
        Destroy(vfx, 1f);
    }
}