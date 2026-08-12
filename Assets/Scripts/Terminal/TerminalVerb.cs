using System.Collections.Generic;
using UnityEngine;

// Right-click in the Project window -> Create -> Terminal -> Verb
[CreateAssetMenu(menuName = "Terminal/Verb")]
public class TerminalVerb : ScriptableObject
{
    [Tooltip("Every accepted spelling of this verb, e.g. \"clear\", \"clr\", \"cleer\"")]
    public List<string> aliases;

    [Tooltip("Optional: response when this verb is typed alone with no noun, e.g. just \"clear\"")]
    [TextArea] public string standaloneResponse;

    [System.Serializable]
    public class NounEntry
    {
        [Tooltip("Every accepted spelling of this noun, e.g. \"fish\", \"fisch\"")]
        public List<string> aliases;
        [TextArea] public string response;
    }

    [Tooltip("Nouns this verb understands (leave empty if it's standalone-only)")]
    public List<NounEntry> compatibleNouns;
}