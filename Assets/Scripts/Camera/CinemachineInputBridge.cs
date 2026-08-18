using UnityEngine;
using Cinemachine;

public class CinemachineInputBridge : MonoBehaviour
{
    // Make sure these match the "Input Axis Name" in your Cinemachine Inspector
    public string horizontalAxisName = "Mouse X";
    public string verticalAxisName = "Mouse Y";

    private InputManager inputManager;

    void Start()
    {
        inputManager = InputManager.Instance;
        
        // This tells Cinemachine: "When you need input, ask this function."
        CinemachineCore.GetInputAxis = GetInputAxisCustom;
    }

    private float GetInputAxisCustom(string axisName)
    {
        if (inputManager == null) return 0;

        Vector2 delta = inputManager.GetMouseDelta();

        if (axisName == horizontalAxisName)
            return delta.x;

        if (axisName == verticalAxisName)
            return delta.y;

        return 0;
    }
    
    // Cleanup the delegate when this object is destroyed
    void OnDestroy()
    {
        CinemachineCore.GetInputAxis = null;
    }
}