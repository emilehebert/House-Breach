using UnityEngine;

// Right-click in the Project window -> Create -> Terminal -> Commands -> Dispatch
// "dispatch [candidate] [location]" — e.g. "dispatch sarah house01".
[CreateAssetMenu(menuName = "Terminal/Commands/Dispatch")]
public class DispatchCommand : TerminalCommand
{
    public override string Execute(SimpleTerminal terminal, string[] words)
    {
        if (words.Length < 3)
            return "Usage: dispatch [candidate] [location]";

        string candidateName = words[1];
        string locationWord = words[2];

        Candidate candidate = terminal.candidates.Find(c =>
            c != null && c.candidateName.Trim().ToLower() == candidateName);

        if (candidate == null)
            return $"Unknown candidate: {candidateName}";

        if (!System.Enum.TryParse(locationWord, ignoreCase: true, out ScenesManager.Scene locationScene))
            return $"Unknown location: {locationWord}";

        if (ScenesManager.Instance == null)
            return "Scene manager not found.";

        DispatchData.StageDispatch(candidate, locationScene);
        ScenesManager.Instance.LoadScene(locationScene);

        return $"Dispatching {candidate.candidateName} to {locationScene}...";
    }
}