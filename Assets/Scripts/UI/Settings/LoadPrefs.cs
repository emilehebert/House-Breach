using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using Cinemachine; // Use 'using Cinemachine;' if you are on an older version

public class LoadPrefs : MonoBehaviour
{
    [Header("Cinemachine Script")]
    //[SerializeField] private CinemachinePOVExtension cinemachineScript;
    [SerializeField] CinemachineVirtualCamera vcam; // Drag your Virtual Camera here
    private CinemachinePOV _pov;


    [Header("General Setting")]
    [SerializeField] private bool canUse = false;
    [SerializeField] private MenuManager menuController;

    [Header("Audio System")]
    [SerializeField] private AudioMixer mixer;

    [Header("Volume Setting")]
    [SerializeField] private Slider ambiantSlider;
    [SerializeField] private TMP_Text ambiantValue;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TMP_Text sfxValue;

    [Header("Sensitivity Setting")]
    [SerializeField] private Slider ySensitivitySlider;
    [SerializeField] private TMP_Text ySensitivityValue;
    [SerializeField] private Slider xSensitivitySlider;
    [SerializeField] private TMP_Text xSensitivityValue;

    private void Awake() 
    {
        // Get the POV component directly from the camera
        _pov = vcam.GetCinemachineComponent<CinemachinePOV>();

        if (canUse)
        {
            FindVCam();
            LoadSensitivitySettings();
        }
    }

    private void Start()
    {
        if (canUse)
        {
            // Load audio settings after AudioManager has initialized
            LoadAudioSettings();
        }
    }

    private void FindVCam()
    {
        if (vcam == null)
        {
            GameObject vcamObj = GameObject.FindWithTag("MainVCam");
            if (vcamObj != null)
                vcam = vcamObj.GetComponent<CinemachineVirtualCamera>();
        }

        if (vcam != null && _pov == null)
            _pov = vcam.GetCinemachineComponent<CinemachinePOV>();
    }

    private void LoadAudioSettings()
    {
        if (PlayerPrefs.HasKey(AudioManager.AMBIANT_KEY))
        {
            float localVolumeAmbiant = PlayerPrefs.GetFloat(AudioManager.AMBIANT_KEY);
            float localVolumeSFX = PlayerPrefs.GetFloat(AudioManager.SFX_KEY);

            // Update UI sliders and text (don't trigger events)
            ambiantSlider.SetValueWithoutNotify(localVolumeAmbiant);
            ambiantValue.text = $"{Mathf.RoundToInt(localVolumeAmbiant * 100)} %";
            
            sfxSlider.SetValueWithoutNotify(localVolumeSFX);
            sfxValue.text = $"{Mathf.RoundToInt(localVolumeSFX * 100)} %";

            // Apply the settings to the mixer
            ApplyAudioSettings(localVolumeAmbiant, localVolumeSFX);
        }
        else
        {
            // Reset to default audio settings if no prefs exist
            ResetAudioSettings();
        }
    }

    private void ApplyAudioSettings(float ambiantVolume, float sfxVolume)
    {
        if (mixer != null)
        {
            // Convert linear values to decibels and apply to mixer
            float ambiantDB = ambiantVolume > 0 ? Mathf.Log10(ambiantVolume) * 20 : -80f;
            float sfxDB = sfxVolume > 0 ? Mathf.Log10(sfxVolume) * 20 : -80f;

            mixer.SetFloat(VolumeSettings.MIXER_AMBIANT, ambiantDB);
            mixer.SetFloat(VolumeSettings.MIXER_SFX, sfxDB);

            Debug.Log($"Applied audio settings - Ambiant: {ambiantDB}dB, SFX: {sfxDB}dB");
        }
        else
        {
            Debug.LogWarning("AudioMixer reference is missing in LoadPrefs!");
        }
    }

    private void LoadSensitivitySettings()
    {
        if (PlayerPrefs.HasKey("YSensitivity"))
        {
            float localYSensitivity = PlayerPrefs.GetFloat("YSensitivity");
            float localXSensitivity = PlayerPrefs.GetFloat("XSensitivity");

            // Update UI
            ySensitivitySlider.value = localYSensitivity;
            ySensitivityValue.text = localYSensitivity.ToString("0");
            
            xSensitivitySlider.value = localXSensitivity;
            xSensitivityValue.text = localXSensitivity.ToString("0");

            // Apply to cinemachine
            if (_pov != null)
            {
                _pov.m_VerticalAxis.m_MaxSpeed = localYSensitivity; 
                _pov.m_HorizontalAxis.m_MaxSpeed = localXSensitivity;
            }
            else
            {
                Debug.LogWarning("CinemachinePOVExtension reference is missing in LoadPrefs!");
            }
        }
        else
        {
            // Reset to default sensitivity settings if no prefs exist
            ResetSensitivitySettings();
        }
    }

    private void ResetAudioSettings()
    {
        // Set default values
        float defaultVolume = 0.25f; // Same as VolumeSettings default
        
        ambiantSlider.SetValueWithoutNotify(defaultVolume);
        ambiantValue.text = $"{Mathf.RoundToInt(defaultVolume * 100)} %";
        
        sfxSlider.SetValueWithoutNotify(defaultVolume);
        sfxValue.text = $"{Mathf.RoundToInt(defaultVolume * 100)} %";

        // Apply the default settings to the mixer
        ApplyAudioSettings(defaultVolume, defaultVolume);

        // Save the default values
        PlayerPrefs.SetFloat(AudioManager.AMBIANT_KEY, defaultVolume);
        PlayerPrefs.SetFloat(AudioManager.SFX_KEY, defaultVolume);
    }

    private void ResetSensitivitySettings()
    {
        // Set default sensitivity values (adjust as needed)
        float defaultSensitivity = 1.0f;
        
        ySensitivitySlider.value = defaultSensitivity;
        ySensitivityValue.text = defaultSensitivity.ToString("0");
        
        xSensitivitySlider.value = defaultSensitivity;
        xSensitivityValue.text = defaultSensitivity.ToString("0");

        if (_pov != null)
        {
            _pov.m_VerticalAxis.m_MaxSpeed = defaultSensitivity; 
            _pov.m_HorizontalAxis.m_MaxSpeed = defaultSensitivity;
        }

        // Save the default values
        PlayerPrefs.SetFloat("YSensitivity", defaultSensitivity);
        PlayerPrefs.SetFloat("XSensitivity", defaultSensitivity);
    }
}