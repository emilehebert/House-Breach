using System.Collections.Generic;
using UnityEngine;

// Base class for terminal commands that need to actually DO something —
// dispatch, hire, fire, call, etc. — instead of just printing fixed text
// (that's what TerminalVerb is for). Make a small subclass per command
// with its own Execute() logic, create an asset from it, and drag it into
// SimpleTerminal's Commands list — same workflow as TerminalVerb assets.
public abstract class TerminalCommand : ScriptableObject
{
    [Tooltip("Every accepted spelling of this command, e.g. \"dispatch\", \"disp\"")]
    public List<string> aliases;

    [Tooltip("If true, this command wipes the terminal's output log instead of adding a line to it (used by Clear)")]
    public bool clearsScreen = false;

    // words[0] is the command itself; words[1], words[2], etc. are its
    // arguments, already lowercased and trimmed by SimpleTerminal.
    public abstract string Execute(SimpleTerminal terminal, string[] words);
}