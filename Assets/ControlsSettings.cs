using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Cinemachine;

public class ControlsSettings : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera vcam;
    [SerializeField] Slider ySensitivitySlider;
    [SerializeField] Slider xSensitivitySlider;
    [SerializeField] TMP_Text ySensitivityValue;
    [SerializeField] TMP_Text xSensitivityValue;

    private CinemachinePOV _pov;
    private const string Y_SENS_KEY = "YSensitivity";
    private const string X_SENS_KEY = "XSensitivity";

    void Awake()
    {
        FindVCam();
        ySensitivitySlider.onValueChanged.AddListener(SetYSensitivity);
        xSensitivitySlider.onValueChanged.AddListener(SetXSensitivity);
    }

    void Start()
    {
        ySensitivitySlider.value = PlayerPrefs.GetFloat(Y_SENS_KEY, 0.4f);
        xSensitivitySlider.value = PlayerPrefs.GetFloat(X_SENS_KEY, 0.4f);
        SetYSensitivity(ySensitivitySlider.value);
        SetXSensitivity(xSensitivitySlider.value);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        FindVCam();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        PlayerPrefs.Save();
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        vcam = null; // force a fresh lookup every scene load
        FindVCam();

        // Re-apply saved sensitivity immediately, not just when the slider is dragged
        if (_pov != null)
        {
            _pov.m_VerticalAxis.m_MaxSpeed = ySensitivitySlider.value;
            _pov.m_HorizontalAxis.m_MaxSpeed = xSensitivitySlider.value;
        }
    }

    void FindVCam()
    {
        if (vcam == null)
        {
            GameObject vcamObj = GameObject.FindWithTag("MainVCam");
            if (vcamObj != null)
                vcam = vcamObj.GetComponent<CinemachineVirtualCamera>();
        }

        _pov = vcam != null ? vcam.GetCinemachineComponent<CinemachinePOV>() : null;
    }

    void SetYSensitivity(float value)
    {
        int pct = Mathf.RoundToInt(value * 100);
        ySensitivityValue.text = $"{pct} %";
        FindVCam();
        if (_pov != null) _pov.m_VerticalAxis.m_MaxSpeed = value;
        PlayerPrefs.SetFloat(Y_SENS_KEY, value);
    }

    void SetXSensitivity(float value)
    {
        int pct = Mathf.RoundToInt(value * 100);
        xSensitivityValue.text = $"{pct} %";
        FindVCam();
        if (_pov != null) _pov.m_HorizontalAxis.m_MaxSpeed = value;
        PlayerPrefs.SetFloat(X_SENS_KEY, value);
    }
}