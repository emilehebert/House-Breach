using UnityEngine;
using UnityEngine.Events;

public class EventInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private AnimationClip animationClip;
    [SerializeField] private bool onlyOnce = false;
    [SerializeField] private float cooldownDuration = 0f;

    [Header("Interaction Settings")]
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private string interactText = "Use";

    private bool hasBeenUsed = false;
    private float lastUseTime = -Mathf.Infinity;

    public void Interact()
    {
        // If it's one-shot and already used, do nothing
        if (onlyOnce && hasBeenUsed) return;

        // If still in cooldown, do nothing
        if (Time.time < lastUseTime + cooldownDuration) return;

        // Fire event
        onInteract?.Invoke();

        // Record usage
        lastUseTime = Time.time;
        hasBeenUsed = true;
    }

    public string GetInteractText()
    {
        if (onlyOnce && hasBeenUsed) return null;
        if (Time.time < lastUseTime + cooldownDuration) return null;
        return interactText;
    }

    public AnimationClip GetInteractAnimation()
    {
        if (onlyOnce && hasBeenUsed) return null;
        if (Time.time < lastUseTime + cooldownDuration) return null;
        return animationClip;
    }
}
