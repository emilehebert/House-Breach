using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows automatically the instant the player is back in the Office with an
// unconsumed mission result waiting — covers both a successful extraction
// and an early bail-out through the same panel. Builds its own UI at
// runtime instead of depending on a hand-wired prefab, so there's nothing
// to set up in the Editor: it creates itself, shows the result, and
// destroys itself once closed. Applies the earned currency/XP exactly once
// (gated by MissionData.ConsumeResult so it can't double-apply). Registers
// with FocusStack like a FocusInteractable does — unlocks the cursor and
// blocks player input without touching GameManager's Playing/Paused state.
public class MissionResultsPanel : MonoBehaviour, IFocusable
{
    TMP_Text outcomeText;
    TMP_Text summaryText;
    TMP_Text rewardText;

    public bool WantsCursorVisible => true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CheckForPendingResult()
    {
        if (GamePhaseTracker.Current != GamePhase.Office || !MissionData.HasPendingResult)
            return;

        if (FindObjectOfType<MissionResultsPanel>() != null)
            return;

        new GameObject("MissionResultsPanel").AddComponent<MissionResultsPanel>();
    }

    void Awake()
    {
        BuildUI();

        MissionReward reward = SaveSystem.ApplyMissionResult(MissionData.Succeeded, MissionData.ElapsedMinutes);
        MissionData.ConsumeResult();

        outcomeText.text = MissionData.Succeeded ? "Mission Complete" : "Mission Failed";
        summaryText.text = MissionData.ResultSummary;
        rewardText.text = $"+{reward.currency} currency   +{reward.xp} xp";

        if (PlayerDriver.Instance != null)
            PlayerDriver.Instance.SetInputEnabled(false);

        FocusStack.Push(this);
    }

    public void OnCancelFocus() => Close();
    public void OnContinuePressed() => Close();

    void Close()
    {
        if (PlayerDriver.Instance != null)
            PlayerDriver.Instance.SetInputEnabled(true);

        FocusStack.Pop(this);
        Destroy(gameObject);
    }

    void BuildUI()
    {
        Canvas canvas = RuntimeUI.CreateOverlayCanvas("Canvas", transform, 1000); // above every other UI in the scene

        GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(canvas.transform, false);
        RuntimeUI.Stretch(dim.GetComponent<RectTransform>());
        dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        box.transform.SetParent(dim.transform, false);
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(520, 0);
        box.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        VerticalLayoutGroup layout = box.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 16;
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        box.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        outcomeText = CreateCenteredText(box.transform, "OutcomeText", 36, FontStyles.Bold);
        summaryText = CreateCenteredText(box.transform, "SummaryText", 22, FontStyles.Normal);
        rewardText = CreateCenteredText(box.transform, "RewardText", 22, FontStyles.Normal);

        GameObject buttonGO = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonGO.transform.SetParent(box.transform, false);
        buttonGO.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f, 1f);
        LayoutElement buttonLayout = buttonGO.GetComponent<LayoutElement>();
        buttonLayout.preferredHeight = 50;
        buttonLayout.preferredWidth = 220;
        buttonGO.GetComponent<Button>().onClick.AddListener(OnContinuePressed);

        TMP_Text buttonText = RuntimeUI.CreateText(buttonGO.transform, "Text", 22, FontStyles.Normal);
        buttonText.text = "Continue";
        buttonText.alignment = TextAlignmentOptions.Center;
        RuntimeUI.Stretch(buttonText.GetComponent<RectTransform>());
    }

    // Adds the layout sizing/centering this panel's VerticalLayoutGroup
    // needs on top of RuntimeUI's bare text element.
    static TMP_Text CreateCenteredText(Transform parent, string name, float fontSize, FontStyles style)
    {
        TMP_Text text = RuntimeUI.CreateText(parent, name, fontSize, style);
        text.alignment = TextAlignmentOptions.Center;

        LayoutElement layoutElement = text.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = fontSize + 12;

        return text;
    }
}
