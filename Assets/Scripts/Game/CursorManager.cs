using UnityEngine;

/// <summary>
/// Single authority for Cursor.visible / Cursor.lockState. Nothing else in
/// the project should touch Cursor.* directly. FocusStack (and anything
/// else that derives an automatic "should the cursor be visible" result
/// from game state) calls Apply() whenever that result changes. Debug/ad-hoc
/// tools that don't want to participate in the focus stack call
/// HideCursor/LockCursor directly instead — those act as manual overrides
/// that win over the automatic result until cleared.
/// </summary>
public static class CursorManager
{
    private static bool lastAutoShouldShow;
    private static bool? manualVisibleOverride;
    private static bool? manualLockOverride;

    /// <summary>Called with the automatically-derived visibility result (pause state OR active focus).</summary>
    public static void Apply(bool autoShouldShow)
    {
        lastAutoShouldShow = autoShouldShow;
        ApplyInternal();
    }

    public static void HideCursor(bool hide)
    {
        manualVisibleOverride = !hide;
        ApplyInternal();
    }

    public static void ClearHideCursorOverride()
    {
        manualVisibleOverride = null;
        ApplyInternal();
    }

    /// <summary>Independent of visibility — e.g. a debug tool may want the cursor visible but still locked to the window.</summary>
    public static void LockCursor(bool locked)
    {
        manualLockOverride = locked;
        ApplyInternal();
    }

    public static void ClearLockCursorOverride()
    {
        manualLockOverride = null;
        ApplyInternal();
    }

    /// <summary>Call on scene change so a forgotten debug override doesn't leak into the next scene.</summary>
    public static void ClearOverrides()
    {
        manualVisibleOverride = null;
        manualLockOverride = null;
        ApplyInternal();
    }

    private static void ApplyInternal()
    {
        bool shouldShow = manualVisibleOverride ?? lastAutoShouldShow;
        bool locked = manualLockOverride ?? !shouldShow;

        Cursor.visible = shouldShow;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
