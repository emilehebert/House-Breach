using UnityEngine;

// Persistent UI's Canvas is Screen Space - Camera (not Overlay) on purpose,
// so the PSX post-processing volume affects the pause/menu UI too, not just
// the game world — that requires a Render Camera reference. Each scene's
// copy of this prefab used to bake in a reference to that scene's own local
// MainCameraController Camera, but MainCameraController is a
// DontDestroyOnLoad singleton that self-destructs every copy after the
// first — so any scene loaded after another one ended up pointing at a
// destroyed Camera. Resolving it here at runtime instead means it always
// points at whichever MainCameraController instance actually survived.
[RequireComponent(typeof(Canvas))]
public class CanvasCameraBinder : MonoBehaviour
{
    void Start()
    {
        if (MainCameraController.Instance == null)
            return;

        Camera cam = MainCameraController.Instance.GetComponent<Camera>();
        if (cam != null)
            GetComponent<Canvas>().worldCamera = cam;
    }
}
