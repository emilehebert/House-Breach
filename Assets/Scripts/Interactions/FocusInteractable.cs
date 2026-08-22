using UnityEngine;
using UnityEngine.Events;
using Cinemachine;

[RequireComponent(typeof(Collider))]
public class FocusInteractable : MonoBehaviour, IInteractable, IFocusable
{
    [SerializeField] private CinemachineVirtualCamera focusCamera;
    [SerializeField] private int focusPriority = 30;
    [SerializeField] private int defaultPriority = 0;
    [SerializeField] private bool showCursor = true;
    [SerializeField] private InteractionSystem interactionSystem;

    [Header("Interaction Settings")]
    [SerializeField] private UnityEvent onFocusEnter;
    [SerializeField] private UnityEvent onFocusExit;
    [SerializeField] private string interactText = "Interact";

    private bool isFocused = false;

    public bool WantsCursorVisible => showCursor;

    private void Start()
    {
        if (focusCamera != null)
            focusCamera.Priority = defaultPriority;
    }

    private void OnDisable()
    {
        if (isFocused)
            ExitFocus(popFromStack: true);
    }

    public string GetInteractText() => interactText;

    public AnimationClip GetInteractAnimation() => null;

    public void Interact()
    {
        if (isFocused) return;
        EnterFocus();
    }

    public void OnCancelFocus() => ExitFocus(popFromStack: false);

    private void EnterFocus()
    {
        isFocused = true;

        if (focusCamera != null)
            focusCamera.Priority = focusPriority;

        if (PlayerDriver.Instance != null)
            PlayerDriver.Instance.SetInputEnabled(false);

        // Cursor is unlocked for UI while focused (showCursor) — without this,
        // moving the mouse to click a screen's UI would also spin the focus
        // camera underneath it.
        CameraLookGate.SetLocked(true);

        if (interactionSystem != null)
            interactionSystem.SetInteractionEnabled(false);

        onFocusEnter?.Invoke();

        FocusStack.Push(this);
    }

    private void ExitFocus(bool popFromStack)
    {
        isFocused = false;

        if (focusCamera != null)
            focusCamera.Priority = defaultPriority;

        if (PlayerDriver.Instance != null)
            PlayerDriver.Instance.SetInputEnabled(true);

        CameraLookGate.SetLocked(false);

        if (interactionSystem != null)
            interactionSystem.SetInteractionEnabled(true);

        onFocusExit?.Invoke();

        if (popFromStack)
            FocusStack.Pop(this);
    }
}