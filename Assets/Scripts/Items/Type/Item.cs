using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class Item : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "New Item";
    [TextArea] public string description = "Describe what this item does…";

    [Header("Hold Settings")]
    public Vector3 holdPositionOffset = Vector3.zero;
    public Vector3 holdRotationOffset = Vector3.zero;
    public AnimatorOverrideController itemOverride; // NEW: Drag your override here (e.g., Pistol_Override)

    [Header("SFX")]
    public bool canBeDropped = true; // allow dropping back into the world?
    public AudioEvent pickUpSfx; // optional sound on pickup
    public AudioEvent dropSfx; // optional sound on drop
    
    public virtual float UseDuration => 0f;
    public virtual bool RemoveOnUse => false;

    public abstract void Use(GameObject item);
    public virtual string GetInteractText() => $"E for {itemName}";
    public virtual string GetUseAnimationTrigger() => null;
}
