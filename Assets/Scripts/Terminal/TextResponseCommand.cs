using System.Collections.Generic;
using UnityEngine;

// Replaces TerminalVerb — this is the "just print fixed text" case,
// now living as one concrete kind of TerminalCommand instead of its own
// separate system. Right-click -> Create -> Terminal -> Commands -> Text Response
[CreateAssetMenu(menuName = "Terminal/Commands/Text Response")]
public class TextResponseCommand : TerminalCommand
{
    [Tooltip("Response when this command is typed alone with no noun, e.g. just \"clear\"")]
    [TextArea] public string standaloneResponse;

    [System.Serializable]
    public class NounEntry
    {
        [Tooltip("Every accepted spelling of this noun, e.g. \"fish\", \"fisch\"")]
        public List<string> aliases;
        [TextArea] public string response;
    }

    [Tooltip("Nouns this command understands (leave empty if it's standalone-only)")]
    public List<NounEntry> compatibleNouns;

    public override string Execute(SimpleTerminal terminal, string[] words)
    {
        if (words.Length == 1)
        {
            return string.IsNullOrWhiteSpace(standaloneResponse)
                ? "I don't understand that."
                : standaloneResponse;
        }

        string noun = words[1];

        if (compatibleNouns != null)
        {
            foreach (var entry in compatibleNouns)
            {
                if (entry.aliases == null) continue;

                foreach (var alias in entry.aliases)
                {
                    if (!string.IsNullOrWhiteSpace(alias) && alias.Trim().ToLower() == noun)
                        return entry.response;
                }
            }
        }

        return "Command not recognized.";
    }
}