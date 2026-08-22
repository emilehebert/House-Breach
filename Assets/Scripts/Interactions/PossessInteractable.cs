using UnityEngine;
using UnityEngine.Events;

// Reusable "take control of a different body" interaction — e.g. a bodycam
// monitor. Deliberately separate from FocusInteractable: that script freezes
// the player because it's for peeking through a fixed viewpoint (keyhole,
// scope, chair, terminal) while staying the same body. This one hands
// control to a DIFFERENT body instead, so it never freezes PlayerDriver —
// the whole point is for input to keep flowing, just routed elsewhere.
//
// Pushes onto FocusStack so the global Back key (PlayerDriver) can hand
// control back at any time, from anywhere — not just standing at the
// monitor — exactly like walking the candidate back would require.
[RequireComponent(typeof(Collider))]
public class PossessInteractable : MonoBehaviour, IInteractable, IFocusable
{
    [Tooltip("Must implement IPossessable (PlayerController or OfficeController).")]
    [SerializeField] private MonoBehaviour bodyToPossess;
    [Tooltip("Must implement IPossessable (PlayerController or OfficeController).")]
    [SerializeField] private MonoBehaviour bodyToReturnTo;
    [SerializeField] private string interactText = "Take control";

    [Header("Events (optional — screen fx, sfx, etc.)")]
    [SerializeField] private UnityEvent onPossessed;
    [SerializeField] private UnityEvent onReturned;

    private bool isPossessing = false;

    // Free-roam FPS control while possessing — mouse stays locked, same as
    // normal gameplay (unlike a screen/menu focus, which wants it visible).
    public bool WantsCursorVisible => false;

    public string GetInteractText() => interactText;

    public AnimationClip GetInteractAnimation() => null;

    public void Interact()
    {
        if (isPossessing) return;
        if (PlayerDriver.Instance == null || bodyToPossess is not IPossessable target) return;

        isPossessing = true;
        PlayerDriver.Instance.Possess(target);
        onPossessed?.Invoke();

        FocusStack.Push(this);
    }

    // Called by FocusStack when this is on top and Back is pressed.
    public void OnCancelFocus() => Return(popFromStack: false);

    private void OnDisable()
    {
        if (isPossessing)
            Return(popFromStack: true);
    }

    private void Return(bool popFromStack)
    {
        if (!isPossessing) return;
        isPossessing = false;

        if (PlayerDriver.Instance != null && bodyToReturnTo is IPossessable target)
            PlayerDriver.Instance.Possess(target);

        onReturned?.Invoke();

        if (popFromStack)
            FocusStack.Pop(this);
    }
}
