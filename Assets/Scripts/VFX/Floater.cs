using UnityEngine;

public class Floater : MonoBehaviour
{
    [Header("Hover")]
    [SerializeField] private float _hoverHeight = 0.5f;

    [Header("Bob")]
    [SerializeField] private bool _bob = true;
    [SerializeField] private float _bobAmplitude = 0.1f;
    [SerializeField] private float _bobSpeed = 2f;

    [Header("Spin")]
    [SerializeField] private bool _spin = true;
    [SerializeField] private float _spinSpeed = 90f;

    [Header("Variation")]
    [SerializeField] private bool _randomOffset = true;

    [Header("Rise")]
    [Tooltip("Seconds taken to rise from the resting pose up to the hover height after BeginFloating.")]
    [SerializeField] private float _riseDuration = 0.6f;

    private Vector3 _startLocalPosition;
    private Quaternion _startLocalRotation;
    private float _phase;
    private float _yaw;
    private float _riseProgress = 1f;

    private void Awake()
    {
        _startLocalPosition = transform.localPosition;
        _startLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        _phase = _randomOffset ? Random.value * Mathf.PI * 2f : 0f;
        _yaw = _randomOffset ? Random.value * 360f : 0f;
    }

    private void Update()
    {
        if (_riseProgress < 1f)
        {
            _riseProgress = _riseDuration <= 0f ? 1f : Mathf.MoveTowards(_riseProgress, 1f, Time.deltaTime / _riseDuration);
        }

        float riseBlend = Mathf.SmoothStep(0f, 1f, _riseProgress);
        float height = _hoverHeight;

        if (_bob)
            height += Mathf.Sin(Time.time * _bobSpeed + _phase) * _bobAmplitude;

        transform.localPosition = _startLocalPosition + Vector3.up * (height * riseBlend);

        if (_spin)
            _yaw += _spinSpeed * Time.deltaTime;

        transform.localRotation = Quaternion.Euler(0f, _yaw, 0f) * _startLocalRotation;
    }

    public void RestoreBasePose()
    {
        transform.localPosition = _startLocalPosition;
        transform.localRotation = _startLocalRotation;
    }

    /// <summary>
    /// Uses the current pose as the resting pose, then smoothly rises to the hover height and starts bobbing/spinning.
    /// </summary>
    public void BeginFloating()
    {
        _startLocalPosition = transform.localPosition;
        _startLocalRotation = transform.localRotation;
        _riseProgress = 0f;
        enabled = true;
    }

    /// <summary>
    /// Stops floating so physics can take over the transform.
    /// </summary>
    public void StopFloating()
    {
        enabled = false;
    }
}
