using UnityEngine.SceneManagement;

/// <summary>
/// The broad phase of the game right now — derived live from which scene
/// is loaded rather than tracked as separate state, so it can't drift out
/// of sync with reality. Mirrors ScenesManager.Scene: MainMenu -> Title,
/// Office -> Office, any other registered scene (House01, etc.) -> Mission.
/// </summary>
public enum GamePhase
{
    Title,
    Office,
    Mission
}

public static class GamePhaseTracker
{
    public static GamePhase Current
    {
        get
        {
            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName == ScenesManager.Scene.MainMenu.ToString())
                return GamePhase.Title;

            if (sceneName == ScenesManager.Scene.Office.ToString())
                return GamePhase.Office;

            return GamePhase.Mission;
        }
    }
}