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

// Lives on its own object, NOT on either player character — so it stays
// active and keeps working no matter which body (office, candidate, future
// characters) is currently possessed. Animator and inventory are looked
// up fresh from PlayerDriver.Instance.CurrentBody each time instead of
// being fixed to one GameObject, since each character keeps its own.
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
    [Tooltip("Nouvelle action Input System pour interagir (touche E).")]
    [SerializeField] private InputActionReference interactAction;

    private string interactClipName = "Interact"; // Name of the slot to swap

    private IInteractable currentInteractable;

    void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();

        // Mirrors OnDisable()'s cleanup — without this, the cursor image
        // stays hidden forever after the first focus/unfocus, since nothing
        // else in this script ever re-activates its GameObject.
        if (cursorImage != null)
            cursorImage.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        // Système mis en pause (ex: focus sur un écran) : on nettoie l'UI
        // immédiatement pour ne pas laisser un prompt ou un curseur figé à l'écran.
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);

        if (cursorImage != null)
            cursorImage.gameObject.SetActive(false);

        currentInteractable = null;
    }

    void Update()
    {
        if (PlayerDriver.Instance == null || PlayerDriver.Instance.CurrentBody == null) return;

        UpdateUI(CheckForInteractable());

        if (currentInteractable != null)
        {
            bool interactPressed = interactAction != null && interactAction.action.WasPressedThisFrame();

            Inventory inventory = PlayerDriver.Instance.CurrentBody.GameObject.GetComponent<Inventory>();
            bool usingItem = inventory != null && inventory.isUsingItem;

            if (interactPressed && !usingItem)
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
        Camera cam = Camera.main;
        if (cam == null)
        {
            currentInteractable = null;
            return false;
        }

        Vector3 center = new Vector3(Screen.width, Screen.height, 0f) * 0.5f;
        var ray = cam.ScreenPointToRay(center);

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

        AnimationClip newClip = currentInteractable.GetInteractAnimation();

        if (newClip != null)
        {
            Animator animator = PlayerDriver.Instance.CurrentBody.GameObject.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
            {
                overrideController[interactClipName] = newClip;
                animator.SetTrigger("Interact");
            }
        }

        // Run the logic
        currentInteractable.Interact();

        // Hide UI
        if (!CheckForInteractable()) interactionText.gameObject.SetActive(false);
    }
}