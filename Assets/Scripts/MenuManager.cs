using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Cinemachine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    public enum GameState
    {
        TitleMenu,  // browsing the title screen
        Playing,    // active gameplay, unpaused
        Paused      // pause menu open during gameplay
    }

    [Header("Root Panels")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject pauseMenuPanel;

    [Header("Shared Panels")]
    [SerializeField] GameObject optionsMenuPanel;

    [Header("Config")]
    [SerializeField] string titleSceneName = "MainTitle";

    [Header("Input")]
    [Tooltip("Action Echap, dédiée exclusivement à la pause. Pour sortir d'un focus (chaise/écran), voir backAction sur PlayerController.")]
    [SerializeField] private InputActionReference cancelAction;

    GameState currentState = GameState.TitleMenu;
    bool optionsOpen = false;
    bool isTitleScene = true;

    CinemachineBasicMultiChannelPerlin _noise;
    float _savedAmplitudeGain;

    // Providers désactivées par la pause (peu importe laquelle est active : gameplay, chaise...)
    private readonly List<CinemachineInputProvider> pausedInputProviders = new List<CinemachineInputProvider>();

    public static bool IsPaused => Instance != null && Instance.currentState == GameState.Paused;

    /// <summary>Vrai sur l'écran titre ET en pause — les deux états où le jeu n'est pas activement joué.</summary>
    public static bool IsMenuOpen => Instance != null && Instance.currentState != GameState.Playing;

    void Awake()
    {
        // Doublon (ex: un MenuManager placé dans plusieurs scènes) : on le
        // détruit tout de suite, SANS jamais toucher à l'event ou à l'action
        // (contrairement à avant où ça passait par OnEnable/OnDisable, ce qui
        // pouvait désactiver l'action partagée du VRAI singleton par erreur).
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup fait UNE SEULE FOIS ici, pour la vraie instance persistante,
        // peu importe combien de scènes se chargent ensuite.
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (cancelAction != null)
            cancelAction.action.Enable();

        // SceneManager.sceneLoaded ne se déclenche PAS pour la scène déjà
        // chargée au démarrage du jeu (seulement pour les scènes chargées
        // après coup) — sans ça, EnterState() (et donc le curseur) ne
        // s'appliquait jamais au tout premier lancement.
        InitializeForCurrentScene();
    }

    void InitializeForCurrentScene()
    {
        CleanupDuplicateEventSystems();
        FocusStack.Clear();

        isTitleScene = SceneManager.GetActiveScene().name == titleSceneName;
        optionsOpen = false;
        EnterState(isTitleScene ? GameState.TitleMenu : GameState.Playing);
    }

    void OnDestroy()
    {
        // Ne nettoie que si c'est la vraie instance qui est détruite (ex: fermeture
        // du jeu), pas un doublon déjà court-circuité dans Awake() ci-dessus.
        if (Instance != this) return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (cancelAction != null)
            cancelAction.action.Disable();
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeForCurrentScene();
    }

    void Update()
    {
        if (isTitleScene) return;
        if (cancelAction == null || !cancelAction.action.WasPressedThisFrame()) return;

        // Echap ouvre/ferme TOUJOURS la pause, même assis ou en focus sur un
        // écran — pour sortir d'un focus, le joueur utilise une touche dédiée
        // (backAction sur PlayerController), pas Echap.
        if (currentState == GameState.Playing) EnterState(GameState.Paused);
        else if (currentState == GameState.Paused && !optionsOpen) EnterState(GameState.Playing);
    }

    // ---------- Core state machine — handles Title / Playing / Paused only ----------

    void EnterState(GameState newState)
    {
        currentState = newState;

        mainMenuPanel.SetActive(newState == GameState.TitleMenu);
        pauseMenuPanel.SetActive(newState == GameState.Paused);
        optionsMenuPanel.SetActive(false);
        optionsOpen = false;

        bool paused = newState == GameState.Paused;

        SetTimeScale(paused ? 0f : 1f);
        FocusStack.RefreshCursor();
        SetCameraPaused(paused);
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
        mainMenuPanel.SetActive(currentState == GameState.TitleMenu);
        pauseMenuPanel.SetActive(currentState == GameState.Paused);
    }

    // ---------- Small, single-purpose helpers ----------

    void SetTimeScale(float value)
    {
        Time.timeScale = value;
    }

    void SetCameraPaused(bool paused)
    {
        // Coupe le look-around peu importe quelle vcam est active (gameplay, chaise,
        // etc.) — avant, ça ne coupait que "MainVCam", donc regarder autour de soi
        // restait possible en pause dès qu'une autre vcam (comme la chaise) était active.
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

        // Le bruit de caméra (shake) reste spécifique à la vcam de gameplay
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
        EventSystem mySystem = GetComponentInChildren<EventSystem>(true);

        foreach (var es in allSystems)
        {
            if (es != mySystem)
                Destroy(es.gameObject);
        }
    }

    // ---------- Remaining button hookups (unchanged names) ----------

    public void OnResumePressed() => EnterState(GameState.Playing);

    public void OnPlayPressed()
    {
        SceneManager.LoadScene("Office");
    }

    public void OnExitPressed()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnLeavePressed()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}