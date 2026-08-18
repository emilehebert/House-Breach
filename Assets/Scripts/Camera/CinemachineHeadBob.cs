using UnityEngine;
using Cinemachine; // Uses the version compatible with your project

public class CinemachineHeadBob : CinemachineExtension
{
    [Header("Animation Settings")]
    public AnimationCurve runningCurve; // Curve defining the X rotation
    public float smoothSpeed = 5f;      // Speed to reset rotation to 0

    [Header("References")]
    public FootstepSoundPlayer footstepAudio;

    private float animationTime = 0f;
    private bool hasPlayedHalfway = false;
    private float currentXBob = 0f; // The calculated rotation to apply

    // This is the Magic Method where Cinemachine allows us to edit the camera
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // Apply rotation only after the 'Aim' stage (when the camera has decided where to look)
        if (stage == CinemachineCore.Stage.Aim)
        {
            // Create a rotation for just the Bob (Pitching up/down locally)
            Quaternion bobRotation = Quaternion.Euler(currentXBob, 0, 0);

            // Add this rotation on top of the existing camera orientation
            state.RawOrientation *= bobRotation;
        }
    }

    // We use Update to calculate the timing and logic, just like your old script
    void Update()
    {
        if (PlayerDriver.Instance == null || PlayerDriver.Instance.CurrentBody == null) return;
        if (GameManager.IsPaused) return;

        float speed = 0f;
        if (PlayerDriver.Instance.CurrentBody.IsGrounded)
        {
            speed = Mathf.Max(PlayerDriver.Instance.CurrentBody.CurrentHorizontalSpeed, 0f);
        }
    
        if (speed > 0.1f)
        {
            animationTime += Time.deltaTime * speed;
    
            float endTime = runningCurve.keys[runningCurve.length - 1].time;
            if (animationTime > endTime)
            {
                animationTime = 0f;
                hasPlayedHalfway = false;
            }
    
            float halfway = endTime / 2f;
            if (!hasPlayedHalfway && animationTime >= halfway && footstepAudio != null)
            {
                footstepAudio.GetMaterial();
                hasPlayedHalfway = true;
            }
    
            currentXBob = runningCurve.Evaluate(animationTime);
        }
        else
        {
            animationTime = 0f;
            currentXBob = Mathf.MoveTowards(currentXBob, 0f, Time.deltaTime * smoothSpeed);
        }
    }
}