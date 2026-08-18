using UnityEngine;
using UnityEngine.UI;

public class OptionsTabController : MonoBehaviour
{
    [System.Serializable]
    public struct Tab
    {
        public GameObject panel;
        public Button button;
    }

    public Tab[] tabs;

    public Color normalColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color selectedColor = new Color(0.4f, 0.6f, 1f, 1f);

    void Start()
    {
        ShowTab(0);
    }

    public void ShowTab(int index)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].panel.SetActive(i == index);

            if (tabs[i].button != null)
            {
                ColorBlock colors = tabs[i].button.colors;
                colors.normalColor = (i == index) ? selectedColor : normalColor;
                tabs[i].button.colors = colors;
            }
        }
    }
}