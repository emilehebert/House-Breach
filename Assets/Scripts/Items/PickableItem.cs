using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickableItem : MonoBehaviour, IInteractable
{
    [SerializeField] private AnimationClip animationClip;  // assign DoorData once on prefab
    [SerializeField] public Item itemData; // assign your GenericItem (or any Item SO)

    public string GetInteractText() => itemData.GetInteractText();

    public AnimationClip GetInteractAnimation() => animationClip;

    public void Interact()
    {
        // 1) find the player’s inventory
        var inv = FindFirstObjectByType<Inventory>();

        // 2) add the SO reference to the array
        if (inv.IsFull) return;

        if (itemData.pickUpSfx != null)
        {
            AudioManager.instance.PlaySfxEvent(itemData.pickUpSfx);
        }

        inv.AddItem(this.gameObject);
        // 3) destroy the scene object
    }
}
