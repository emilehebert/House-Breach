using UnityEngine;

[CreateAssetMenu(menuName = "Items/Generic")]
public class GenericItem : Item
{
    private void OnValidate()
    {
        // By default it never disappears when “used”
        // (i.e. it’s not a consumable drink or ammo)
    }

    public override void Use(GameObject item)
    {
        // No real effect—just a debug message
        Debug.Log($"Nothing happens when you use {itemName}.");
    }
}
