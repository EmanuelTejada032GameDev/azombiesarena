using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    private PlayerInput _inputs;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        _inputs = new PlayerInput();
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        _inputs.Disable();
    }


    public PlayerInput GetInputInstance() => _inputs;

}
