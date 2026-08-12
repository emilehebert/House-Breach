using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f; // reset no matter what state the last scene left us in
        bool isMenu = scene.name == "MainMenu";
        SetGameState(isMenu);
    }

    public void SetGameState(bool isMenu)
    {
        Time.timeScale = isMenu ? Time.timeScale : 1f; // menu itself doesn't need timeScale=0, only pause does
        Cursor.lockState = isMenu ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isMenu;
    }

    public void OnQuit()
    {
        Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}