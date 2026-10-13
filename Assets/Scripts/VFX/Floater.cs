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

    private Vector3 _startLocalPosition;
    private Quaternion _startLocalRotation;
    private float _phase;
    private float _yaw;

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
        float height = _hoverHeight;

        if (_bob)
            height += Mathf.Sin(Time.time * _bobSpeed + _phase) * _bobAmplitude;

        transform.localPosition = _startLocalPosition + Vector3.up * height;

        if (_spin)
            _yaw += _spinSpeed * Time.deltaTime;

        transform.localRotation = Quaternion.Euler(0f, _yaw, 0f) * _startLocalRotation;
    }

    public void RestoreBasePose()
    {
        transform.localPosition = _startLocalPosition;
        transform.localRotation = _startLocalRotation;
    }
}