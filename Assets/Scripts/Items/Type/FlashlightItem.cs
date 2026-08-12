using UnityEngine;

[CreateAssetMenu(menuName = "Items/Flashlight")]
public class FlashlightItem : Item
{
    [Header("Flashlight SFX")]
    [SerializeField] private AudioEvent turnOnSound;
    [SerializeField] private AudioEvent turnOffSound;
    private void OnValidate()
    {
    }
    
    public override void Use(GameObject item)
    {
        // FIX: Use 'root' to start searching from the top Player object,
        // no matter which child part passed the 'item' reference.
        Transform lightTransform = item.transform.root.Find("CameraFPS/CamHolder/Camera/Light");
        
        if (lightTransform != null)
        {
            bool isTurningOn = !lightTransform.gameObject.activeSelf;
            lightTransform.gameObject.SetActive(isTurningOn);
            //flareQuad.gameObject.SetActive(isTurningOn);

            AudioSource sfxSource = AudioManager.instance.GetSfxSource();

            if (isTurningOn)
            {
                if (turnOnSound != null) turnOnSound.PlayRandom(sfxSource);
            }
            else
            {
                if (turnOffSound != null) turnOffSound.PlayRandom(sfxSource);
            }
        }
        else
        {
            // Debug helps you confirm if the path is wrong
            Debug.LogWarning($"Could not find Light! Searched for 'CameraFPS/Light' inside root: {item.transform.root.name}");
        }
    }
}