using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HauntedPSX.RenderPipelines.PSX.Runtime;


public class Inventory : MonoBehaviour
{
    [SerializeField] private GameObject[] items; // Changed from Item[] to GameObject[]
    [SerializeField] private Animator armsAnimator;
    [SerializeField] private Transform holdPoint;
    private GameObject heldItem;
    public int maxSlots = 5;
    public int currentSlot = 0;
    
    [Header("Drop Settings")]
    [SerializeField] private float dropForwardDistance = 5f;    // how far ahead to try to drop
    [SerializeField] private float dropRayHeight      = 1f;    // cast down from this much above the hit
    [SerializeField] private float dropDownDistance   = 10f;   // how far down to raycast

    public bool isUsingItem;
    public bool isHolding;
    private string holdClipName = "Hold";

    private AnimatorOverrideController playerOverride;
    private AnimationClip defaultHoldClip;
    private PlayerBody body;

    private void Awake()
    {
        InitVariables();
        body = GetComponent<PlayerBody>();

        // 1. Get the original asset assigned in the Inspector
        RuntimeAnimatorController originalAsset = armsAnimator.runtimeAnimatorController;

        // 2. Create a CLONE of that controller in memory
        // This new controller exists only while the game is running
        playerOverride = new AnimatorOverrideController(originalAsset);

        // 3. Assign the CLONE to the Animator
        armsAnimator.runtimeAnimatorController = playerOverride;

        // 4. Save the default "Hold" clip from this clean clone
        if (playerOverride != null)
        {
            defaultHoldClip = playerOverride[holdClipName];
        }
        else
        {
            Debug.LogError("Could not create AnimatorOverrideController!");
        }
    }

    private void Update()
    {
        // Both bodies keep their Inventory ticking even while not possessed
        // (only the camera/arms rig toggles) — skip entirely when this body
        // isn't the one in control, so item switching doesn't fire for both
        // bodies at once and we don't poke an armsAnimator sitting on a
        // currently-inactive hierarchy (that's what throws "Animator is not
        // playing an AnimatorController").
        if (body != null && !body.IsPossessed) return;

        if(!isUsingItem) HandleInput();
        UpdateHoldingState();
    }

    void HandleInput()
    {
        if (PlayerDriver.Instance != null && !PlayerDriver.Instance.InputEnabled) return;

        // Tests
        if (Input.GetKeyDown(KeyCode.I)) RemoveItem(currentSlot);

        // Slot selection with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);

        // Scroll wheel selection
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) SelectSlot((currentSlot + 1) % maxSlots);
        if (scroll < 0f) SelectSlot((currentSlot - 1 + maxSlots) % maxSlots);

        // RIGHT-CLICK to use
        if (Input.GetMouseButtonDown(1))
            UseItem(currentSlot);

        if (Input.GetKeyDown(KeyCode.Q))
            DropCurrentItem(currentSlot);
    }

    private void EquipItem()
    {
        UnequipItem();
        var itemObject = items[currentSlot];
        
        if (itemObject != null)
        {
            heldItem = itemObject;
            var HeldItemData = heldItem.GetComponent<PickableItem>().itemData;

            // --- ANIMATION LOGIC ---
            if (playerOverride != null)
            {
                // Check if this specific item has a "Hold" override in its list
                // We use the string holdClipName ("Hold") to look it up
                AnimationClip itemSpecificHold = null;
                
                if (HeldItemData.itemOverride != null)
                {
                    itemSpecificHold = HeldItemData.itemOverride[holdClipName];
                }

                // If the item has a special hold, use it. 
                // If not (it's null), revert to the defaultHoldClip we saved at Start.
                if (itemSpecificHold != null)
                {
                    playerOverride[holdClipName] = itemSpecificHold;
                }
                else
                {
                    playerOverride[holdClipName] = defaultHoldClip;
                }
            }
            
            // Move to hold point and reset transform
            heldItem.transform.SetParent(holdPoint);
            heldItem.transform.localPosition = HeldItemData.holdPositionOffset;
            heldItem.transform.localRotation = Quaternion.Euler(HeldItemData.holdRotationOffset);
            
            // Show the object
            heldItem.SetActive(true);
            
            // Disable physics so it doesn't fall through your floor
            foreach (var rb in heldItem.GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = true;
            foreach (var col in heldItem.GetComponentsInChildren<Collider>())
                col.enabled = false;
                
            // Put it on your player's layer
            ChangeLayerRecursively(
                heldItem, 
                LayerMask.NameToLayer("Player"), 
                PSXRenderQueue.RenderQueueType.UIOverlayOpaque
            );
        }
    }
    
    private void UnequipItem()
    {
        if (heldItem != null) heldItem.SetActive(false);
        heldItem = null;

        // Always reset to default when hand is empty
        if (playerOverride != null)
        {
            playerOverride[holdClipName] = defaultHoldClip;
        }
    }

    private void UseItem(int index)
    {
        var itemObject = items[index];
        if (itemObject == null) return;
        
        // Get the Item data from the PickableItem component
        var pickableItem = itemObject.GetComponent<PickableItem>();
        if (pickableItem == null || pickableItem.itemData == null) return;
        
        var itemData = pickableItem.itemData;
        
        // (1) play animation
        var trigger = itemData.GetUseAnimationTrigger();
        if (armsAnimator != null && !string.IsNullOrEmpty(trigger))
            armsAnimator.SetTrigger(trigger);

        // (2) run the item's logic
        itemData.Use(heldItem);
        
        StartCoroutine(UseRoutine(itemData, index));
    }

    private IEnumerator UseRoutine(Item itemData, int slotIndex)
    {
        isUsingItem = true;

        // (1) wait the unified duration
        yield return new WaitForSeconds(itemData.UseDuration);

        // (2) only clear if RemoveOnUse is true
        if (itemData.RemoveOnUse)
        {
            UnequipItem();
            // Destroy the actual GameObject since it's consumed
            if (items[slotIndex] != null)
            {
                Destroy(items[slotIndex]);
                items[slotIndex] = null;
            }
        }

        isUsingItem = false;
    }

    private void InitVariables()
    {
        items = new GameObject[maxSlots];
    }

    public void AddItem(GameObject newItemObject)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i] == null)
            {
                items[i] = newItemObject;
                currentSlot = i;
                
                // Hide the item initially (it will be shown when equipped)
                newItemObject.SetActive(false);
                
                EquipItem();
                return;
            }
        }
        Debug.Log("Inventory is full!");
    }

    private void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return;
        if(items[slotIndex] != null) armsAnimator.SetTrigger("Raise");
        
        UnequipItem();
        currentSlot = slotIndex;
        EquipItem();
    }

    void UpdateHoldingState()
    {
        isHolding = HasAnyItem();
        armsAnimator.SetBool("Holding", isHolding);
    }
    
    bool HasAnyItem()
    {
        return items[currentSlot] != null;
    }

    public void RemoveItem(int index)
    {
        if (items[index] == null) return;
        
        var pickableItem = items[index].GetComponent<PickableItem>();
        string itemName = pickableItem?.itemData?.itemName ?? "Unknown Item";
        
        Debug.Log($"Removed: {itemName}");
        
        // Destroy the GameObject
        Destroy(items[index]);
        items[index] = null;
        UnequipItem();
    }

    /// Drop the item in slot ‘index’ into the world.
    public void DropCurrentItem(int index)
    {
        GameObject item = items[index];
        if (item == null) return;

        // 1) detach from player
        item.transform.SetParent(null);

        // 2) compute and set world position
        Vector3 dropPos = CalculateDropPosition(item);
        item.transform.position = dropPos;
        item.transform.rotation = Quaternion.identity;

        // 3) re-enable physics & colliders
        foreach (var rb  in item.GetComponentsInChildren<Rigidbody>())  rb.isKinematic = false;
        foreach (var col in item.GetComponentsInChildren<Collider>())   col.enabled     = true;

        // 4) set it back to world layer
        ChangeLayerRecursively(
            item, 
            LayerMask.NameToLayer("Interactable"), 
            PSXRenderQueue.RenderQueueType.MainOpaque
        );

        // 5) animate the arms model
        armsAnimator.SetTrigger("Drop");

        // 6) clear inventory & held reference
        items[index] = null;
        if (heldItem == item) heldItem = null;
    }
    private Vector3 CalculateDropPosition(GameObject item)
    {
        Transform cam    = Camera.main.transform;
        Vector3 origin   = cam.position;
        Vector3 forward  = cam.forward;
        Vector3 forwardPt = origin + forward * dropForwardDistance;
        RaycastHit hit;

        // draw forward ray
        Debug.DrawRay(origin, forward * dropForwardDistance, Color.red, 2f);
        if (Physics.Raycast(origin, forward, out hit, dropForwardDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            forwardPt = hit.point;
        }

        // draw downward ray
        Vector3 downOrigin = forwardPt + Vector3.up * dropRayHeight;
        Debug.DrawRay(downOrigin, Vector3.down * (dropDownDistance + dropRayHeight), Color.blue, 2f);
        if (Physics.Raycast(downOrigin, Vector3.down, out hit, dropDownDistance + dropRayHeight, ~0, QueryTriggerInteraction.Ignore))
        {
            // compute mesh half-height
            var rends = item.GetComponentsInChildren<Renderer>();
            Bounds b   = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
            float yOff = b.extents.y;

            return hit.point + Vector3.up * yOff;
        }

        // fallback if no ground hit
        return forwardPt;
    }

    public GameObject GetItem(int index)
    {
        return items[index];
    }
    
    public Item GetItemData(int index)
    {
        if (items[index] == null) return null;
        
        var pickableItem = items[index].GetComponent<PickableItem>();
        return pickableItem?.itemData;
    }

    private void ChangeLayerRecursively(GameObject go, int layer, PSXRenderQueue.RenderQueueType queueType)
    {
        // 1. Change the Layer
        go.layer = layer;
    
        // 2. Change the Render Queue (Shader setting)
        PSXRuntimeMaterialUtils.SetRenderQueue(go, queueType);
    
        // 3. Repeat for all children
        foreach (Transform t in go.transform)
        {
            ChangeLayerRecursively(t.gameObject, layer, queueType);
        }
    }
    
    public bool IsFull
    {
        get
        {
            for (int i = 0; i < maxSlots; i++)
                if (items[i] == null)
                    return false;
            return true;
        }
    }
}