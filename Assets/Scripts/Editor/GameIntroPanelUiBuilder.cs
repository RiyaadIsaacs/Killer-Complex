using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

/// <summary>
/// Editor utilities to bake the intro panel UI into <see cref="GlobalNotificationHud"/> prefabs and scenes.
/// </summary>
public static class GameIntroPanelUiBuilder
{
    public static bool BuildOnHudRoot(GameObject hudRoot, bool replaceExisting)
    {
        if (hudRoot == null)
            return false;

        var intro = hudRoot.GetComponent<GameSceneIntroPanel>();
        if (intro == null)
            intro = hudRoot.AddComponent<GameSceneIntroPanel>();

        var existingPanel = hudRoot.transform.Find(GameIntroPanelUiLayout.PanelObjectName);
        if (existingPanel != null)
        {
            if (!replaceExisting)
            {
                if (GameIntroPanelUiLayout.TryFindExisting(hudRoot.transform, out var found))
                {
                    WireIntroComponent(intro, found);
                    return true;
                }
            }

            Object.DestroyImmediate(existingPanel.gameObject);
        }

        var soIntro = new SerializedObject(intro);
        var continueHint = soIntro.FindProperty("continueHint").stringValue;
        if (string.IsNullOrWhiteSpace(continueHint))
            continueHint = "Continue";

        if (!GameIntroPanelUiLayout.TryBuild(hudRoot.transform, continueHint, out var built))
        {
            Debug.LogError("Could not build game intro UI — import TMP Essential Resources first.");
            return false;
        }

        WireIntroComponent(intro, built);
        EditorUtility.SetDirty(hudRoot);
        return true;
    }

    static void WireIntroComponent(GameSceneIntroPanel intro, GameIntroPanelUiLayout.BuiltUi built)
    {
        if (built.PanelRoot == intro.gameObject)
        {
            Debug.LogError("Intro panel root cannot be the HUD canvas itself — build failed.", intro);
            return;
        }

        var so = new SerializedObject(intro);
        so.FindProperty("panelRoot").objectReferenceValue = built.PanelRoot;
        so.FindProperty("titleText").objectReferenceValue = built.TitleText;
        so.FindProperty("bodyText").objectReferenceValue = built.BodyText;
        so.FindProperty("continueButton").objectReferenceValue = built.ContinueButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        built.ContinueButton.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(built.ContinueButton.onClick, intro.DismissIntro);
        EditorUtility.SetDirty(built.ContinueButton);

        built.TitleText.text = so.FindProperty("title").stringValue;
        built.BodyText.text = so.FindProperty("body").stringValue;
        var label = built.ContinueButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (label != null)
            label.text = so.FindProperty("continueHint").stringValue;

        EditorUtility.SetDirty(intro);
    }
}
