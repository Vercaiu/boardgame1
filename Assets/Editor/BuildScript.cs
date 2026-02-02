using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    // Scenes to include in the build
    private static string[] scenes = { "Assets/Scenes/Main.unity" };

    // Base folder outside Assets with version
    private static string GetBuildFolder()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string version = "v" + PlayerSettings.bundleVersion; // pull project version
        return Path.Combine(projectRoot, "Builds", version);
    }

    // =======================
    // Build All Platforms
    // =======================
    [MenuItem("Build/Build All Platforms")]
    public static void BuildAllPlatforms()
    {
        BuildWindows();
        BuildMac();
        BuildLinux();
    }

    // =======================
    // Build Windows Only
    // =======================
    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        BuildPlatform("Windows", "Game.exe", BuildTarget.StandaloneWindows64);
    }

    // =======================
    // Build Mac Only
    // =======================
    [MenuItem("Build/Build Mac")]
    public static void BuildMac()
    {
        BuildPlatform("Mac", "Game.app", BuildTarget.StandaloneOSX);
    }

    // =======================
    // Build Linux Only
    // =======================
    [MenuItem("Build/Build Linux")]
    public static void BuildLinux()
    {
        BuildPlatform("Linux", "Game.x86_64", BuildTarget.StandaloneLinux64);
    }

    // =======================
    // Generic Build Method
    // =======================
    private static void BuildPlatform(string platformName, string exeName, BuildTarget target)
    {
        string buildRoot = GetBuildFolder();
        string platformFolder = Path.Combine(buildRoot, platformName);

        if (!Directory.Exists(platformFolder))
        {
            Directory.CreateDirectory(platformFolder);
        }

        string buildPath = Path.Combine(platformFolder, exeName);

        BuildPipeline.BuildPlayer(scenes, buildPath, target, BuildOptions.None);

        Debug.Log($"{platformName} build complete: {Path.GetFullPath(buildPath)}");
    }
}
