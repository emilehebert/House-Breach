using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class EventZone : MonoBehaviour
{
    [Header("Filter")]
    public LayerMask triggerLayers = ~0;

    [Header("Timing")]
    [Tooltip("Minimum seconds between triggers")]
    public float cooldown = 0f;
    [Tooltip("If true, zone disables itself after first trigger")]
    public bool oneShot = false;

    [Header("Zone Events")]
    [Tooltip("Collider-enter event (passes the Collider)")]
    public UnityEvent<Collider> OnEnter;
    [Tooltip("Collider-exit event (passes the Collider)")]
    public UnityEvent<Collider> OnExit;
    [Tooltip("Collider-stay event (passes the Collider)")]
    public UnityEvent<Collider> OnStay;

    private float _lastTriggerTime = -Mathf.Infinity;
    private BoxCollider _col;
    private bool _hasTriggered = false; // Track if zone has been triggered

    void Reset()
    {
        _col = GetComponent<BoxCollider>();
        _col.isTrigger = true;
    }

    void Awake()
    {
        _col = GetComponent<BoxCollider>();
        _col.isTrigger = true;
    }

    bool IsValid(Collider other)
    {
        // Check if one-shot has already been triggered
        if (oneShot && _hasTriggered) return false;
        
        return ((1 << other.gameObject.layer) & triggerLayers) != 0
               && Time.time - _lastTriggerTime >= cooldown;
    }

    void HandleTrigger(UnityEvent<Collider> e, Collider other)
    {
        _lastTriggerTime = Time.time;
        e.Invoke(other);
        
        if (oneShot) 
        {
            _hasTriggered = true;
            // Optionally disable the collider completely
            _col.enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsValid(other)) HandleTrigger(OnEnter, other);
    }

    void OnTriggerStay(Collider other)
    {
        if (IsValid(other)) HandleTrigger(OnStay, other);
    }

    void OnTriggerExit(Collider other)
    {
        if (IsValid(other)) HandleTrigger(OnExit, other);
    }
}