using UnityEngine;

public class DebugMenu : MonoBehaviour
{
    enum MenuState { Main, Scene, GameObjects }
    MenuState currentMenu = MenuState.Main;

    Vector2 scrollPos;
    GameObject selectedObject;
    float moveStep = 0.1f;

    bool menuVisible = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            menuVisible = !menuVisible;

            if (menuVisible)
                CursorManager.HideCursor(false); // force cursor visible while the debug menu is open
            else
                CursorManager.ClearHideCursorOverride(); // hand control back to the automatic pause/focus state

            CameraLookGate.SetLocked(menuVisible); // stop FPS look-around from fighting mouse clicks on the debug menu
        }
    }

    void OnGUI()
    {
        if (!menuVisible) return;

        GUILayout.BeginVertical("box");

        if (currentMenu == MenuState.Main)
        {
            GUILayout.Label("- DEBUG MENU -");

            if (GUILayout.Button("Player")) { }
            if (GUILayout.Button("Scene")) { currentMenu = MenuState.Scene; }
            if (GUILayout.Button("Floor Plan")) { }

            if (GUILayout.Button("Revert config")) { }
            if (GUILayout.Button("Save config changes")) { }
        }
        else if (currentMenu == MenuState.Scene)
        {
            GUILayout.Label("- SCENE -");

            if (GUILayout.Button("Add object")) { }
            if (GUILayout.Button("Edit objects")) { currentMenu = MenuState.GameObjects; }

            if (GUILayout.Button("Revert")) { }
            if (GUILayout.Button("Save to map file")) { }

            if (GUILayout.Button("Back")) { currentMenu = MenuState.Main; }
        }
        else if (currentMenu == MenuState.GameObjects)
        {
            GUILayout.Label("- GAME OBJECTS -");

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));

            foreach (GameObject obj in FindObjectsOfType<GameObject>())
            {
                if (GUILayout.Button(obj.name))
                {
                    selectedObject = obj;
                }
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("Back")) { currentMenu = MenuState.Scene; }
        }

        GUILayout.EndVertical();

        if (selectedObject != null)
        {
            GUILayout.Label("Selected: " + selectedObject.name);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Left")) { selectedObject.transform.position += Vector3.left * moveStep; }
            if (GUILayout.Button("Right")) { selectedObject.transform.position += Vector3.right * moveStep; }
            if (GUILayout.Button("Up")) { selectedObject.transform.position += Vector3.up * moveStep; }
            if (GUILayout.Button("Down")) { selectedObject.transform.position += Vector3.down * moveStep; }
            GUILayout.EndHorizontal();
        }
    }
}