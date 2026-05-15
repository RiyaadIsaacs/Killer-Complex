using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Marks apartment room number meshes that must rotate with <see cref="InteractDoor"/>.
/// Modular letter prefabs ship with <b>Static</b> flags, which freezes world position even when parented to the door pivot.
/// </summary>
[DisallowMultipleComponent]
public class DoorRoomNumberLabel : MonoBehaviour
{
    [Tooltip("If set, digits are reparented under this transform on Awake (usually InteractDoor.doorPivot). Leave empty to auto-find an InteractDoor on parents.")]
    [SerializeField] private Transform followPivot;

    void Awake() => Apply();

    void OnValidate()
    {
        if (!Application.isPlaying)
            ClearStaticFlags(gameObject);
    }

    /// <summary>Reparents under the door pivot (world pose preserved) and clears static flags on this object and children.</summary>
    public void Apply()
    {
        var pivot = ResolveFollowPivot();
        if (pivot != null && transform.parent != pivot)
            transform.SetParent(pivot, true);

        ClearStaticFlags(gameObject);
    }

    Transform ResolveFollowPivot()
    {
        if (followPivot != null)
            return followPivot;

        var door = GetComponentInParent<InteractDoor>();
        if (door == null)
            return null;

        return door.DoorPivotTransform;
    }

    public static void ClearStaticFlags(GameObject root)
    {
        if (root == null)
            return;

        root.isStatic = false;

#if UNITY_EDITOR
        GameObjectUtility.SetStaticEditorFlags(root, 0);
#endif

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == null)
                continue;
            child.gameObject.isStatic = false;
#if UNITY_EDITOR
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, 0);
#endif
        }
    }

    public static bool NameLooksLikeRoomNumber(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        string n = objectName.ToLowerInvariant();
        return n.Contains("block number") || n.Contains("number ");
    }
}
