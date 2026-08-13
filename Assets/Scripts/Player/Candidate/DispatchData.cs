/// <summary>
/// Carries data across scene loads for the dispatch flow:
/// Office -> Location -> Results -> Office. Static, like FocusStack — no
/// GameObject or DontDestroyOnLoad needed, the data just persists for the
/// life of the application. Holds a reference to the Candidate asset
/// itself rather than copying its fields — ScriptableObjects aren't
/// destroyed on scene load, so the reference stays valid the whole way
/// through without any extra work.
/// </summary>
public static class DispatchData
{
    // --- Set by the Office terminal right before loading the location scene ---
    public static Candidate DispatchedCandidate { get; private set; }
    public static ScenesManager.Scene LocationScene { get; private set; }

    public static void StageDispatch(Candidate candidate, ScenesManager.Scene locationScene)
    {
        DispatchedCandidate = candidate;
        LocationScene = locationScene;
    }

    // --- Set by the location scene right before loading the results screen ---
    public static bool MissionSucceeded { get; private set; }
    public static string ResultSummary { get; private set; }

    public static void ReportResult(bool succeeded, string summary)
    {
        MissionSucceeded = succeeded;
        ResultSummary = summary;
    }
}