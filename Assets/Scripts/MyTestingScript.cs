using UnityEngine;
using static PlayerStats;

public class MyTestingScript : MonoBehaviour
{

    void Start()
    {
  
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("L key was pressed.");
            PlayerStats.Instance.ApplyModifier(StatType.MoveSpeed, ModifierKind.Percent, 1f);
        }
    }


}
