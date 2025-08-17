using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Used to rapidly switch between currently working on scenes
/// </summary>
public class SceneTabsWindow : EditorWindow
{
    private string[] scenePaths = new string[]
    {
        "Assets/Scenes/DebugScenes/DebBSPScene.unity",
        "Assets/Scenes/DebugScenes/DebCAScene.unity"
    };

    [MenuItem("Window/Scene Tabs")]
    public static void ShowWindow()
    {
        GetWindow<SceneTabsWindow>("Scene Tabs");
    }

    private void OnGUI()
    {
        GUILayout.Label("Quick Scene Switch", EditorStyles.boldLabel);

        foreach (string scenePath in scenePaths)
        {
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (GUILayout.Button(sceneName, GUILayout.Height(30)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                }
            }
        }
    }
}
