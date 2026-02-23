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
}
