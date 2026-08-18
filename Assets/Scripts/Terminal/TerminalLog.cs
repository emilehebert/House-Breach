/// <summary>
/// Carries the terminal's on-screen text log across scene loads, so typing
/// in one scene's terminal (e.g. Office) and continuing in another (House01,
/// a future House02, etc.) shows the same running history instead of each
/// SimpleTerminal instance starting blank. Static, like DispatchData/
/// FocusStack — no GameObject or DontDestroyOnLoad needed, the string just
/// persists for the life of the application.
/// </summary>
public static class TerminalLog
{
    public static string History { get; set; } = "";
}
