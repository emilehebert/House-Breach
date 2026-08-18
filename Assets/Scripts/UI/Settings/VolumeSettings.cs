using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
public class VolumeSettings : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider ambiantSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] TMP_Text ambiantValue;
    [SerializeField] TMP_Text sfxValue;
    public const string MIXER_AMBIANT = "AmbiantVolume";
    public const string MIXER_SFX = "SFXVolume";

    void Awake()
    {
        ambiantSlider.onValueChanged.AddListener(SetAmbiantVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    void Start()
    {
        ambiantSlider.value = PlayerPrefs.GetFloat(AudioManager.AMBIANT_KEY, .25f);
        sfxSlider.value = PlayerPrefs.GetFloat(AudioManager.SFX_KEY, .25f);

        SetAmbiantVolume(ambiantSlider.value);
        SetSFXVolume(sfxSlider.value);
    }

    void SetAmbiantVolume(float value)
    {
        // update UI
        int pct = Mathf.RoundToInt(value * 100);
        ambiantValue.text = $"{pct} %";
        
        mixer.SetFloat(MIXER_AMBIANT, Mathf.Log10(value) * 20);

        PlayerPrefs.SetFloat(AudioManager.AMBIANT_KEY, value);
    }
    void SetSFXVolume(float value)
    {
        // update UI
        int pct = Mathf.RoundToInt(value * 100);
        sfxValue.text = $"{pct} %";

        mixer.SetFloat(MIXER_SFX, Mathf.Log10(value) * 20);

        PlayerPrefs.SetFloat(AudioManager.SFX_KEY, value);
    }
    void OnDisable()
    {
        PlayerPrefs.SetFloat(AudioManager.AMBIANT_KEY, ambiantSlider.value);
        PlayerPrefs.SetFloat(AudioManager.SFX_KEY, sfxSlider.value);
        PlayerPrefs.Save();
    }
}
