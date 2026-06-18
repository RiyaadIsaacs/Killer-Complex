#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click setup for the pause menu Settings button and settings overlay root.
/// </summary>
public static class PauseMenuSettingsSetup
{
    const string MainGameScenePath = "Assets/Scenes/Main Game.unity";

    /// <summary>Opens Main Game and saves pause settings UI into the scene (for batch/CI).</summary>
    public static void SetupMainGamePauseSettingsBatch()
    {
        EditorSceneManager.OpenScene(MainGameScenePath);
        SetupPauseSettingsMenu();
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Killer Complex/UI/Setup Pause Settings Menu")]
    public static void SetupPauseSettingsMenu()
    {
        var pauseScreen = Object.FindFirstObjectByType<PauseScreen>();
        if (pauseScreen == null)
        {
            EditorUtility.DisplayDialog("Pause Settings", "No PauseScreen found in the open scene.", "OK");
            return;
        }

        var pausePanel = GetPausePanel(pauseScreen);
        if (pausePanel == null)
        {
            EditorUtility.DisplayDialog("Pause Settings", "PauseScreen has no pause panel assigned.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(pausePanel, "Setup Pause Settings Menu");

        var styleSource = PauseMenuUiFactory.FindMenuButtonStyleSource(pausePanel.transform);
        var mainMenu = pausePanel.transform.Find("Main Menu");
        if (mainMenu != null)
        {
            var mainMenuRt = mainMenu.GetComponent<RectTransform>();
            if (mainMenuRt != null)
                mainMenuRt.anchoredPosition = new Vector2(mainMenuRt.anchoredPosition.x, -222f);
        }

        var settingsTransform = pausePanel.transform.Find("Settings");
        Button settingsButton;
        if (settingsTransform == null)
        {
            settingsButton = PauseMenuUiFactory.CreateTextButton(
                pausePanel.transform,
                "Settings",
                "",
                new Vector2(0f, -111f),
                new Vector2(250f, 80f),
                styleSource,
                pauseScreen.OpenSettings);
            Undo.RegisterCreatedObjectUndo(settingsButton.gameObject, "Create Settings Button");
        }
        else
        {
            settingsButton = settingsTransform.GetComponent<Button>();
            PauseMenuUiFactory.RewireButton(settingsButton, pauseScreen.OpenSettings);
        }

        var settingsMenu = pausePanel.GetComponentInChildren<GameSettingsMenu>(true);
        if (settingsMenu == null)
        {
            var settingsGo = new GameObject("GameSettingsMenu", typeof(RectTransform), typeof(GameSettingsMenu));
            Undo.RegisterCreatedObjectUndo(settingsGo, "Create GameSettingsMenu");
            settingsGo.transform.SetParent(pausePanel.transform, false);
            var rt = settingsGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            settingsMenu = settingsGo.GetComponent<GameSettingsMenu>();
        }

        settingsMenu.Initialize(pausePanel, styleSource, pauseScreen);

        var so = new SerializedObject(pauseScreen);
        so.FindProperty("settingsMenu").objectReferenceValue = settingsMenu;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(pauseScreen);
        EditorUtility.SetDirty(pausePanel);
        Debug.Log("Pause settings menu setup complete.");
    }

    static GameObject GetPausePanel(PauseScreen pauseScreen)
    {
        var so = new SerializedObject(pauseScreen);
        return so.FindProperty("pausePanel").objectReferenceValue as GameObject;
    }
}
#endif
