#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Clears local Unity build caches and enforces player settings required for URP standalone builds.
/// </summary>
public static class BuildCacheMaintenance
{
    const string SplashScreenCachePath = "Library/SplashScreenCache";
    const string WinPlayerArtifactsPath = "Library/Bee/artifacts/WinPlayerBuildProgram";
    const string UwpPlayerArtifactsPath = "Library/Bee/artifacts/UWPPlayerBuildProgram";

    [MenuItem("Killer Complex/Build/Clear Splash Screen Cache")]
    public static void ClearSplashScreenCacheMenu()
    {
        if (ClearSplashScreenCache())
            Debug.Log("Cleared Library/SplashScreenCache. Rebuild the Windows player.");
        else
            Debug.Log("SplashScreenCache folder was not present (nothing to clear).");
    }

    [MenuItem("Killer Complex/Build/Clear Windows Player Build Cache")]
    public static void ClearWindowsPlayerBuildCacheMenu()
    {
        if (ClearWindowsPlayerBuildCache())
            Debug.Log("Cleared Win/UWP player build caches. Rebuild to a NEW folder.");
        else
            Debug.Log("No Win/UWP player build cache folders were present.");
    }

    [MenuItem("Killer Complex/Build/Apply Standalone Build Settings (URP-safe)")]
    public static void ApplyStandaloneBuildSettingsMenu()
    {
        ClearWindowsPlayerBuildCache();
        if (ApplyStandaloneBuildSettings())
            Debug.Log("Standalone build settings applied (Strip Engine Code off, Managed Stripping Disabled). Rebuild to a new folder.");
        else
            Debug.Log("Standalone build settings were already correct. Rebuild to a new folder after clearing cache.");
    }

    public static bool ClearSplashScreenCache()
    {
        var fullPath = Path.GetFullPath(SplashScreenCachePath);
        if (!Directory.Exists(fullPath))
            return false;

        Directory.Delete(fullPath, recursive: true);
        return true;
    }

    public static bool ClearWindowsPlayerBuildCache()
    {
        var removed = false;
        foreach (var relativePath in new[] { WinPlayerArtifactsPath, UwpPlayerArtifactsPath })
        {
            var fullPath = Path.GetFullPath(relativePath);
            if (!Directory.Exists(fullPath))
                continue;

            Directory.Delete(fullPath, recursive: true);
            removed = true;
        }

        return removed;
    }

    public static bool ApplyStandaloneBuildSettings()
    {
        var changed = false;

        if (PlayerSettings.stripEngineCode)
        {
            PlayerSettings.stripEngineCode = false;
            changed = true;
        }

        if (PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Standalone) != ManagedStrippingLevel.Disabled)
        {
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.Disabled);
            changed = true;
        }

        if (changed)
            AssetDatabase.SaveAssets();

        return changed;
    }

    [MenuItem("Killer Complex/Build/Patch URP CoreModule in Windows Build...")]
    public static void PatchCoreModuleInExistingBuildMenu()
    {
        var exePath = EditorUtility.OpenFilePanel(
            "Select Windows player .exe",
            Path.GetDirectoryName(EditorUserBuildSettings.GetBuildLocation(BuildTarget.StandaloneWindows64)) ?? "",
            "exe");

        if (string.IsNullOrEmpty(exePath))
            return;

        if (PatchCoreModuleForUrp(exePath, EditorUserBuildSettings.development))
            EditorUtility.DisplayDialog(
                "URP CoreModule patched",
                "UnityEngine.CoreModule.dll was replaced with the correct player variation.\n\nRun the .exe again (keep the full folder).",
                "OK");
        else
            EditorUtility.DisplayDialog(
                "Patch failed",
                "Could not replace UnityEngine.CoreModule.dll. See the Console for details.",
                "OK");
    }

    /// <summary>
    /// Unity 6000 Win64 release builds can copy an outdated mono variation CoreModule missing URP startup APIs.
    /// </summary>
    public static bool PatchCoreModuleForUrp(string playerExecutablePath, bool developmentBuild)
    {
        if (string.IsNullOrEmpty(playerExecutablePath) || !File.Exists(playerExecutablePath))
        {
            Debug.LogError("URP CoreModule patch: player executable not found.");
            return false;
        }

        var exeDir = Path.GetDirectoryName(playerExecutablePath);
        var productName = Path.GetFileNameWithoutExtension(playerExecutablePath);
        var managedDir = Path.Combine(exeDir, productName + "_Data", "Managed");
        var dest = Path.Combine(managedDir, "UnityEngine.CoreModule.dll");

        if (!Directory.Exists(managedDir))
        {
            Debug.LogError($"URP CoreModule patch: Managed folder not found at {managedDir}");
            return false;
        }

        var source = ResolveCoreModuleSource(developmentBuild);
        if (source == null)
            return false;

        try
        {
            File.Copy(source, dest, overwrite: true);
        }
        catch (IOException ex)
        {
            Debug.LogError($"URP CoreModule patch: failed to copy {source} -> {dest}: {ex.Message}");
            return false;
        }

        if (!CoreModuleHasUrpStartupApi(dest))
        {
            Debug.LogError($"URP CoreModule patch: {dest} still missing IsCurrentRenderPipelineValid after copy.");
            return false;
        }

        Debug.Log($"URP CoreModule patch: replaced {dest} ({new FileInfo(dest).Length} bytes) from {source}");
        return true;
    }

    static string ResolveCoreModuleSource(bool developmentBuild)
    {
        var variation = developmentBuild ? "win64_player_development_mono" : "win64_player_nondevelopment_mono";
        var playerVariation = Path.Combine(
            EditorApplication.applicationContentsPath,
            "PlaybackEngines",
            "windowsstandalonesupport",
            "Variations",
            variation,
            "Data",
            "Managed",
            "UnityEngine.CoreModule.dll");

        if (File.Exists(playerVariation) && CoreModuleHasUrpStartupApi(playerVariation))
            return playerVariation;

        var editorManaged = Path.Combine(
            EditorApplication.applicationContentsPath,
            "Managed",
            "UnityEngine",
            "UnityEngine.CoreModule.dll");

        if (File.Exists(editorManaged) && CoreModuleHasUrpStartupApi(editorManaged))
            return editorManaged;

        Debug.LogError(
            "URP CoreModule patch: no source DLL with IsCurrentRenderPipelineValid found in the Unity install.");
        return null;
    }

    static bool CoreModuleHasUrpStartupApi(string dllPath)
    {
        try
        {
            var bytes = File.ReadAllBytes(dllPath);
            var text = System.Text.Encoding.ASCII.GetString(bytes);
            return text.Contains("IsCurrentRenderPipelineValid") &&
                   text.Contains("InitializeGlobalRenderPipelineTag");
        }
        catch (IOException ex)
        {
            Debug.LogError($"URP CoreModule patch: cannot read {dllPath}: {ex.Message}");
            return false;
        }
    }

    class StandaloneUrpBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows64 &&
                report.summary.platform != BuildTarget.StandaloneWindows)
                return;

            ClearWindowsPlayerBuildCache();
            ApplyStandaloneBuildSettings();
        }
    }

    class StandaloneUrpBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.result != BuildResult.Succeeded)
                return;

            if (report.summary.platform != BuildTarget.StandaloneWindows64 &&
                report.summary.platform != BuildTarget.StandaloneWindows)
                return;

            var outputPath = report.summary.outputPath;
            if (string.IsNullOrEmpty(outputPath))
                return;

            PatchCoreModuleForUrp(outputPath, report.summary.options.HasFlag(BuildOptions.Development));
        }
    }
}
#endif
