using UnityEngine;

// Ends the mission successfully once the player reaches this point — the
// "walk back to the van" moment. Reuses EventZone for the trigger volume
// itself (layer filter, cooldown, one-shot) instead of reimplementing it.
// Default mode just needs the player to walk in; set Require Interaction if
// a specific extraction point should need a keypress instead (e.g. opening
// a locked van door).
[RequireComponent(typeof(EventZone))]
public class MissionExitZone : MonoBehaviour, IInteractable
{
    [Header("Result")]
    [SerializeField] string resultSummary = "Mission complete";

    [Header("Trigger Mode")]
    [SerializeField] bool requireInteraction = false;
    [SerializeField] string interactText = "Leave";

    EventZone zone;

    void Awake()
    {
        zone = GetComponent<EventZone>();
        zone.OnEnter.AddListener(HandleZoneEnter);
    }

    void HandleZoneEnter(Collider other)
    {
        if (!requireInteraction)
            CompleteMission();
    }

    public void Interact()
    {
        if (requireInteraction)
            CompleteMission();
    }

    public string GetInteractText() => requireInteraction ? interactText : null;
    public AnimationClip GetInteractAnimation() => null;

    void CompleteMission()
    {
        MissionData.ReportResult(succeeded: true, resultSummary);

        if (ScenesManager.Instance != null)
            ScenesManager.Instance.LoadScene(ScenesManager.Scene.Office);
    }
}
