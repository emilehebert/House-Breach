using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float maxSwayAmount = 0.06f;
    [SerializeField] private float swaySmooth = 4f;
    
    [Header("Rotation Sway")]
    [SerializeField] private float rotationSwayAmount = 1f;
    [SerializeField] private float maxRotationSway = 5f;
    
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    
    void Start()
    {
        // Store the initial position and rotation
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }
    
    void Update()
    {
        // Get mouse input — reads the same raw axis camera look would use,
        // so it needs to freeze under the same lock or the weapon keeps
        // swaying while look (and everything else) is paused/focused.
        float mouseX = CameraLookGate.IsLocked ? 0f : Input.GetAxis("Mouse X");
        float mouseY = CameraLookGate.IsLocked ? 0f : Input.GetAxis("Mouse Y");
        
        // Calculate sway position
        Vector3 swayPosition = CalculateSwayPosition(mouseX, mouseY);
        
        // Calculate sway rotation
        Quaternion swayRotation = CalculateSwayRotation(mouseX, mouseY);
        
        // Apply sway with smoothing
        transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition + swayPosition, Time.deltaTime * swaySmooth);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, initialRotation * swayRotation, Time.deltaTime * swaySmooth);
    }
    
    private Vector3 CalculateSwayPosition(float mouseX, float mouseY)
    {
        // Calculate the desired sway amount
        Vector3 sway = new Vector3(-mouseX * swayAmount, -mouseY * swayAmount, 0);
        
        // Clamp the sway to maximum values
        sway.x = Mathf.Clamp(sway.x, -maxSwayAmount, maxSwayAmount);
        sway.y = Mathf.Clamp(sway.y, -maxSwayAmount, maxSwayAmount);
        
        return sway;
    }
    
    private Quaternion CalculateSwayRotation(float mouseX, float mouseY)
    {
        // Calculate rotation sway (opposite direction for natural feel)
        float rotX = -mouseY * rotationSwayAmount;
        float rotY = mouseX * rotationSwayAmount;
        float rotZ = mouseX * rotationSwayAmount;
        
        // Clamp rotation values
        rotX = Mathf.Clamp(rotX, -maxRotationSway, maxRotationSway);
        rotY = Mathf.Clamp(rotY, -maxRotationSway, maxRotationSway);
        rotZ = Mathf.Clamp(rotZ, -maxRotationSway, maxRotationSway);
        
        return Quaternion.Euler(rotX, rotY, rotZ);
    }
}