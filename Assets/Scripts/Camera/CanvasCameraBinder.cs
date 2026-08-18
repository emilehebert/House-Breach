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

    // Camera.main changes whenever PlayerDriver possesses a different body
    // (each body has its own camera, only one enabled at a time) — polling
    // here means this canvas always follows whoever's currently possessed,
    // with no coupling to PlayerDriver/possession logic at all.
    void Update()
    {
        BindCamera();
    }

    void BindCamera()
    {
        Camera sceneCamera = Camera.main; // finds whatever is tagged "MainCamera" in the active scene
        if (sceneCamera == null || canvas == null) return;
        if (canvas.worldCamera == sceneCamera) return; // already correct — skip the canvas rebuild

        // Only worldCamera — never touch renderMode. Works for Screen Space -
        // Camera canvases (HUD, pause menu) AND World Space ones (a diegetic
        // terminal screen), both of which use worldCamera for rendering/
        // raycasting; forcing Screen Space would wreck a World Space canvas's
        // in-world position/scale.
        canvas.worldCamera = sceneCamera;
    }
}