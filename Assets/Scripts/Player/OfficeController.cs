using UnityEngine;

// Lightweight possessable body for the office worker: walk + gravity only,
// no jump/crouch/slopes/inventory — deliberately asymmetric with PlayerBody,
// same shape as e.g. Call of Duty's RC-XD (a simpler body you can freely
// swap into/out of, main body stays wherever it was left).
[RequireComponent(typeof(CharacterController))]
public class OfficeController : MonoBehaviour, IPossessable
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float gravity = -20f;

    [Header("Camera")]
    [Tooltip("This body's own camera rig — only enabled while possessed.")]
    public GameObject cameraObject;

    [Header("State (read-only)")]
    public bool isGrounded;
    [HideInInspector] public float currentHorizontalSpeed;

    private CharacterController controller;
    private Vector3 velocity;

    public GameObject GameObject => gameObject;
    public bool IsPossessed => PlayerDriver.Instance != null && (Object)PlayerDriver.Instance.CurrentBody == this;
    public bool IsGrounded => isGrounded;
    public float CurrentHorizontalSpeed => currentHorizontalSpeed;

    void Awake() => controller = GetComponent<CharacterController>();

    // Always ticks, possessed or not — same reasoning as PlayerBody.Update():
    // nothing should glitch or pop when you switch back to this body later.
    void Update()
    {
        isGrounded = controller.isGrounded;

        if (!IsPossessed)
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }

        if (isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        if (controller.enabled)
            controller.Move(velocity * Time.deltaTime);
    }

    // Called by PlayerDriver only while this body is possessed.
    public void Tick(Vector2 moveInput, bool jumpInput, bool runInput, bool crouchInput)
    {
        // Looked up fresh (not cached) so it always resolves to THIS body's
        // own active camera, same pattern as PlayerBody.
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        move = cam.transform.forward * move.z + cam.transform.right * move.x;
        move.y = 0f;

        Vector3 horiz = move.normalized * walkSpeed;
        velocity.x = horiz.x;
        velocity.z = horiz.z;
        currentHorizontalSpeed = horiz.magnitude;
    }

    public void SetCameraActive(bool active)
    {
        if (cameraObject == null) return;

        if (active && MainCameraController.Instance != null)
            MainCameraController.Instance.CutTo(cameraObject);
        else
            cameraObject.SetActive(active);
    }
}
