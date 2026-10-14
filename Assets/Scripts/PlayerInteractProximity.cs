using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractProximity : MonoBehaviour
{
    [Header("Radar Settings")]
    [SerializeField] private float INTERACT_RANGE = 3f;
    [SerializeField] private Vector3 _interactionOffset = new Vector3(0f, 1f, 0f);

    [Header("Target Stability")]
    [Tooltip("A new interactable must be this much closer than the current one to steal focus (prevents prompt flicker).")]
    [SerializeField] private float _switchDistanceMargin = 0.5f;

    private IInteractable _currentTarget;
    private string _displayedText;

    private void Update()
    {
        IInteractable closestInteractable = GetInteractableObject();
        UpdatePrompt(closestInteractable);

#if ENABLE_INPUT_SYSTEM
        bool isFKeyDown = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
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

    /// <summary>
    /// Shows or hides the HUD prompt, only touching the HUD when the text actually changes.
    /// </summary>
    private void UpdatePrompt(IInteractable target)
    {
        string newText = null;

        if (target != null && target.CanDoInteractAction(IInteractable.InteractAction.Primary))
        {
            var textDict = target.GetInteractTextDictionary();
            if (textDict != null && textDict.TryGetValue(IInteractable.InteractAction.Primary, out string text))
            {
                newText = text;
            }
        }

        if (newText == _displayedText) return;
        _displayedText = newText;

        if (UI_InteractionHUD.Instance == null) return;

        if (newText != null)
            UI_InteractionHUD.Instance.DisplayPrompt(newText);
        else
            UI_InteractionHUD.Instance.HidePrompt();
    }

    /// <summary>
    /// Returns the interactable in range, keeping the current one unless another is clearly closer.
    /// </summary>
    public IInteractable GetInteractableObject()
    {
        Vector3 detectionCenter = transform.position + _interactionOffset;
        Collider[] colliderArray = Physics.OverlapSphere(detectionCenter, INTERACT_RANGE);

        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;
        bool currentTargetStillInRange = false;
        float currentTargetDistance = float.MaxValue;

        foreach (Collider collider in colliderArray)
        {
            if (!collider.TryGetComponent(out IInteractable interactable)) continue;

            // Unity-destroyed objects compare as null through the interface cast
            if (interactable is Object unityObject && unityObject == null) continue;

            float distance = Vector3.Distance(detectionCenter, interactable.GetTransform().position);

            if (interactable == _currentTarget)
            {
                currentTargetStillInRange = true;
                currentTargetDistance = distance;
            }

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        // Hysteresis: only switch when the new candidate is significantly closer
        if (currentTargetStillInRange && closestInteractable != _currentTarget &&
            closestDistance > currentTargetDistance - _switchDistanceMargin)
        {
            closestInteractable = _currentTarget;
        }

        _currentTarget = closestInteractable;
        return closestInteractable;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + _interactionOffset, INTERACT_RANGE);
    }
}
