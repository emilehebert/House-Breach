using TMPro;
using UnityEngine;

// Shows an ambient world clock while in the Office (running continuously
// since the game started), or how long the current mission has been going
// once you're actually out on one (resets to 0:00 am each time a new
// mission starts). Same format either way — just a different source of
// minutes depending on GamePhaseTracker.Current.
public class TerminalClock : MonoBehaviour
{
    [SerializeField] private TMP_Text clockText;

    [Tooltip("How many in-game minutes pass per real second. 1 = a full 12-hour cycle takes 12 real minutes.")]
    [SerializeField] private float minutesPerRealSecond = 1f;

    // Only re-render the text when the displayed minute actually changes,
    // instead of every single frame.
    private int lastDisplayedMinute = -1;

    void Update()
    {
        if (clockText == null) return;

        // In a mission: count from when THIS mission started (resets each
        // time). In the Office: count from when the game started (keeps
        // running continuously, since Time.time never resets on its own).
        float realSecondsElapsed = GamePhaseTracker.Current == GamePhase.Mission
            ? MissionData.ElapsedMinutes * 60f
            : Time.time;

        int totalMinutes = Mathf.FloorToInt(realSecondsElapsed * minutesPerRealSecond);
        if (totalMinutes == lastDisplayedMinute) return;

        lastDisplayedMinute = totalMinutes;

        int hour24 = (totalMinutes / 60) % 24;
        int minute = totalMinutes % 60;

        // 0-11 instead of the usual 1-12 clock convention, to match your
        // "0:00 am" style rather than a real-world "12:00 am".
        int hour12 = hour24 % 12;
        string period = hour24 < 12 ? "am" : "pm";

        clockText.text = $"{hour12}:{minute:00} {period}";
    }
}