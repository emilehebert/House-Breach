using UnityEngine;
using Cinemachine;

// Attach anywhere on the monitor object (same object as its
// FocusInteractable is fine). Wire EnterCandidate() to On Focus Enter,
// and ExitCandidate() to On Focus Exit.
public class CandidateFocusHandoff : MonoBehaviour
{
    [SerializeField] private GameObject officeCharacter;
    [SerializeField] private GameObject candidateCharacter;

    [Tooltip("The candidate's own Cinemachine camera.")]
    [SerializeField] private CinemachineVirtualCamera candidateCamera;
    [Tooltip("Must be HIGHER than the monitor's own Focus Priority (usually 30), or the screen zoom-in camera keeps winning.")]
    [SerializeField] private int candidateCameraPriority = 40;

    private int defaultCandidateCameraPriority;

    void Start()
    {
        if (candidateCamera != null)
            defaultCandidateCameraPriority = candidateCamera.Priority;
    }

    public void EnterCandidate()
    {
        if (officeCharacter != null) officeCharacter.SetActive(false);
        if (candidateCharacter != null) candidateCharacter.SetActive(true);

        if (candidateCamera != null)
            candidateCamera.Priority = candidateCameraPriority;
    }

    public void ExitCandidate()
    {
        if (candidateCamera != null)
            candidateCamera.Priority = defaultCandidateCameraPriority;

        if (candidateCharacter != null) candidateCharacter.SetActive(false);
        if (officeCharacter != null) officeCharacter.SetActive(true);
    }
}