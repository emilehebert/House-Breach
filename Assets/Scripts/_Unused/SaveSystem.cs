using System.IO;
using UnityEngine;

// What's earned from a single mission result — returned to the caller
// (MissionResultsPanel) so it can display "+100 currency, +50 xp" without
// re-deriving the formula itself.
[System.Serializable]
public struct MissionReward
{
    public int currency;
    public int xp;
}

// Minimal JSON-file save for cross-session progress. PlayerPrefs (see
// UI/Settings/) stays fine for individual settings values, but a
// structured blob like this belongs in a real file instead of flat
// registry-style keys — written to persistentDataPath so it survives
// between Editor sessions and builds alike.
public static class SaveSystem
{
    static readonly string SavePath = Path.Combine(Application.persistentDataPath, "progress.json");

    public static PlayerProgress Current { get; private set; } = new PlayerProgress();

    // PARKED — bootstrap disabled. _Unused/ isn't excluded from
    // compilation, so this would otherwise still auto-load progress.json
    // in the background even though nothing reads Current anymore.
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() => Load();

    public static void Load()
    {
        Current = File.Exists(SavePath)
            ? JsonUtility.FromJson<PlayerProgress>(File.ReadAllText(SavePath))
            : new PlayerProgress();
    }

    public static void Save()
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(Current, true));
    }

    // Placeholder v1 economy — flat reward on success (with a small bonus
    // for time survived), a smaller flat consolation amount on a bailed/
    // failed run so leaving early isn't a total waste. Purely a starting
    // point to tune, not a locked design.
    public static MissionReward ApplyMissionResult(bool succeeded, float elapsedMinutes)
    {
        MissionReward reward = new MissionReward
        {
            currency = succeeded ? 100 + Mathf.RoundToInt(elapsedMinutes * 5f) : 25,
            xp = succeeded ? 50 : 10
        };

        Current.currency += reward.currency;
        Current.xp += reward.xp;
        Save();

        return reward;
    }
}
