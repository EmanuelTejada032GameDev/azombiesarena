using System.Collections.Generic;
using UnityEngine;

public class AudioPoolManager : MonoBehaviour
{
    private static AudioPoolManager _instance;
    public static AudioPoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AudioPoolManager");
                _instance = go.AddComponent<AudioPoolManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private List<AudioSource> _pool = new List<AudioSource>();
    private int _initialPoolSize = 10;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateAudioNewSource();
        }
    }

    private AudioSource CreateAudioNewSource()
    {
        GameObject child = new GameObject("PooledAudioSource");
        child.transform.SetParent(transform);
        AudioSource source = child.AddComponent<AudioSource>();

        // Default to 2D space for safety, the AudioEvent can override this if needed.
        source.spatialBlend = 0f;

        _pool.Add(source);
        return source;
    }

    public AudioSource GetAvailableSource()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].isPlaying)
            {
                return _pool[i];
            }
        }

        return CreateAudioNewSource();
    }
}
