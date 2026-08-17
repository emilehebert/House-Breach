using System.Collections.Generic;
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

    private readonly List<CinemachineInputProvider> pausedInputProviders = new List<CinemachineInputProvider>();

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
        FocusStack.Clear();

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
    }

    void SetTimeScale(float value)
    {
        Time.timeScale = value;
    }

    void SetCameraPaused(bool paused)
    {
        // Cuts look-around regardless of which vcam is active (gameplay,
        // chair, etc.) — only cutting "MainVCam" would leave look-around
        // possible in pause whenever another vcam (like a chair) is active.
        if (paused)
        {
            pausedInputProviders.Clear();
            foreach (var provider in FindObjectsOfType<CinemachineInputProvider>())
            {
                if (provider.enabled)
                {
                    provider.enabled = false;
                    pausedInputProviders.Add(provider);
                }
            }
        }
        else
        {
            foreach (var provider in pausedInputProviders)
            {
                if (provider != null)
                    provider.enabled = true;
            }
            pausedInputProviders.Clear();
        }

        // Camera shake stays specific to the gameplay vcam
        GameObject vcamObj = GameObject.FindWithTag("MainVCam");
        if (vcamObj == null) return;

        var vcam = vcamObj.GetComponent<CinemachineVirtualCamera>();
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

    void CleanupDuplicateEventSystems()
    {
        EventSystem[] allSystems = FindObjectsOfType<EventSystem>(true);
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