using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    [SerializeField] private Collider _collider;

    private PlayerMeleeController _controller;

    public void Bind(PlayerMeleeController controller)
    {
        _controller = controller;
        SetActive(false);
    }

    public void SetActive(bool isActive)
    {
        if (_collider != null)
            _collider.enabled = isActive;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_controller != null)
            _controller.RegisterHit(other);
    }
}