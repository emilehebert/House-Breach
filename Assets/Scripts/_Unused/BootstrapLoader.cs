using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "MainTitle";

    void Start()
    {
        SceneManager.LoadScene(firstSceneName, LoadSceneMode.Single);
    }
}