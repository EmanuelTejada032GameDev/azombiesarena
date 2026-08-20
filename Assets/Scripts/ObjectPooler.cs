using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [SerializeField] private GameObject _objectPrefab;
    [SerializeField] private int _poolSize;
    [SerializeField] private List<GameObject> _pool;


    private void Start()
    {
        _pool = new List<GameObject>();

        for (int i = 0; i < _poolSize; i++)
        {
            GameObject obj = Instantiate(_objectPrefab, transform);
            obj.SetActive(false); 
            _pool.Add(obj);      
        }
    }

    public GameObject GetPooledObject()
    {
        foreach (GameObject obj in _pool)
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }

        return null;
    }

}
