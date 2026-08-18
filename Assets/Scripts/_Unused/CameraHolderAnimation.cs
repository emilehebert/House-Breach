using UnityEngine;

public class CameraHolderAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    public AnimationCurve runningCurve; // Curve defining the X rotation during running
    public float baseAnimationSpeed = 1f; // Base animation speed multiplier
    public float smoothSpeed = 5f; // Speed to reset rotation to 0

    [Header("References")]
    public Transform cameraHolder; // The GameObject to rotate
    public PlayerController playerMovement; // Reference to the PlayerMovement script
    public FootstepSoundPlayer footstepAudio; // Reference to the FootstepAudio script

    private float animationTime = 0f; // Keeps track of curve time
    private bool hasPlayedHalfway = false; // Flag to check if the sound has played halfway

    void Update()
    {
        if (cameraHolder == null || playerMovement == null || footstepAudio == null)
            return;

        // Prevent animation if not grounded
        if (!playerMovement.isGrounded)
        {
            ResetRotation();
            return;
        }

        // Update animation speed based on player movement
        baseAnimationSpeed = Mathf.Max(playerMovement.currentHorizontalSpeed, 0f);

        if (baseAnimationSpeed > 0.1f) // If walking or running
        {
            // Increment animation time
            animationTime += Time.deltaTime * baseAnimationSpeed;

            // Restart animation if it completes
            if (animationTime > runningCurve.keys[runningCurve.length - 1].time)
            {
                animationTime = 0f;
                hasPlayedHalfway = false; // Reset halfway flag for next loop
            }

            // Check if halfway point is reached and play sound
            float halfwayTime = runningCurve.keys[runningCurve.length - 1].time / 2f;
            if (!hasPlayedHalfway && animationTime >= halfwayTime)
            {
                footstepAudio.GetMaterial();
                hasPlayedHalfway = true;
            }

            // Apply the curve rotation
            float xRotation = runningCurve.Evaluate(animationTime);
            Vector3 currentRotation = cameraHolder.localRotation.eulerAngles;
            cameraHolder.localRotation = Quaternion.Euler(xRotation, currentRotation.y, currentRotation.z);
        }
        else
        {
            // Reset rotation to 0 linearly
            ResetRotation();
            animationTime = 0f;
        }
    }

    private void ResetRotation()
    {
        Vector3 currentRotation = cameraHolder.localRotation.eulerAngles;
        float newXRotation = Mathf.MoveTowards(currentRotation.x, 0f, Time.deltaTime * smoothSpeed);
        cameraHolder.localRotation = Quaternion.Euler(newXRotation, currentRotation.y, currentRotation.z);
    }
}
