using UnityEngine;

public class PlayerArmsAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator armsAnimator;

    // This rig lives under its own body's CameraFPS (Player Office/Player
    // Mission > CameraFPS > ... > FP_Arms), which is exactly what gets
    // toggled on/off on possession — so this always finds the right body
    // and never needs to check "is this body currently possessed" itself:
    // Update() simply doesn't run while that hierarchy is inactive.
    private PlayerBody body;

    private void Start()
    {
        body = GetComponentInParent<PlayerBody>();

        if (armsAnimator == null)
            armsAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (body == null) return;

        UpdateAnimationStates();
    }

    private void UpdateAnimationStates()
    {
        armsAnimator.SetBool("Airing", body.isAiring);
        armsAnimator.SetBool("Grounded", body.isGrounded);
        armsAnimator.SetFloat("Speed", body.currentHorizontalSpeed);
    }
}