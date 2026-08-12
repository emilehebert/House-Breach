using UnityEngine;
using UnityEngine.Events;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Handle")]
    [SerializeField] private GameObject handle;

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip animationClipOpen;
    [SerializeField] private AnimationClip animationClipClose;

    [Header("Door Settings")]
    [SerializeField] private bool canOpenBothWays = true;
    [SerializeField] private bool isLocked = false;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private AnimationCurve openingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool closeAutomatically = true;
    [SerializeField] private float autoCloseDelay = 3f;
    
    [Header("Rotation Axis")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;
    [SerializeField] private Vector3 customAxis = Vector3.up;
    
    [Header("Events (no parameters!)")]
    public UnityEvent OnDoorOpened;
    public UnityEvent OnDoorClosed;
    public UnityEvent OnDoorLocked;

    public enum RotationAxis
    {
        X,
        Y,
        Z,
        Custom
    }
    
    private Quaternion originalRotation;
    private Quaternion targetRotation;
    private Quaternion startRotation;
    private bool isOpen = false;
    private bool isMoving = false;
    private float animationTime = 0f;
    private Coroutine autoCloseCoroutine;

    private void Start()
    {
        originalRotation = transform.rotation;
        targetRotation = originalRotation;
    }

    private void Update()
    {
        if (!isMoving) return;

        animationTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(animationTime / animationDuration);
        float curveValue = openingCurve.Evaluate(normalizedTime);
        
        transform.rotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);

        if (normalizedTime >= 1f)
        {
            transform.rotation = targetRotation;
            isMoving = false;
            animationTime = 0f;

            if (isOpen)
            {
                
                if (closeAutomatically)
                {
                    if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
                    autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
                }
            }
            else
            {
                OnDoorClosed.Invoke();
            }
        }
    }

    public string GetInteractText()
    {
        if (isLocked) 
            return "Door is locked";
        return isOpen ? "E to Close" : "E to Open";
    }
    
    public AnimationClip GetInteractAnimation()
    {
        if (isLocked || isMoving) 
            return null;
        return isOpen ? animationClipClose : animationClipOpen;
    }
    
    public void Interact()
    {
        InteractWithDoor(Vector3.zero);
    }
    
    public void Lock() => SetLocked(true);
    public void Unlock() => SetLocked(false);
    public void ToggleLock() => SetLocked(!isLocked);

    // —— Core logic —— 
    private void InteractWithDoor(Vector3 interactionPoint)
    {
        if (isLocked)
        {
            OnDoorLocked.Invoke();
            return;
        }

        if (isMoving) return;
        if (isOpen) CloseDoor();
        else       OpenDoor(interactionPoint);
    }

    public void OpenDoor(Vector3 interactionPoint)
    {
        if (isLocked || isMoving || isOpen) return;

        OnDoorOpened.Invoke();

        float rotationDirection = 1f;
        Vector3 rotationAxisVector = GetRotationAxisVector();
        
        if (canOpenBothWays)
        {
            Vector3 playerPosition = Camera.main ? Camera.main.transform.position : interactionPoint;
            Vector3 doorToPlayer = (playerPosition - transform.position);
            Vector3 projected = Vector3.ProjectOnPlane(doorToPlayer, rotationAxisVector);
            
            // Get the reference direction based on the rotation axis
            Vector3 referenceDirection = GetReferenceDirection(rotationAxisVector);
            float dot = Vector3.Dot(projected.normalized, referenceDirection);
            rotationDirection = dot > 0 ? 1f : -1f;
        }

        // Create rotation based on the selected axis
        Vector3 eulerRotation = rotationAxisVector * (openAngle * rotationDirection);
        startRotation = transform.rotation;
        targetRotation = originalRotation * Quaternion.Euler(eulerRotation);
        
        isOpen = true;
        isMoving = true;
        animationTime = 0f;
    }

    public void CloseDoor()
    {
        if (isMoving || !isOpen) return;
        if (autoCloseCoroutine != null) { StopCoroutine(autoCloseCoroutine); autoCloseCoroutine = null; }
        
        startRotation = transform.rotation;
        targetRotation = originalRotation;
        isOpen = false;
        isMoving = true;
        animationTime = 0f;
    }

    private Vector3 GetRotationAxisVector()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                return Vector3.right;
            case RotationAxis.Y:
                return Vector3.up;
            case RotationAxis.Z:
                return Vector3.forward;
            case RotationAxis.Custom:
                return customAxis.normalized;
            default:
                return Vector3.up;
        }
    }

    private Vector3 GetReferenceDirection(Vector3 axisVector)
    {
        // Get a reference direction perpendicular to the rotation axis
        if (Mathf.Abs(Vector3.Dot(axisVector, Vector3.forward)) < 0.9f)
            return Vector3.Cross(axisVector, Vector3.forward).normalized;
        else
            return Vector3.Cross(axisVector, Vector3.right).normalized;
    }

    private void SetLocked(bool locked) => isLocked = locked;

    private System.Collections.IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        CloseDoor();
        autoCloseCoroutine = null;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Application.isPlaying
            ? (isLocked ? Color.red : (isOpen ? Color.blue : Color.yellow))
            : Color.green;

        Vector3 axisVector = GetRotationAxisVector();
        Vector3 referenceDir = GetReferenceDirection(axisVector);
        
        // Draw rotation axis
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, axisVector * 2f);
        
        // Draw reference direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, referenceDir * 1.5f);
        
        if (canOpenBothWays)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}