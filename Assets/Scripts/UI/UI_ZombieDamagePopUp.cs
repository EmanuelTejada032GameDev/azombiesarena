using UnityEngine;
using DamageNumbersPro;

[RequireComponent(typeof(HealthSystem))]
public class UI_ZombieDamagePopup : MonoBehaviour
{
    [SerializeField] private DamageNumberMesh _normalPrefab;
    [SerializeField] private DamageNumberMesh _critPrefab;
    [SerializeField] private Vector3 _headOffset = new Vector3(0f, 2f, 0f);

    private HealthSystem _health;

    private void Awake()
    {
        _health = GetComponent<HealthSystem>();
    }

    private void OnEnable()
    {
        _health.OnDamageDealt += HandleDamageDealt;
    }

    private void OnDisable()
    {
        _health.OnDamageDealt -= HandleDamageDealt;
    }

    private void HandleDamageDealt(object sender, DamageEventArgs e)
    {
        DamageNumber prefab = e.IsCrit ? _critPrefab : _normalPrefab;
        if (prefab == null) return;

        Vector3 spawnPosition = transform.position + Vector3.Scale(_headOffset, transform.lossyScale);
        prefab.Spawn(spawnPosition, e.Amount);
    }
}