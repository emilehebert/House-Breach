using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public interface IInteractable
{
    void Interact();
    string GetInteractText();
    AnimationClip GetInteractAnimation();
}

public class InteractionSystem : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionLayer = 0;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI interactionText;

    [Header("Cursor")]
    [SerializeField] private Image cursorImage;
    [SerializeField] private Sprite defaultCursorSprite;
    [SerializeField] private Sprite interactCursorSprite;

    [Header("Input")]
    [Tooltip("Nouvelle action Input System pour interagir (touche E). Crée-la dans ton asset .inputactions, comme moveAction/jumpAction sur le PlayerController.")]
    [SerializeField] private InputActionReference interactAction;

    private string interactClipName = "Interact"; // Name of the slot to swap

    private Animator playerAnimator;
    private AnimatorOverrideController playerOverride;

    private Camera playerCamera;
    private Inventory inventory;
    private IInteractable currentInteractable;

    void Start()
    {
        playerAnimator = GetComponentInChildren<Animator>();
        playerCamera = GetComponentInChildren<Camera>();
        inventory = GetComponent<Inventory>();

        // Grab the CLONE that Inventory.cs just created
        playerOverride = playerAnimator.runtimeAnimatorController as AnimatorOverrideController;
    }

    void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();

        // Bring the crosshair back when interaction turns back on (e.g.
        // leaving a screen's focus) — it was hidden entirely in OnDisable.
        if (cursorImage != null)
            cursorImage.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        // Système mis en pause (ex: focus sur un écran) : on nettoie l'UI
        // immédiatement pour ne pas laisser un prompt ou un curseur figé à l'écran.
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);

        // Hide the crosshair entirely rather than just resetting its sprite —
        // there's nothing to interact with while this system is disabled, so
        // it shouldn't stay on screen.
        if (cursorImage != null)
            cursorImage.gameObject.SetActive(false);

        currentInteractable = null;
    }

    void Update()
    {
        UpdateUI(CheckForInteractable());
        if (currentInteractable != null)
        {
            bool interactPressed = interactAction != null && interactAction.action.WasPressedThisFrame();

            if (interactPressed && !inventory.isUsingItem)
                DoInteract();
        }
    }

    /// <summary>
    /// Active/désactive complètement ce système (raycast, prompt, input E).
    /// Utilisé par FocusInteractable pendant le focus caméra, mais réutilisable
    /// par n'importe quel autre système qui doit temporairement bloquer les interactions.
    /// </summary>
    public void SetInteractionEnabled(bool value)
    {
        enabled = value; // déclenche automatiquement OnEnable / OnDisable ci-dessus
    }

    private bool CheckForInteractable()
    {
        Vector3 center = new Vector3(Screen.width, Screen.height, 0f) * 0.5f;
        var ray    = playerCamera.ScreenPointToRay(center);

        if (Physics.Raycast(ray, out var hit, interactionRange, interactionLayer))
        {
            currentInteractable = hit.transform.GetComponent<IInteractable>();
            return currentInteractable != null;
        }

        currentInteractable = null;
        return false;
    }

    private void UpdateUI(bool isLooking)
    {
        interactionText.gameObject.SetActive(isLooking);
        if (isLooking)
            interactionText.text = currentInteractable.GetInteractText();

        if (cursorImage != null)
            cursorImage.sprite = isLooking ? interactCursorSprite : defaultCursorSprite;
    }

    private void DoInteract()
    {
        if (currentInteractable == null) return;

        // 1. Get the animation from the door/button
        AnimationClip newClip = currentInteractable.GetInteractAnimation();
        
        // 2. Swap ONLY the interact slot
        if (newClip != null && playerOverride != null)
        {
            playerOverride[interactClipName] = newClip;
            
            // 3. Play the animation
            playerAnimator.SetTrigger("Interact");
        }

        // 4. Run the logic
        currentInteractable.Interact();
        
        // Hide UI
        if(!CheckForInteractable()) interactionText.gameObject.SetActive(false);
    }
}