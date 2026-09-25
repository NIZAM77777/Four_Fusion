using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FusionProjectTools
{
    [MenuItem("Four Fusion/Configure Advanced Text")]
    public static void ConfigureText()
    {
        // Unity exposes this project setting through an internal editor settings type.
        var type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
        {
            try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
        }).FirstOrDefault(t => t.Name == "UIToolkitProjectSettings");
        if (type == null) throw new InvalidOperationException("UI Toolkit project settings type not found in this Unity version.");
        var property = type.GetProperty("enableAdvancedText", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null) throw new InvalidOperationException("Enable Advanced Text Generator in Project Settings > UI Toolkit.");
        property.SetValue(null, true);
    }

    [MenuItem("Four Fusion/Generate Service Prefabs")]
    public static void CreateServicePrefabs()
    {
        var panel = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>("Assets/Resources/Fusion/PanelSettings.asset");
        if (panel == null)
        {
            panel = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
            panel.themeStyleSheet = Resources.Load<UnityEngine.UIElements.ThemeStyleSheet>("Fusion/FusionTheme");
            AssetDatabase.CreateAsset(panel, "Assets/Resources/Fusion/PanelSettings.asset");
        }
        // Use a preview scene so unsaved work in the open scene is never changed.
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainMenu.unity");
        try
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<AudioManager>() != null)
                    PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Fusion/AudioManager.prefab");
                if (root.GetComponent<AdManager>() != null)
                    PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Fusion/AdsManager.prefab");
                if (root.GetComponent<PieceThemeManager>() != null)
                    PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Fusion/PieceThemes.prefab");
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [MenuItem("Four Fusion/Validate Android Release")]
    public static void ValidateRelease()
    {
        if (PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            throw new BuildFailedException("Android release must use IL2CPP.");
        if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            throw new BuildFailedException("Android release must include ARM64.");
        if (PlayerSettings.Android.targetSdkVersion != AndroidSdkVersions.AndroidApiLevelAuto && (int)PlayerSettings.Android.targetSdkVersion < 36)
            throw new BuildFailedException("Verify Play target SDK policy; this release requires API 36 or later.");
        if (!Uri.TryCreate(PlayerSettingsStore.PrivacyUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new BuildFailedException("Set the public HTTPS privacy policy URL.");
        foreach (var scene in new[] { "Splash", "MainMenu", "GameScene" })
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path.EndsWith("/" + scene + ".unity")))
                throw new BuildFailedException("Missing build scene: " + scene);
        foreach (var font in new[] { "NotoSans", "NotoSansArabic", "NotoSansDevanagari", "NotoSansMalayalam", "NotoSansTamil" })
            if (Resources.Load<Font>("Fusion/Fonts/" + font + "-Regular") == null) throw new BuildFailedException("Missing font: " + font);
        if (Resources.Load<UnityEngine.UIElements.PanelSettings>("Fusion/PanelSettings") == null)
            throw new BuildFailedException("Missing serialized UI panel. Use Four Fusion > Generate Service Prefabs.");
        Debug.Log("Four Fusion release configuration checks passed. Device, consent and signed bundle validation are still required.");
    }

    [MenuItem("Four Fusion/Build Android App Bundle")]
    public static void BuildAndroid()
    {
        ConfigureText(); ValidateRelease();
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), "Builds/FourFusion.aab", BuildTarget.Android, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException(report.summary.result.ToString());
    }
}

public sealed class FusionReleaseGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android || (report.summary.options & BuildOptions.Development) != 0) return;
        FusionProjectTools.ValidateRelease();
        if (!EditorUserBuildSettings.buildAppBundle) throw new BuildFailedException("Release delivery must be an Android App Bundle. Use Four Fusion > Build Android App Bundle.");
    }
}

// Keep the original editable scene artwork, but exclude superseded presentation
// objects and their texture dependencies from players. Gameplay objects are retained.
public sealed class FusionSceneBuildCleanup : IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || (scene.name != "MainMenu" && scene.name != "GameScene")) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            if (root == null) continue;
            var menu = root.GetComponent<MainMenuManager>();
            if (menu != null) UnityEngine.Object.DestroyImmediate(menu);
            if (root.name == "Bg") UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
