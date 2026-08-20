using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectPooler _bulletPool;
    [SerializeField] private Transform _muzzle;

    public void FireTrigger()
    {
        GameObject bullet = _bulletPool.GetPooledObject();

        if (bullet != null)
        {
            bullet.transform.position = _muzzle.position;
            bullet.transform.rotation = _muzzle.rotation;

            bullet.SetActive(true);
        }
    }
}
