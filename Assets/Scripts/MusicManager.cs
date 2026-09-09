using UnityEngine;

public class MusicManager : MonoBehaviour
{

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioEvent _audioEvent;


    private void Start()
    {
        _audioEvent.Play(_audioSource);
    }
}
