using UnityEngine;

public class SceneAmbience : MonoBehaviour
{
    [SerializeField] private AudioEvent ambienceEvent;
    [SerializeField] private float fadeInSeconds = 1.5f;

    private void Start()
    {
        if (AudioManager.instance == null) return;
        AudioManager.instance.PlayAmbience(ambienceEvent, fadeInSeconds);
    }
}