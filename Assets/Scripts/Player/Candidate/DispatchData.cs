/// <summary>
/// Carries who/where was dispatched across the scene load from Office into
/// the mission scene. Static, like FocusStack — no GameObject or
/// DontDestroyOnLoad needed, the data just persists for the life of the
/// application. Holds a reference to the Candidate asset itself rather
/// than copying its fields — ScriptableObjects aren't destroyed on scene
/// load, so the reference stays valid the whole way through without any
/// extra work.
///
/// How the mission resolves (success/failure, summary, rewards) is tracked
/// separately by MissionData — this class only ever describes what was
/// staged at dispatch time.
/// </summary>
public static class DispatchData
{
    public static Candidate DispatchedCandidate { get; private set; }
    public static ScenesManager.Scene LocationScene { get; private set; }

    public static void StageDispatch(Candidate candidate, ScenesManager.Scene locationScene)
    {
        DispatchedCandidate = candidate;
        LocationScene = locationScene;
    }
}