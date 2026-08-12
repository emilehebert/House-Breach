using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        // Make sure we always start on the main panel, options closed
        if (mainPanel) mainPanel.SetActive(true);
        if (optionsPanel) optionsPanel.SetActive(false);
    }

    // --- Button hooks ---

    public void OnPlayButtonPressed()
    {
        ScenesManager.Instance.LoadNewGame();
    }

    public void OnQuitButtonPressed()
    {
        GameManager.Instance.OnQuit();
    }

    public void OpenOptions()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        mainPanel.SetActive(true);

        if (settingsPanel != null)
        {
            foreach (Transform child in settingsPanel.transform)
                child.gameObject.SetActive(false);
        }
    }

    // --- Settings ---

    
}