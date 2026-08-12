using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// FocusInteractable handles showing/hiding the canvas and the input field
// via its On Focus Enter/Exit events. This script only processes commands
// and manages the output/scroll.
public class SimpleTerminal : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public TMP_Text outputText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRect;

    [Header("History")]
    [SerializeField] private int maxLines = 200;

    [Header("Commands (drag your TerminalVerb assets in here)")]
    public List<TerminalVerb> verbs;

    private class VerbData
    {
        public string standaloneResponse;
        public Dictionary<string, string> nouns = new Dictionary<string, string>();
    }

    private readonly Dictionary<string, VerbData> lookup = new Dictionary<string, VerbData>();
    private bool wasActive;
    private float savedScrollPos;

    void Start()
    {
        BuildLookup();
        inputField?.onSubmit.AddListener(OnPlayerSubmit);
    }

    void Update()
    {
        if (inputField == null) return;

        bool active = inputField.gameObject.activeInHierarchy && !MenuManager.IsPaused;
        inputField.interactable = active;

        if (active && !inputField.isFocused) inputField.ActivateInputField();
        else if (!active && inputField.isFocused) inputField.DeactivateInputField();

        if (active && !wasActive) RestoreScrollPosition();
        else if (!active && wasActive && scrollRect != null) savedScrollPos = scrollRect.verticalNormalizedPosition;
        wasActive = active;
    }

    private void BuildLookup()
    {
        foreach (var verb in verbs)
        {
            if (verb?.aliases == null || verb.aliases.Count == 0) continue;

            var data = new VerbData { standaloneResponse = verb.standaloneResponse };

            if (verb.compatibleNouns != null)
                foreach (var entry in verb.compatibleNouns)
                    if (entry.aliases != null)
                        foreach (var alias in entry.aliases)
                            if (!string.IsNullOrWhiteSpace(alias))
                                data.nouns[alias.Trim().ToLower()] = entry.response;

            foreach (var alias in verb.aliases)
                if (!string.IsNullOrWhiteSpace(alias))
                    lookup[alias.Trim().ToLower()] = data;
        }
    }

    public void OnPlayerSubmit(string playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText)) return;

        AppendLine("> " + playerText, blankBefore: true);

        string response = ProcessInput(playerText);
        if (!string.IsNullOrWhiteSpace(response))
            AppendLine(response, blankBefore: false);

        inputField.text = "";
        inputField.ActivateInputField();
    }

    private void AppendLine(string line, bool blankBefore)
    {
        if (outputText == null) return;

        outputText.text += (outputText.text.Length == 0 ? "" : blankBefore ? "\n\n" : "\n") + line;
        TrimHistory();
        SnapScrollToBottom();
    }

    private void SnapScrollToBottom() => SetScroll(0f);

    private void RestoreScrollPosition() => SetScroll(savedScrollPos);

    private void SetScroll(float normalizedPos)
    {
        if (scrollRect == null) return;

        // LayoutRebuilder force un vrai recalcul (fiable même avec des layouts imbriqués).
        if (contentRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = normalizedPos;
    }

    private void TrimHistory()
    {
        var lines = outputText.text.Split('\n');
        if (lines.Length <= maxLines) return;

        outputText.text = string.Join("\n", lines, lines.Length - maxLines, maxLines);
    }

    public string ProcessInput(string playerText)
    {
        var words = playerText.Trim().ToLower().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !lookup.TryGetValue(words[0], out var verb))
            return "Command not recognized.";

        if (words.Length == 1)
            return string.IsNullOrWhiteSpace(verb.standaloneResponse) ? "I don't understand that." : verb.standaloneResponse;

        return verb.nouns.TryGetValue(words[1], out var response) ? response : "Command not recognized.";
    }
}