using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class CreateMainScene
{
    [MenuItem("Tools/Create Main Scene")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Add SceneBuilder to scene
        var builderObj = new GameObject("SceneBuilder");
        builderObj.AddComponent<LeaderboardGame.SceneBuilder>();
        
        // Save scene
        string path = "Assets/Scenes/MainScene.unity";
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(
            System.IO.Path.Combine(Application.dataPath, "../", path)));
        EditorSceneManager.SaveScene(scene, path);
        
        // Add to build settings
        var scenes = new EditorBuildSettingsScene[] {
            new EditorBuildSettingsScene(path, true)
        };
        EditorBuildSettings.scenes = scenes;
        
        Debug.Log("MainScene created and saved!");
    }
}
