using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Event", fileName = "NewAudioEvent")]
public class AudioEvent : ScriptableObject
{
    public enum AudioChannel { Sfx, Ambient }

    [System.Serializable]
    public struct SoundEntry
    {
        public AudioClip clip;
        [Min(1f)] public float weight;
    }

    [SerializeField] private List<SoundEntry> sounds = new List<SoundEntry>();
    [SerializeField] private Vector2 pitchRange = new Vector2(1f, 1f);
    [SerializeField] private Vector2 volumeRange = new Vector2(1f, 1f);

    public static AudioSource GlobalAudioSource { get; set; }

    /// <summary>Pour les UnityEvent (Inspector) : choisis Sfx ou Ambient, résolu via AudioManager.instance.</summary>
    public void Play(AudioChannel channel = AudioChannel.Sfx)
    {
        if (AudioManager.instance == null) return;

        AudioSource source = channel == AudioChannel.Ambient
            ? AudioManager.instance.GetAmbientSource()
            : AudioManager.instance.GetSfxSource();

        PlayRandom(source);
    }

    public void PlayRandom(AudioSource audioSource = null)
    {
        if (audioSource == null) audioSource = GlobalAudioSource; // "==" pas "??" : respecte le fake-null d'Unity
        if (audioSource == null)
        {
            Debug.LogWarning($"{name}: No AudioSource provided to PlayRandom()");
            return;
        }

        AudioClip clip = GetRandomClip();
        if (clip == null) return;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, Random.Range(volumeRange.x, volumeRange.y));
    }

    public AudioClip PickClip() => GetRandomClip();

    private AudioClip GetRandomClip()
    {
        float total = 0f;
        foreach (var s in sounds) total += s.weight;
        if (total <= 0f) return null;

        float r = Random.Range(0f, total);
        foreach (var s in sounds)
        {
            if (r <= s.weight) return s.clip;
            r -= s.weight;
        }
        return sounds[sounds.Count - 1].clip;
    }
}