using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio Events/Simple")]
public class SimpleAudioEvent : AudioEvent
{
    [Header("Audio Configurations")]
    public AudioClip[] clips;

    public AudioMixerGroup mixerGroup;

    [Header("Juice & Variation Settings")]
    [Range(0f, 1f)] public float minVolume = 1f;
    [Range(0f, 1f)] public float maxVolume = 1f;

    [Range(0f, 2f)] public float minPitch = 1f;
    [Range(0f, 2f)] public float maxPitch = 1f;

    [Header("Spatial Settings")]
    [Range(0f, 1f)] public float spatialBlend = 0f;

    public override void Play(AudioSource source)
    {
        if (clips.Length == 0) return;

        source.clip = clips[Random.Range(0, clips.Length)];

        source.outputAudioMixerGroup = mixerGroup;

        source.volume = Random.Range(minVolume, maxVolume);
        source.pitch = Random.Range(minPitch, maxPitch);

        source.spatialBlend = spatialBlend;

        source.Play();
    }
}
