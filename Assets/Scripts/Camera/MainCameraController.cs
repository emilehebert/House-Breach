using System.Collections;
using UnityEngine;
using Cinemachine;

// The one real Camera/AudioListener/CinemachineBrain for all gameplay
// scenes (Office, House01, future locations) — persists across scene loads,
// same singleton pattern as GameManager/AudioManager/ScenesManager. Every
// possessable body only carries its own lightweight CinemachineVirtualCamera
// and competes for THIS camera via the usual enable/disable + priority
// rules; nothing ever needs its own real Camera again.
public class MainCameraController : MonoBehaviour
{
    public static MainCameraController Instance { get; private set; }

    private CinemachineBrain brain;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        brain = GetComponent<CinemachineBrain>();
    }

    // Activates a body's vcam as an instant cut instead of the brain's usual
    // smooth blend. Needed specifically for possession swaps — the office
    // worker and a candidate can be in completely different, far-apart
    // locations, so blending between them means the camera visibly flies
    // through the world (through floors, walls, etc.) over the blend
    // duration. Chair/screen focus zooms don't go through here — they use
    // their own dedicated vcams and keep the brain's normal smooth blend,
    // which is correct for a "look at this thing" zoom.
    public void CutTo(GameObject vcamObject)
    {
        if (vcamObject == null) return;

        if (brain == null)
        {
            vcamObject.SetActive(true);
            return;
        }

        var savedBlend = brain.m_DefaultBlend;
        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
        vcamObject.SetActive(true);
        StartCoroutine(ForceCut(savedBlend));
    }

    // CinemachineBrain detects "the active vcam changed" and starts a blend
    // lazily, during its OWN LateUpdate — not reactively the instant
    // SetActive() runs. Restoring m_DefaultBlend synchronously in the same
    // call (the first version of this method) therefore did nothing: the
    // brain hadn't even looked yet, so it always saw the restored, slow
    // value. Holding Cut across a few real frames — and actively canceling
    // any blend that manages to start anyway — is what actually works.
    private IEnumerator ForceCut(CinemachineBlendDefinition savedBlend)
    {
        for (int i = 0; i < 3; i++)
        {
            if (brain.IsBlending)
                brain.ActiveBlend = null;
            yield return null;
        }

        brain.m_DefaultBlend = savedBlend;
    }
}
