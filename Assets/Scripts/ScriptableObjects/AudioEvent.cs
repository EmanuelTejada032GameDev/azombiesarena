using UnityEngine;

public abstract class AudioEvent : ScriptableObject
{
    public abstract void Play(AudioSource source);

    public void Play()
    {
        AudioSource pooledSource = AudioPoolManager.Instance.GetAvailableSource();

        Play(pooledSource);
    }
}
