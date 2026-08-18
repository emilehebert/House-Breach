using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        // Find the main camera automatically
        mainCam = Camera.main;
    }

    // We use LateUpdate to make sure the camera has finished moving before we rotate
    // This prevents the quad from "jittering" or lagging behind
    void LateUpdate()
    {
        // OPTION 1: Simple LookAt (Points the Z-axis at the camera)
        // transform.LookAt(mainCam.transform);

        // OPTION 2: Match Camera Rotation (Best for Lens Flares/UI)
        // This keeps the quad perfectly flat against the screen, so it doesn't distort at the edges.
        transform.rotation = mainCam.transform.rotation;
    }
}