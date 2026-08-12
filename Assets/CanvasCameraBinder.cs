using UnityEngine;
using UnityEngine.SceneManagement;

public class CanvasCameraBinder : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindCamera(); // also try immediately, in case this runs after a scene is already loaded
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindCamera();
    }

    void BindCamera()
    {
        Camera sceneCamera = Camera.main; // finds whatever is tagged "MainCamera" in the active scene

        if (sceneCamera != null && canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = sceneCamera;
        }
    }
}