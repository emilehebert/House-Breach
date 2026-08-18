using TMPro;
using UnityEngine;

/// <summary>
/// Owns UI panel visibility and button click handlers only — no game
/// state, no timeScale, no cursor logic (that's GameManager's job now).
/// Watches GameManager.CurrentState and shows the matching panel.
/// </summary>
public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [Header("Root Panels")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject pauseMenuPanel;

    [Header("Shared Panels")]
    [SerializeField] GameObject optionsMenuPanel;

    [Header("Pause Menu Extras")]
    [Tooltip("The same button's label — reads \"Leave\" in the Office, \"Leave Mission\" during a mission.")]
    [SerializeField] TMP_Text leaveButtonText;

    bool optionsOpen = false;
    GameManager.GameState? lastSeenState = null;

    public static bool IsOptionsOpen => Instance != null && Instance.optionsOpen;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Polls GameManager.CurrentState the same way other scripts already
        // poll GameManager.IsPaused — reacts whenever it changes, no event
        // wiring needed. Nullable so the very first frame always applies
        // the correct panel — a plain GameState field defaults to 0
        // (TitleMenu), which would make this look like "nothing changed"
        // if the game actually starts on TitleMenu, and the panel would
        // never get explicitly shown.
        if (GameManager.CurrentState == lastSeenState) return;

        lastSeenState = GameManager.CurrentState;
        ShowPanelForState(lastSeenState.Value);
    }

    void ShowPanelForState(GameManager.GameState state)
    {
        mainMenuPanel.SetActive(state == GameManager.GameState.TitleMenu);
        pauseMenuPanel.SetActive(state == GameManager.GameState.Paused);
        optionsMenuPanel.SetActive(false);
        optionsOpen = false;

        if (leaveButtonText != null)
            leaveButtonText.text = GamePhaseTracker.Current == GamePhase.Mission ? "Leave Mission" : "Leave";
    }

    // ---------- Options is a panel layered on top of the current state, not its own state ----------

    public void OnOptionsPressed()
    {
        optionsOpen = true;
        mainMenuPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
        optionsMenuPanel.SetActive(true);
        // timeScale / cursor / camera stay exactly as they already are for the current state
    }

    public void OnBackFromOptionsPressed()
    {
        optionsOpen = false;
        optionsMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(GameManager.CurrentState == GameManager.GameState.TitleMenu);
        pauseMenuPanel.SetActive(GameManager.CurrentState == GameManager.GameState.Paused);
    }

    // ---------- Button hookups ----------

    public void OnResumePressed() => GameManager.Instance.EnterState(GameManager.GameState.Playing);

    public void OnPlayPressed() => GameManager.Instance.LoadScene(ScenesManager.Scene.Office.ToString());

    public void OnExitPressed() => GameManager.Instance.QuitGame();

    public void OnLeavePressed()
    {
        if (GamePhaseTracker.Current == GamePhase.Mission)
            MissionData.LeaveEarly();
        else
            GameManager.Instance.LoadTitleScene();
    }
}