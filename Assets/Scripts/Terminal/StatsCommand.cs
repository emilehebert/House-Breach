using UnityEngine;

// Right-click in the Project window -> Create -> Terminal -> Commands -> Stats
// "stats [candidate]" — e.g. "stats sarah".
[CreateAssetMenu(menuName = "Terminal/Commands/Stats")]
public class StatsCommand : TerminalCommand
{
    public override string Execute(SimpleTerminal terminal, string[] words)
    {
        if (words.Length < 2)
            return "Usage: stats [candidate]";

        string candidateName = words[1];

        Candidate candidate = terminal.candidates.Find(c =>
            c != null && c.candidateName.Trim().ToLower() == candidateName);

        if (candidate == null)
            return $"Unknown candidate: {candidateName}";

        return $"{candidate.candidateName}\nHeight: {candidate.height}\nWeight: {candidate.weight}\nSpeed: {candidate.speed}";
    }
}
