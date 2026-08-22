using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Single info overlay covering everything DebugHUD + DebugMenu used to split
// across two scripts — game/mission state, whichever body is currently
// possessed, and inventory. Read-only: nothing here mutates the scene or
// game state (DebugMenu's old scene-editing buttons were mostly empty stubs
// anyway and are gone). Self-spawns once per session via
// RuntimeInitializeOnLoadMethod, so there's nothing to place in any scene.
public class DebugHUD : MonoBehaviour
{
    TMP_Text text;
    GameObject panelRoot;

    bool visible = true;
    bool fullDetail = true;

    float fpsTimer;
    int fpsFrameCount;
    float fps;

    readonly StringBuilder sb = new StringBuilder(1024);

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
        Canvas canvas = RuntimeUI.CreateOverlayCanvas("Canvas", transform, 2000); // above everything else
        panelRoot = canvas.gameObject;

        // Backdrop so the text stays legible over bright scenery.
        GameObject bg = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvas.transform, false);
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(0, 1);
        bgRect.pivot = new Vector2(0, 1);
        bgRect.anchoredPosition = new Vector2(8, -8);
        bgRect.sizeDelta = new Vector2(420, 560);

        text = RuntimeUI.CreateText(canvas.transform, "DebugText", 15, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = false;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(16, -16);
        rect.sizeDelta = new Vector2(400, 540);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            visible = !visible;

        if (Input.GetKeyDown(KeyCode.F2))
            fullDetail = !fullDetail;

        panelRoot.SetActive(visible);
        if (!visible)
            return;

        UpdateFps();
        text.text = BuildText();
    }

    void UpdateFps()
    {
        fpsFrameCount++;
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.5f)
        {
            fps = fpsFrameCount / fpsTimer;
            fpsFrameCount = 0;
            fpsTimer = 0f;
        }
    }

    string BuildText()
    {
        sb.Clear();

        AppendGame();
        AppendMission();
        AppendPlayer();

        sb.Append("\n[F1] toggle   [F2] ").Append(fullDetail ? "minimal" : "full").Append(" view");

        return sb.ToString();
    }

    void AppendGame()
    {
        sb.Append("=== GAME ===\n");
        sb.Append("Phase: ").Append(GamePhaseTracker.Current).Append('\n');
        sb.Append("State: ").Append(GameManager.CurrentState).Append('\n');

        if (fullDetail)
        {
            sb.Append("Scene: ").Append(SceneManager.GetActiveScene().name).Append('\n');
            sb.Append("TimeScale: ").Append(Time.timeScale.ToString("F2")).Append('\n');
        }

        sb.Append("FPS: ").Append(fps.ToString("F0")).Append('\n');
        sb.Append('\n');
    }

    void AppendMission()
    {
        sb.Append("=== MISSION ===\n");
        sb.Append("In Progress: ").Append(MissionData.InProgress).Append('\n');
        sb.Append("Timer: ").Append(FormatTime(MissionData.ElapsedMinutes)).Append('\n');

        if (fullDetail)
        {
            Candidate candidate = DispatchData.DispatchedCandidate;
            if (candidate != null)
            {
                sb.Append("Dispatched: ").Append(candidate.candidateName)
                    .Append(" -> ").Append(DispatchData.LocationScene).Append('\n');
                sb.Append("  (h=").Append(candidate.height.ToString("F2"))
                    .Append(" w=").Append(candidate.weight.ToString("F0"))
                    .Append(" spd=").Append(candidate.speed.ToString("F1")).Append(")\n");
            }
            else
            {
                sb.Append("Dispatched: -\n");
            }

            if (MissionData.HasPendingResult)
                sb.Append("Pending Result: ").Append(MissionData.Succeeded ? "Success" : "Failed")
                    .Append(" - ").Append(MissionData.ResultSummary).Append('\n');
        }

        sb.Append('\n');
    }

    void AppendPlayer()
    {
        sb.Append("=== PLAYER ===\n");

        PlayerDriver driver = PlayerDriver.Instance;
        if (driver == null)
        {
            sb.Append("No PlayerDriver in scene\n");
            return;
        }

        IPossessable body = driver.CurrentBody;
        if (body == null)
        {
            sb.Append("No body possessed\n");
        }
        else
        {
            sb.Append("Possessing: ").Append(body.GetType().Name).Append('\n');

            Vector3 pos = body.GameObject.transform.position;
            sb.Append("Position: (").Append(pos.x.ToString("F1")).Append(", ")
                .Append(pos.y.ToString("F1")).Append(", ").Append(pos.z.ToString("F1")).Append(")\n");

            sb.Append("Grounded: ").Append(body.IsGrounded)
                .Append("   Speed: ").Append(body.CurrentHorizontalSpeed.ToString("F1")).Append('\n');

            if (fullDetail)
            {
                if (body is PlayerController pc)
                {
                    sb.Append("Crouch/Slide/Air: ").Append(pc.isCrouching).Append('/')
                        .Append(pc.isSliding).Append('/').Append(pc.isAiring).Append('\n');
                    sb.Append("Walk/Run/Crouch spd: ").Append(pc.walkSpeed.ToString("F1")).Append('/')
                        .Append(pc.runSpeed.ToString("F1")).Append('/').Append(pc.crouchSpeed.ToString("F1")).Append('\n');
                    sb.Append("Gravity: ").Append(pc.gravity.ToString("F1"))
                        .Append("   JumpHeight: ").Append(pc.jumpHeight.ToString("F1")).Append('\n');
                }
                else if (body is OfficeController oc)
                {
                    sb.Append("Walk speed: ").Append(oc.walkSpeed.ToString("F1"))
                        .Append("   Gravity: ").Append(oc.gravity.ToString("F1")).Append('\n');
                }

                Inventory inventory = body.GameObject.GetComponent<Inventory>();
                if (inventory != null)
                {
                    sb.Append("Slot: ").Append(inventory.currentSlot + 1).Append('/').Append(inventory.maxSlots)
                        .Append("   Holding: ").Append(inventory.isHolding)
                        .Append("   Using: ").Append(inventory.isUsingItem).Append('\n');

                    Item held = inventory.GetItemData(inventory.currentSlot);
                    sb.Append("Held Item: ").Append(held != null ? held.itemName : "-").Append('\n');
                }
            }
        }

        if (fullDetail)
        {
            sb.Append("Input Enabled: ").Append(driver.InputEnabled).Append('\n');
            sb.Append("Camera Locked: ").Append(CameraLookGate.IsLocked)
                .Append("   Focus Active: ").Append(FocusStack.IsActive).Append('\n');
            sb.Append("Cursor: visible=").Append(Cursor.visible).Append(" lock=").Append(Cursor.lockState).Append('\n');
        }
    }

    static string FormatTime(float minutes)
    {
        int totalSeconds = Mathf.FloorToInt(minutes * 60f);
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}
