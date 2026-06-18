using UnityEngine.InputSystem;

namespace SojaExiles
{
    /// <summary>Input System replacement for legacy <c>Input.GetMouseButtonDown(0)</c> in BPS interact scripts.</summary>
    public static class BpsMouseInput
    {
        public static bool PrimaryClickThisFrame =>
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }
}
