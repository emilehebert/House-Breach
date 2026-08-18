using System.Collections;
using UnityEngine;

public class PixelatedFullscreen : MonoBehaviour
{
    [Header("Low-Res Target (4:3)")]
    public int targetWidth  = 320;
    public int targetHeight = 240;

    [Header("Fullscreen Mode")]
    [Tooltip("Borderless fullscreen at low res")]
    public FullScreenMode fullscreenMode = FullScreenMode.FullScreenWindow;

    private IEnumerator Start()
    {
        // 1) Windowed at low res
        Screen.SetResolution(targetWidth, targetHeight, FullScreenMode.Windowed);
        
        // wait one frame so the window actually resizes...
        yield return null;
        
        // 2) Borderless fullscreen at same low res
        Screen.SetResolution(targetWidth, targetHeight, fullscreenMode);
    }
}
