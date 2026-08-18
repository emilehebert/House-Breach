using UnityEngine;

// Shared contract for anything PlayerDriver can possess. PlayerBody
// (candidate — full moveset) and OfficeController (office worker — walk
// only) both implement this so PlayerDriver can drive either one without
// caring which. GameObject/IsGrounded/CurrentHorizontalSpeed are here
// because other systems (MountableChair, CinemachineHeadBob) need them
// regardless of which body type is currently possessed.
public interface IPossessable
{
    GameObject GameObject { get; }
    bool IsPossessed { get; }
    bool IsGrounded { get; }
    float CurrentHorizontalSpeed { get; }

    // Called by PlayerDriver only while this body is the one possessed.
    void Tick(Vector2 moveInput, bool jumpInput, bool runInput, bool crouchInput);

    void SetCameraActive(bool active);
}
