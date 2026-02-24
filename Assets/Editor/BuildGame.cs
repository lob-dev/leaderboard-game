using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildGame
{
    public static void Build()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainScene.unity" },
            locationPathName = "Build/LeaderboardGame.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Build result: " + report.summary.result);
    }

    public static void BuildWebGL()
    {
        // Switch to WebGL
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        // Configure WebGL settings for mobile
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.template = "PROJECT:Mobile";

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainScene.unity" },
            locationPathName = "Build/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("WebGL Build result: " + report.summary.result);

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"WebGL Build failed with {report.summary.totalErrors} errors");
            EditorApplication.Exit(1);
        }
    }
}
