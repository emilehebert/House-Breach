using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// FocusInteractable handles showing/hiding the canvas and
// activating/deactivating the input field via its On Focus Enter / On
// Focus Exit events. This script's only job is finding the right
// TerminalCommand for what was typed and running it.
public class SimpleTerminal : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public TMP_Text outputText;

    [Header("Scrolling (optional — leave empty for no scroll/trim)")]
    public ScrollRect scrollRect;
    public RectTransform contentRect;
    public int maxLines = 200;

    [Header("Commands (drag your TerminalCommand assets in here)")]
    public List<TerminalCommand> commands;

    [Header("Candidates (for the dispatch command)")]
    public List<Candidate> candidates;

    // key = every alias, all pointing at the same TerminalCommand
    private Dictionary<string, TerminalCommand> lookup;

    private bool wasActive;
    private float savedScrollPos;

    void Start()
    {
        BuildLookup();

        if (inputField != null)
            inputField.onSubmit.AddListener(OnPlayerSubmit);
    }

    void Update()
    {
        if (inputField == null) return;

        // The field should only be active while the terminal is actually
        // open (its canvas is visible) and the game isn't paused. Checked
        // fresh every frame instead of tracking what changed and when —
        // this makes the field self-correcting no matter what caused it
        // to lose focus (pause, Unity's own Escape handling, whatever).
        bool shouldBeActive = inputField.gameObject.activeInHierarchy && !GameManager.IsPaused;

        inputField.interactable = shouldBeActive;

        if (shouldBeActive && !inputField.isFocused)
            inputField.ActivateInputField();
        else if (!shouldBeActive && inputField.isFocused)
            inputField.DeactivateInputField();

        // Remember where you were scrolled to when the terminal closes,
        // and put it back instead of always snapping to the bottom on reopen.
        if (shouldBeActive && !wasActive)
            RestoreScrollPosition();
        else if (!shouldBeActive && wasActive && scrollRect != null)
            savedScrollPos = scrollRect.verticalNormalizedPosition;

        wasActive = shouldBeActive;
    }

    // Flattens every TerminalCommand asset into a dictionary keyed by
    // every accepted spelling, so "clear", "clr" and "cleer" all resolve
    // to the exact same command.
    private void BuildLookup()
    {
        lookup = new Dictionary<string, TerminalCommand>();

        foreach (var command in commands)
        {
            if (command == null || command.aliases == null) continue;

            foreach (var alias in command.aliases)
            {
                if (string.IsNullOrWhiteSpace(alias)) continue;
                lookup[alias.Trim().ToLower()] = command;
            }
        }
    }

    public void OnPlayerSubmit(string playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText))
            return;

        TerminalCommand matchedCommand = FindCommand(playerText);
        string response = ProcessInput(playerText);

        if (outputText != null)
        {
            bool clear = matchedCommand != null && matchedCommand.clearsScreen;

            if (clear)
            {
                outputText.text = response;
            }
            else
            {
                AppendLine("> " + playerText, blankLineBefore: true);

                if (!string.IsNullOrWhiteSpace(response))
                    AppendLine(response, blankLineBefore: false);
            }
        }

        inputField.text = "";
        inputField.ActivateInputField();
    }

    // blankLineBefore starts a new exchange (a blank line separates it from
    // the previous one) — used for the echoed command. False keeps a line
    // tight against the one above it — used for a response under its own
    // echoed command, so the pair reads as one block.
    private void AppendLine(string line, bool blankLineBefore)
    {
        if (outputText == null) return;

        string separator = string.IsNullOrEmpty(outputText.text) ? "" : (blankLineBefore ? "\n\n" : "\n");
        outputText.text += separator + line;

        TrimHistory();
        SnapScrollToBottom();
    }

    // Keeps memory/text length bounded over a long play session — this is
    // a safety net, not the fix for the overlap issue (the scroll view is).
    private void TrimHistory()
    {
        var lines = outputText.text.Split('\n');
        if (lines.Length <= maxLines) return;

        outputText.text = string.Join("\n", lines, lines.Length - maxLines, maxLines);
    }

    private void SnapScrollToBottom() => SetScroll(0f);

    private void RestoreScrollPosition() => SetScroll(savedScrollPos);

    private void SetScroll(float normalizedPos)
    {
        if (scrollRect == null) return;

        // Content has to actually recalculate its new (taller) height
        // before we scroll, or ScrollRect is still working off last
        // frame's shorter height and the scroll position ends up wrong.
        if (contentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = normalizedPos;
    }

    private TerminalCommand FindCommand(string playerText)
    {
        string[] words = playerText.Trim().ToLower()
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0) return null;

        lookup.TryGetValue(words[0], out TerminalCommand command);
        return command;
    }

    public string ProcessInput(string playerText)
    {
        string[] words = playerText.Trim().ToLower()
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
            return "I don't understand that.";

        if (!lookup.TryGetValue(words[0], out TerminalCommand command))
            return "Command not recognized.";

        return command.Execute(this, words);
    }
}