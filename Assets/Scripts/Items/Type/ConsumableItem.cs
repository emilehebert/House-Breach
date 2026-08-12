using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable")]
public class ConsumableItem : Item
{
    [Header("Usage")]
    public bool consumable = true; // removed after Use()
    public float useDuration = 1f; // blocks re-use for this long
    public AudioClip useSfx; // optional sound when used
    public string useAnimationTrigger; // animator trigger name, if any

    private void OnValidate()
    {
        // This makes it consumable, sets a short use time, and plays a “Drink” trigger
        consumable = true;
    }

    public override void Use(GameObject item)
    {
        Debug.Log("give player speed");
    }
    public override float UseDuration => useDuration;
    public override bool RemoveOnUse => consumable;
    public override string GetUseAnimationTrigger() => useAnimationTrigger;
}
