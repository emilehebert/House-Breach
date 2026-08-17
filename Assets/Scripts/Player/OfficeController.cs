using UnityEngine;
using UnityEngine.InputSystem;

// Simple movement for the office character — just walking, no jump,
// crouch, slopes, or air control. The candidate keeps using the full
// PlayerController.cs for actual mission gameplay.
[RequireComponent(typeof(CharacterController))]
public class OfficeController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float gravity = -20f;

    [Header("Camera")]
    public Transform camTransform;

    [Header("Input Actions")]
    public InputActionReference moveAction;
    [Tooltip("Exits an active focus (chair/screen). Same LeftShift action as before.")]
    public InputActionReference backAction;

    // Same nested-lock pattern as PlayerController, so overlapping freezes
    // (e.g. seated AND focused on a screen) still resolve correctly.
    private int inputLockCount = 0;
    public bool InputEnabled => inputLockCount <= 0;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector2 moveInput;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (moveAction != null) moveAction.action.Enable();
        if (backAction != null) backAction.action.Enable();
    }

    void OnDestroy()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (backAction != null) backAction.action.Disable();
    }

    void Update()
    {
        if (GameManager.IsPaused) return;

        HandleFocusBackInput();
        HandleInput();
        HandleMovement();
        ApplyGravity();
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
            return;
        }

        moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
    }

    void HandleMovement()
    {
        if (camTransform == null) return;

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        move = camTransform.forward * move.z + camTransform.right * move.x;
        move.y = 0f;

        controller.Move(move.normalized * walkSpeed * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Same shape as PlayerController's — CandidateFocusHandoff calls this.
    public void SetInputEnabled(bool value)
    {
        inputLockCount = value ? Mathf.Max(0, inputLockCount - 1) : inputLockCount + 1;

        if (!InputEnabled)
        {
            moveInput = Vector2.zero;
            if (moveAction != null) moveAction.action.Disable();
        }
        else
        {
            if (moveAction != null) moveAction.action.Enable();
        }
    }
}