using UnityEngine;

public class ToggleInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private AnimationClip animationClip;  // assign DoorData once on prefab
    
    [Header("What to Toggle")]
    [SerializeField] private GameObject target;
    [SerializeField] private bool startOn = true;

    [Header("UI Text")]
    [SerializeField] private string interactTextOn = "Turn Off";
    [SerializeField] private string interactTextOff = "Turn On";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioEvent interactAudio;

    private bool isOn;

    private void Awake()
    {
        isOn = startOn;
        if (target != null)
            target.SetActive(isOn);
    }

    // Called by your InteractionSystem when the player activates this
    public void Interact()
    {
        isOn = !isOn;
        if (target != null)
            target.SetActive(isOn);
        interactAudio.PlayRandom(audioSource);
    }

    // What text to show in the interaction prompt
    public string GetInteractText()
    {
        return isOn ? interactTextOn : interactTextOff;
    }

    // Which trigger to fire on the player's animator
    public AnimationClip GetInteractAnimation() => animationClip;

}
