using UnityEngine;

/// <summary>
/// Single authority for whether FPS look-around input should be applied,
/// kept independent of PlayerDriver's move/jump/run/crouch lock —
/// MountableChair blocks movement while sitting but must keep camera look
/// working (that's the point of a chair), so this can't live inside
/// PlayerDriver.SetInputEnabled. Nested-lock counted the same way
/// PlayerDriver already does it, so e.g. Pause and a screen focus can both
/// hold the lock at once without one release re-enabling look while the
/// other still wants it blocked.
///
/// The actual look input comes from CinemachineInputBridge overriding
/// CinemachineCore.GetInputAxis — Cinemachine's own CinemachineInputProvider
/// component does nothing in this project (CINEMACHINE_UNITY_INPUTSYSTEM
/// isn't defined, so it compiles to an empty stub), so IsLocked has to be
/// checked at that bridge, not by toggling vcam components.
/// </summary>
public static class CameraLookGate
{
    private static int lockCount = 0;

    public static bool IsLocked => lockCount > 0;

    public static void SetLocked(bool locked)
    {
        lockCount = locked ? lockCount + 1 : Mathf.Max(0, lockCount - 1);
    }

    /// <summary>Call on scene change so a lock count left unbalanced by a destroyed object can't leak into the next scene.</summary>
    public static void Clear()
    {
        lockCount = 0;
    }
}
