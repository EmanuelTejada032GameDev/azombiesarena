using UnityEngine;

public abstract class MagnetCollectible : MonoBehaviour
{
    [Header("Attraction")]
    [SerializeField] private float _attractionRange = 4f;
    [SerializeField] private float _collectionDistance = 0.65f;
    [SerializeField] private float _flySpeed = 12f;
    [SerializeField] private float _targetHeightOffset = 1f;

    [Header("Visuals")]
    [SerializeField] private Floater _floater;
    [SerializeField] private TrailRenderer _trailRenderer;

    private int _amount = 1;
    private bool _isFlyingToPlayer;
    private bool _hasBeenCollected;

    public void Initialize(int amount)
    {
        _amount = Mathf.Max(0, amount);
    }

    private void Awake()
    {
        if (_floater == null)
            _floater = GetComponent<Floater>();

        if (_trailRenderer == null)
            _trailRenderer = GetComponent<TrailRenderer>();

        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = false;
            _trailRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if (_hasBeenCollected || Player.Instance == null)
            return;

        Transform playerTransform = Player.Instance.transform;
        Vector3 targetPosition = playerTransform.position + Vector3.up * _targetHeightOffset;

        if (!_isFlyingToPlayer)
        {
            float attractionRange = Mathf.Max(0f, PlayerStats.Get(PlayerStats.StatType.PickupRange, _attractionRange));
            if ((targetPosition - transform.position).sqrMagnitude > attractionRange * attractionRange)
                return;

            BeginAttraction();
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, _flySpeed * Time.deltaTime);

        if ((targetPosition - transform.position).sqrMagnitude <= _collectionDistance * _collectionDistance)
            Collect();
    }

    private void BeginAttraction()
    {
        _isFlyingToPlayer = true;

        if (_floater != null)
            _floater.StopFloating();

        if (_trailRenderer != null)
        {
            _trailRenderer.Clear();
            _trailRenderer.enabled = true;
            _trailRenderer.emitting = true;
        }
    }

    private void Collect()
    {
        _hasBeenCollected = true;

        if (_trailRenderer != null)
            _trailRenderer.emitting = false;

        GrantReward(_amount);
        Destroy(gameObject);
    }

    protected abstract void GrantReward(int amount);
}
