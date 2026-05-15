using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractDoor))]
public class InteractDoorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var door = (InteractDoor)target;
        EditorGUILayout.Space(8);

        if (GUILayout.Button("Fix room numbers (reparent + un-static)"))
        {
            Undo.RegisterFullObjectHierarchyUndo(door.gameObject, "Fix door room numbers");
            door.EnsureRoomNumbersFollowPivot();
            EditorUtility.SetDirty(door);
        }
    }
}
