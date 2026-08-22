using UnityEngine;
using UnityEngine.InputSystem;

// The ONE thing that reads input in the whole game. Never turns off,
// never destroyed, never duplicated — there's only one driver, period.
// Switching characters means pointing it at a different PlayerController, not
// creating/destroying/toggling controllers.
public class PlayerDriver : MonoBehaviour
{
    public static PlayerDriver Instance { get; private set; }

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference runAction;
    public InputActionReference crouchAction;
    [Tooltip("Exits an active focus (chair/screen). LeftShift.")]
    public InputActionReference backAction;

    [Header("Starting Body")]
    [Tooltip("Whoever should be controlled the moment this scene starts. Drag a component that implements IPossessable (PlayerController or OfficeController) in here.")]
    public MonoBehaviour startingBody;

    public IPossessable CurrentBody { get; private set; }

    private int inputLockCount = 0;
    public bool InputEnabled => inputLockCount <= 0;

    private Vector2 moveInput;
    private bool jumpInput;
    private bool runInput;
    private bool crouchInput;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnableInputActions();

        if (startingBody != null && startingBody is not IPossessable)
            Debug.LogError($"PlayerDriver.startingBody ({startingBody.name}) doesn't implement IPossessable.");

        Possess(startingBody as IPossessable);
    }

    void OnDestroy()
    {
        DisableInputActions();
    }

    void Update()
    {
        if (GameManager.IsPaused) return;

        HandleFocusBackInput();
        HandleInput();

        if (CurrentBody != null)
            CurrentBody.Tick(moveInput, jumpInput, runInput, crouchInput);
    }

    void HandleFocusBackInput()
    {
        if (backAction == null) return;
        if (!backAction.action.WasPressedThisFrame()) return;

        if (FocusStack.IsActive)
            FocusStack.TryHandleCancel();
    }

    void HandleInput()
    {
        if (!InputEnabled)
        {
            moveInput = Vector2.zero;
            jumpInput = false;
            runInput = false;
            crouchInput = false;
            return;
        }

        moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        jumpInput = jumpAction != null && jumpAction.action.WasPressedThisFrame();
        runInput = runAction != null && runAction.action.IsPressed();
        crouchInput = crouchAction != null && crouchAction.action.IsPressed();
    }

    // Call this to switch who's being controlled. Handles the camera swap
    // too — old body's camera off, new body's camera on. Nothing else
    // needs to touch cameras or Instance references.
    public void Possess(IPossessable newBody)
    {
        if (newBody == CurrentBody) return;

        if (CurrentBody != null)
            CurrentBody.SetCameraActive(false);

        CurrentBody = newBody;

        if (CurrentBody != null)
            CurrentBody.SetCameraActive(true);
    }

    void EnableInputActions()
    {
        moveAction?.action.Enable();
        jumpAction?.action.Enable();
        runAction?.action.Enable();
        crouchAction?.action.Enable();
        backAction?.action.Enable();
    }

    void DisableInputActions()
    {
        moveAction?.action.Disable();
        jumpAction?.action.Disable();
        runAction?.action.Disable();
        crouchAction?.action.Disable();
        backAction?.action.Disable();
    }

    // Same nested-lock shape as before — pause, sitting, etc. can all
    // freeze input without stepping on each other. Also directly disables
    // the shared input actions (not just a local flag), so any OTHER
    // script reading these same actions directly also sees "no input".
    public void SetInputEnabled(bool value)
    {
        bool wasEnabled = InputEnabled;

        inputLockCount = value
            ? Mathf.Max(0, inputLockCount - 1)
            : inputLockCount + 1;

        bool isEnabledNow = InputEnabled;
        if (wasEnabled == isEnabledNow) return; // no real change (another lock level still active)

        if (!isEnabledNow)
        {
            moveInput = Vector2.zero;
            jumpInput = false;
            runInput = false;
            crouchInput = false;

            moveAction?.action.Disable();
            jumpAction?.action.Disable();
            runAction?.action.Disable();
            crouchAction?.action.Disable();
            // backAction stays enabled on purpose — exiting a focus needs
            // to keep working even while everything else is locked.
        }
        else
        {
            moveAction?.action.Enable();
            jumpAction?.action.Enable();
            runAction?.action.Enable();
            crouchAction?.action.Enable();
        }
    }
}