using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Sources")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource sfxSource;
    public AudioSource GetAmbientSource() => ambientSource;
    public AudioSource GetSfxSource() => sfxSource;
    public const string AMBIANT_KEY = "ambiantVolume";
    public const string SFX_KEY = "sfxVolume";

    private Coroutine ambienceRoutine;
    private AudioEvent currentAmbienceEvent;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        AudioEvent.GlobalAudioSource = sfxSource;
        LoadVolume();
    }

    public void PlayAmbientEvent(AudioEvent ev) => ev.PlayRandom(ambientSource); // unchanged, still usable for one-shot ambient stingers
    public void PlaySfxEvent(AudioEvent ev) => ev.PlayRandom(sfxSource);

    // NEW: looping ambience with crossfade
    public void PlayAmbience(AudioEvent ambienceEvent, float fadeSeconds = 1.5f)
    {
        if (ambienceEvent == currentAmbienceEvent && ambientSource.isPlaying) return;
        currentAmbienceEvent = ambienceEvent;

        AudioClip clip = ambienceEvent != null ? ambienceEvent.PickClip() : null;

        if (ambienceRoutine != null) StopCoroutine(ambienceRoutine);
        ambienceRoutine = StartCoroutine(CrossfadeAmbience(clip, fadeSeconds));
    }

    public void StopAmbience(float fadeSeconds = 1.5f)
    {
        currentAmbienceEvent = null;
        if (ambienceRoutine != null) StopCoroutine(ambienceRoutine);
        ambienceRoutine = StartCoroutine(CrossfadeAmbience(null, fadeSeconds));
    }

    private IEnumerator CrossfadeAmbience(AudioClip newClip, float duration)
    {
        float startVolume = ambientSource.volume;
        float half = duration / 2f;

        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            ambientSource.volume = Mathf.Lerp(startVolume, 0f, t / half);
            yield return null;
        }
        ambientSource.volume = 0f;

        ambientSource.clip = newClip;
        ambientSource.loop = true;
        if (newClip != null) ambientSource.Play();
        else ambientSource.Stop();

        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        {
            ambientSource.volume = Mathf.Lerp(0f, startVolume, t / half);
            yield return null;
        }
        ambientSource.volume = startVolume;
    }

    void LoadVolume()
    {
        float ambiantVolume = PlayerPrefs.GetFloat(AMBIANT_KEY, 1f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        mixer.SetFloat(VolumeSettings.MIXER_AMBIANT, Mathf.Log10(Mathf.Max(0.0001f, ambiantVolume)) * 20);
        mixer.SetFloat(VolumeSettings.MIXER_SFX, Mathf.Log10(Mathf.Max(0.0001f, sfxVolume)) * 20);
    }
}