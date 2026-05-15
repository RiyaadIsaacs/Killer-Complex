using UnityEditor;
using UnityEngine;

public static class DoorRoomNumberTools
{
    [MenuItem("Tools/Killer-Complex/Fix all door room numbers in scene")]
    public static void FixAllDoorRoomNumbersInScene()
    {
        int count = 0;
        foreach (var door in Object.FindObjectsByType<InteractDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (door == null)
                continue;
            Undo.RegisterFullObjectHierarchyUndo(door.gameObject, "Fix door room numbers");
            door.EnsureRoomNumbersFollowPivot();
            count++;
        }

        Debug.Log($"Fixed room numbers on {count} {nameof(InteractDoor)} object(s). Save the scene.");
    }
}
