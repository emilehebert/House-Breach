using UnityEngine;

/// <summary>
/// The state of the currently active mission — separate from DispatchData
/// (which only carries who/where at the moment of dispatch). Tracks
/// whether a mission is running, how long it's been going, and how it
/// resolved once it ends. Static, like FocusStack and DispatchData.
/// </summary>
public static class MissionData
{
    public static bool InProgress { get; private set; }

    private static float startTime;

    // Time.time freezes while Time.timeScale is 0, so pausing correctly
    // doesn't count against the mission clock.
    public static float ElapsedMinutes => InProgress ? (Time.time - startTime) / 60f : 0f;

    public static void Begin()
    {
        InProgress = true;
        startTime = Time.time;
    }

    public static bool Succeeded { get; private set; }
    public static string ResultSummary { get; private set; }

    public static void ReportResult(bool succeeded, string summary)
    {
        Succeeded = succeeded;
        ResultSummary = summary;
        InProgress = false;
    }

    // Called by whatever the player uses to bail out early — a physical
    // exit point in the level, a pause menu button, etc. Both should call
    // this instead of duplicating the logic themselves.
    public static void LeaveEarly()
    {
        ReportResult(succeeded: false, "Left the mission early");

        if (ScenesManager.Instance != null)
            ScenesManager.Instance.LoadScene(ScenesManager.Scene.Office);
    }
}