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
        RebuildPauseSettingsPanel();
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Killer Complex/UI/Setup Pause Settings Menu")]
    public static void SetupPauseSettingsMenu()
    {
        if (!TryGetPauseContext(out var pauseScreen, out var pausePanel, out var styleSource))
            return;

        Undo.RegisterFullObjectHierarchyUndo(pausePanel, "Setup Pause Settings Menu");

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

        var settingsMenu = EnsureSettingsMenuComponent(pausePanel, pauseScreen, styleSource);
        RebuildSettingsPanelOnMenu(settingsMenu, pausePanel, styleSource, pauseScreen);

        EditorUtility.SetDirty(pauseScreen);
        EditorUtility.SetDirty(pausePanel);
        Debug.Log("Pause settings menu setup complete.");
    }

    [MenuItem("Killer Complex/UI/Rebuild Pause Settings Panel")]
    public static void RebuildPauseSettingsPanel()
    {
        if (!TryGetPauseContext(out var pauseScreen, out var pausePanel, out var styleSource))
            return;

        var settingsMenu = EnsureSettingsMenuComponent(pausePanel, pauseScreen, styleSource);
        Undo.RegisterFullObjectHierarchyUndo(settingsMenu.gameObject, "Rebuild Pause Settings Panel");
        RebuildSettingsPanelOnMenu(settingsMenu, pausePanel, styleSource, pauseScreen);

        EditorUtility.SetDirty(settingsMenu);
        EditorSceneManager.MarkSceneDirty(settingsMenu.gameObject.scene);
        Debug.Log("Pause settings panel rebuilt in scene. Expand GameSettingsMenu → SettingsPanel to edit layout.");
    }

    static bool TryGetPauseContext(out PauseScreen pauseScreen, out GameObject pausePanel, out Button styleSource)
    {
        pauseScreen = Object.FindFirstObjectByType<PauseScreen>();
        pausePanel = null;
        styleSource = null;

        if (pauseScreen == null)
        {
            EditorUtility.DisplayDialog("Pause Settings", "No PauseScreen found in the open scene.", "OK");
            return false;
        }

        pausePanel = GetPausePanel(pauseScreen);
        if (pausePanel == null)
        {
            EditorUtility.DisplayDialog("Pause Settings", "PauseScreen has no pause panel assigned.", "OK");
            return false;
        }

        styleSource = PauseMenuUiFactory.FindMenuButtonStyleSource(pausePanel.transform);
        return true;
    }

    static GameSettingsMenu EnsureSettingsMenuComponent(GameObject pausePanel, PauseScreen pauseScreen, Button styleSource)
    {
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

        var pauseSo = new SerializedObject(pauseScreen);
        pauseSo.FindProperty("settingsMenu").objectReferenceValue = settingsMenu;
        pauseSo.ApplyModifiedPropertiesWithoutUndo();

        return settingsMenu;
    }

    static void RebuildSettingsPanelOnMenu(
        GameSettingsMenu settingsMenu,
        GameObject pausePanel,
        Button styleSource,
        PauseScreen pauseScreen)
    {
        settingsMenu.Initialize(pausePanel, styleSource, pauseScreen);
        settingsMenu.EditorRebuildPanel();
        AssignBuiltReferences(settingsMenu);
    }

    static void AssignBuiltReferences(GameSettingsMenu settingsMenu)
    {
        var panelTransform = settingsMenu.transform.Find("SettingsPanel");
        if (panelTransform == null)
            return;

        var box = panelTransform.Find("SettingsBox");
        if (box == null)
            return;

        var menuSo = new SerializedObject(settingsMenu);
        menuSo.FindProperty("settingsPanel").objectReferenceValue = panelTransform.gameObject;
        menuSo.FindProperty("mouseSensitivitySlider").objectReferenceValue = box.Find("MouseSlider")?.GetComponent<Slider>();
        menuSo.FindProperty("mouseSensitivityValueLabel").objectReferenceValue = box.Find("MouseValue")?.GetComponent<TMPro.TMP_Text>();
        menuSo.FindProperty("sfxVolumeSlider").objectReferenceValue = box.Find("SfxSlider")?.GetComponent<Slider>();
        menuSo.FindProperty("sfxVolumeValueLabel").objectReferenceValue = box.Find("SfxValue")?.GetComponent<TMPro.TMP_Text>();
        menuSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject GetPausePanel(PauseScreen pauseScreen)
    {
        var so = new SerializedObject(pauseScreen);
        return so.FindProperty("pausePanel").objectReferenceValue as GameObject;
    }
}
#endif
