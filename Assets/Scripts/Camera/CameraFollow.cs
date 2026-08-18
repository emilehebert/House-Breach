using UnityEngine;

// Keeps this transform (and anything parented under it, like FP_Arms) locked
// to the shared MainCameraController every frame. Needed because
// CinemachineBrain moves the transform it actually lives on (the persistent
// shared camera) to match the active vcam — it does NOT move the vcam's own
// GameObject. Before the camera-per-body refactor, this object WAS that
// Brain's own transform, so arms parented under it got this for free; now
// that the Brain lives elsewhere, this restores the same effect explicitly.
public class CameraFollow : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = MainCameraController.Instance;
        if (cam == null) return;

        transform.position = cam.transform.position;
        transform.rotation = cam.transform.rotation;
    }
}
