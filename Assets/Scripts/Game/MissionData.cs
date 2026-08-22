using UnityEngine;
using UnityEngine.SceneManagement;

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

    // True from the moment a mission resolves until MissionResultsPanel
    // consumes it in Office — lets the panel know there's something new to
    // show without needing a separate "just came from a mission" flag.
    public static bool HasPendingResult { get; private set; }

    private static bool sceneLoadHookRegistered;

    // AfterSceneLoad (not SubsystemRegistration) so GetActiveScene() below
    // already reflects whatever scene the player actually pressed Play
    // from — matters when a mission scene is opened directly.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        InProgress = false;

        // Guarded — with "Enter Play Mode without domain reload" enabled,
        // this method re-runs every Play session but static fields (and
        // event subscriptions) survive between them, so without this guard
        // HandleSceneLoaded would end up subscribed multiple times.
        if (!sceneLoadHookRegistered)
        {
            sceneLoadHookRegistered = true;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        CheckForMissionStart();
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => CheckForMissionStart();

    static void CheckForMissionStart()
    {
        if (GamePhaseTracker.Current == GamePhase.Mission)
            Begin();
    }

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
        HasPendingResult = true;
    }

    // Called once by MissionResultsPanel after it's shown the outcome, so
    // the panel doesn't reappear on the next pause/unpause or scene event.
    public static void ConsumeResult() => HasPendingResult = false;

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