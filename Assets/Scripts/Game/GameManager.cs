using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Cinemachine;

/// <summary>
/// Owns the core game state (Title/Playing/Paused) and its game-wide
/// side effects: timeScale, cursor (via FocusStack), camera pause, scene
/// load reactions. Renamed from MenuManager — this script no longer
/// knows panels exist at all; MenuManager reacts to CurrentState instead.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState
    {
        TitleMenu,  // browsing the title screen
        Playing,    // active gameplay, unpaused
        Paused      // pause menu open during gameplay
    }

    [Header("Input")]
    [Tooltip("Action Echap, dédiée exclusivement à la pause. Pour sortir d'un focus (chaise/écran), voir backAction sur PlayerController.")]
    [SerializeField] private InputActionReference cancelAction;

    GameState currentState = GameState.TitleMenu;
    bool isTitleScene = true;

    CinemachineBasicMultiChannelPerlin _noise;
    float _savedAmplitudeGain;

    InteractionSystem _interactionSystem;

    public static GameState CurrentState => Instance != null ? Instance.currentState : GameState.TitleMenu;
    public static bool IsPaused => Instance != null && Instance.currentState == GameState.Paused;
    public static bool IsMenuOpen => Instance != null && Instance.currentState != GameState.Playing;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (cancelAction != null)
        {
            cancelAction.action.Enable();
            cancelAction.action.performed += OnCancelPerformed;
        }

        InitializeForCurrentScene();
    }

    void InitializeForCurrentScene()
    {
        CleanupDuplicateEventSystems();
        CleanupDuplicateAudioListeners();
        FocusStack.Clear();

        CameraLookGate.Clear();
        isTitleScene = SceneManager.GetActiveScene().name == ScenesManager.Scene.MainMenu.ToString();
        EnterState(isTitleScene ? GameState.TitleMenu : GameState.Playing);
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (cancelAction != null)
        {
            cancelAction.action.performed -= OnCancelPerformed;
            cancelAction.action.Disable();
        }
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeForCurrentScene();
    }

    void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (isTitleScene) return;

        // Escape always toggles pause, even seated or focused on a screen —
        // exiting a focus uses a dedicated key (backAction on
        // PlayerController), not Escape.
        if (currentState == GameState.Playing) EnterState(GameState.Paused);
        // Don't resume straight out of the options panel — MenuManager
        // owns that UI state, GameManager just checks it.
        else if (currentState == GameState.Paused && !MenuManager.IsOptionsOpen) EnterState(GameState.Playing);
    }

    // ---------- Core state machine — Title / Playing / Paused only ----------

    public void EnterState(GameState newState)
    {
        currentState = newState;

        bool paused = newState == GameState.Paused;

        SetTimeScale(paused ? 0f : 1f);
        FocusStack.RefreshCursor();
        SetCameraPaused(paused);
        SetInteractionSystemPaused(paused);
    }

    void SetTimeScale(float value)
    {
        Time.timeScale = value;
    }

    void SetCameraPaused(bool paused)
    {
        // Cuts look-around regardless of which vcam is active (gameplay,
        // chair, etc.) via the shared CameraLookGate — only cutting
        // "MainVCam" would leave look-around possible in pause whenever
        // another vcam (like a chair) is active.
        CameraLookGate.SetLocked(paused);

        // Camera shake stays specific to the gameplay vcam — resolved via
        // Camera.main (whichever body is currently possessed has the active
        // camera) instead of a "MainVCam" tag nothing in the project actually
        // carries, so this silently never fired before.
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        var vcam = mainCam.GetComponentInParent<CinemachineVirtualCamera>();
        if (vcam == null) return;

        _noise = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (_noise == null) return;

        if (paused)
        {
            _savedAmplitudeGain = _noise.m_AmplitudeGain;
            _noise.m_AmplitudeGain = 0f;
        }
        else
        {
            _noise.m_AmplitudeGain = _savedAmplitudeGain;
        }
    }

    // Freezes/hides the crosshair cursor and "E for X" prompt while paused —
    // SetInteractionEnabled() also stops InteractionSystem.Update() entirely
    // (not just the visuals), so raycasting/interacting can't sneak through
    // while paused either. Same reusable hook FocusInteractable already uses
    // for screen/chair focus. Looked up lazily instead of cached at scene
    // load since InteractionSystem lives on its own object that
    // CleanupDuplicateEventSystems-style timing issues could otherwise miss.
    void SetInteractionSystemPaused(bool paused)
    {
        if (_interactionSystem == null)
            _interactionSystem = FindFirstObjectByType<InteractionSystem>();

        if (_interactionSystem == null) return;

        _interactionSystem.SetInteractionEnabled(!paused);
    }

    void CleanupDuplicateEventSystems()
    {
        EventSystem[] allSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allSystems.Length <= 1) return;

        // Prefer keeping one that's already persistent (living in the
        // special DontDestroyOnLoad scene) over one that just came in with
        // this scene load. Falls back to the first one found if none are
        // persistent yet (e.g. the very first scene load). Doesn't assume
        // any hierarchy relationship to GameManager — they don't need to
        // be under the same parent.
        EventSystem keep = System.Array.Find(allSystems, es => es.gameObject.scene.name == "DontDestroyOnLoad")
            ?? allSystems[0];

        foreach (var es in allSystems)
        {
            if (es != keep)
                Destroy(es.gameObject);
        }
    }

    // Same situation as CleanupDuplicateEventSystems, one level up: MainMenu
    // carries its own standalone Camera/AudioListener/CinemachineBrain so the
    // title screen has something to render with on a cold boot (before any
    // MainCameraController has ever existed). Once the real
    // MainCameraController survives back into MainMenu from Office/House01,
    // that scene-local camera becomes a duplicate — same fix, same preference
    // for whichever one is already persistent.
    void CleanupDuplicateAudioListeners()
    {
        AudioListener[] allListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allListeners.Length <= 1) return;

        AudioListener keep = System.Array.Find(allListeners, al => al.gameObject.scene.name == "DontDestroyOnLoad")
            ?? allListeners[0];

        foreach (var al in allListeners)
        {
            if (al != keep)
                Destroy(al.gameObject);
        }
    }

    // ---------- Scene / app lifecycle ----------

    public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);

    public void LoadTitleScene()
    {
        if (ScenesManager.Instance != null)
            ScenesManager.Instance.LoadScene(ScenesManager.Scene.MainMenu);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}