using TMPro;
using UnityEngine;

// Top-left overlay showing internal state while testing — game phase,
// mission timer, saved currency/xp, and FPS. Toggle with F1 (matches the
// legacy Input.GetKeyDown convention already used by Inventory.cs). Builds
// its own UI via RuntimeUI (same approach as MissionResultsPanel) and
// self-spawns once per session, so there's nothing to place in any scene.
public class DebugHUD : MonoBehaviour
{
    TMP_Text text;
    bool visible = true;

    float fpsTimer;
    int fpsFrameCount;
    float fps;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<DebugHUD>() != null)
            return;

        GameObject go = new GameObject("DebugHUD");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugHUD>();
    }

    void Awake()
    {
        Canvas canvas = RuntimeUI.CreateOverlayCanvas("Canvas", transform, 2000); // above everything, including MissionResultsPanel

        text = RuntimeUI.CreateText(canvas.transform, "DebugText", 20, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(16, -16);
        rect.sizeDelta = new Vector2(400, 200);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            visible = !visible;

        text.gameObject.SetActive(visible);
        if (!visible)
            return;

        fpsFrameCount++;
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.5f)
        {
            fps = fpsFrameCount / fpsTimer;
            fpsFrameCount = 0;
            fpsTimer = 0f;
        }

        string timer = MissionData.InProgress ? FormatTime(MissionData.ElapsedMinutes) : "--:--";

        text.text =
            $"Phase: {GamePhaseTracker.Current}\n" +
            $"Mission Timer: {timer}\n" +
            $"Currency: {SaveSystem.Current.currency}\n" +
            $"XP: {SaveSystem.Current.xp} (Lv {SaveSystem.Current.Level})\n" +
            $"FPS: {fps:F0}\n" +
            $"GameState: {GameManager.CurrentState}\n" +
            $"CameraLookGate.IsLocked: {CameraLookGate.IsLocked}\n" +
            "[F1] toggle";
    }

    static string FormatTime(float minutes)
    {
        int totalSeconds = Mathf.FloorToInt(minutes * 60f);
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}
