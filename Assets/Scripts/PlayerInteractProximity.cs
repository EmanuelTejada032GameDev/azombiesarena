using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractProximity : MonoBehaviour
{
    [Header("Radar Settings")]
    [SerializeField] private float INTERACT_RANGE = 3f;
    [SerializeField] private Vector3 _interactionOffset = new Vector3(0f, 1f, 0f);

    private void Update()
    {
        IInteractable closestInteractable = GetInteractableObject();

        if (closestInteractable != null && closestInteractable.CanDoInteractAction(IInteractable.InteractAction.Primary))
        {
            var textDict = closestInteractable.GetInteractTextDictionary();
            if (textDict != null && textDict.ContainsKey(IInteractable.InteractAction.Primary))
            {
                UI_InteractionHUD.Instance.DisplayPrompt(textDict[IInteractable.InteractAction.Primary]);
            }
        }
        else
        {
            UI_InteractionHUD.Instance.HidePrompt();
        }

#if ENABLE_INPUT_SYSTEM
        bool isFKeyDown = Keyboard.current.fKey.wasPressedThisFrame;
#else
                bool isFKeyDown = Input.GetKeyDown(KeyCode.F);
#endif

        if (isFKeyDown && closestInteractable != null)
        {
            if (closestInteractable.CanDoInteractAction(IInteractable.InteractAction.Primary))
            {
                closestInteractable.Interact(IInteractable.InteractAction.Primary, transform);
            }
        }
    }

    public IInteractable GetInteractableObject()
    {
        List<IInteractable> interactableList = new List<IInteractable>();
        Vector3 detectionCenter = transform.position + _interactionOffset;
        Collider[] colliderArray = Physics.OverlapSphere(detectionCenter, INTERACT_RANGE);

        foreach (Collider collider in colliderArray)
        {
            if (collider.TryGetComponent(out IInteractable interactable))
            {
                interactableList.Add(interactable);
            }
        }

        IInteractable closestInteractable = null;
        foreach (IInteractable interactable in interactableList)
        {
            if (closestInteractable == null)
            {
                closestInteractable = interactable;
            }
            else
            {
                if (Vector3.Distance(detectionCenter, interactable.GetTransform().position) <
                    Vector3.Distance(detectionCenter, closestInteractable.GetTransform().position))
                {
                    closestInteractable = interactable;
                }
            }
        }

        return closestInteractable;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + _interactionOffset, INTERACT_RANGE);
    }
}
