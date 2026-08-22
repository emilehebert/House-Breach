using UnityEngine;
using Cinemachine;

public class CinemachineInputBridge : MonoBehaviour
{
    // Make sure these match the "Input Axis Name" in your Cinemachine Inspector
    public string horizontalAxisName = "Mouse X";
    public string verticalAxisName = "Mouse Y";

    // Lives on the same DontDestroyOnLoad MainCamera object as MainCameraController,
    // which destroys any duplicate that loads in with a new scene — but not until
    // after that duplicate's own Start() has already run. Without this guard the
    // duplicate would re-hook the static CinemachineCore.GetInputAxis to itself and
    // then null/no-op it again in OnDestroy, breaking the surviving instance's input.
    private static CinemachineInputBridge activeInstance;

    private InputManager inputManager;

    void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            enabled = false;
            return;
        }

        activeInstance = this;
    }

    void Start()
    {
        if (activeInstance != this) return;

        inputManager = InputManager.Instance;

        // This tells Cinemachine: "When you need input, ask this function."
        CinemachineCore.GetInputAxis = GetInputAxisCustom;
    }

    void OnEnable()
    {
        if (activeInstance != this) return;

        // Re-hook in case this component was disabled/re-enabled without OnDestroy running
        // (e.g. object pooling), which would otherwise leave Cinemachine's static delegate stale.
        CinemachineCore.GetInputAxis = GetInputAxisCustom;
    }

    private float GetInputAxisCustom(string axisName)
    {
        if (inputManager == null) return 0;
        if (CameraLookGate.IsLocked) return 0;

        Vector2 delta = inputManager.GetMouseDelta();

        if (axisName == horizontalAxisName)
            return delta.x;

        if (axisName == verticalAxisName)
            return delta.y;

        return 0;
    }

    // Reset to a safe no-op instead of null: CinemachineCore.GetInputAxis is a static
    // delegate shared by every Cinemachine camera, and AxisState.Update() only catches
    // ArgumentException around it, so leaving it null throws an uncaught NullReferenceException
    // every frame as soon as any vcam updates.
    void OnDestroy()
    {
        if (activeInstance != this) return;

        CinemachineCore.GetInputAxis = delegate { return 0f; };
        activeInstance = null;
    }
}
