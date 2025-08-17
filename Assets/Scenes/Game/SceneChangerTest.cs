using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangerTest : MonoBehaviour
{
    [ContextMenu("Change Scene")]
    public void ChangeScene()
    {
#if UNITY_EDITOR
        string path = "Assets/Scenes/Game/Game.unity";
        EditorSceneManager.OpenScene(path);
#else
        SceneManager.LoadScene("Game");
#endif
    }
}
