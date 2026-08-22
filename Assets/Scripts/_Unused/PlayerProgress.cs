// Plain serializable data blob for JsonUtility — no logic here beyond a
// derived display value, so SaveSystem stays the one place that mutates it.
[System.Serializable]
public class PlayerProgress
{
    public int currency;
    public int xp;

    // Placeholder leveling curve — 100 xp per level. Tune freely, nothing
    // else depends on this specific number.
    public int Level => 1 + xp / 100;
}
