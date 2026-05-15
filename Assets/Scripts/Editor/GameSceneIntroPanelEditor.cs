using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameSceneIntroPanel))]
public class GameSceneIntroPanelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Build Intro UI on Canvas (Title, Body, Continue)", GUILayout.Height(28f)))
        {
            var intro = (GameSceneIntroPanel)target;
            if (GameIntroPanelUiBuilder.BuildOnHudRoot(intro.gameObject, replaceExisting: true))
                Debug.Log($"Built {GameIntroPanelUiLayout.PanelObjectName} under {intro.name}.", intro);
        }

        var panel = introPanelRoot();
        if (panel == null)
        {
            EditorGUILayout.HelpBox(
                "Panel Root is not assigned. Click the button above to create GameIntroPanel on this canvas.",
                MessageType.Info);
        }
    }

    GameObject introPanelRoot()
    {
        var prop = serializedObject.FindProperty("panelRoot");
        return prop?.objectReferenceValue as GameObject;
    }
}
