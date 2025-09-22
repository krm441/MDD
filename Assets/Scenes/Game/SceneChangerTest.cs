using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity doesnt pass args across scenes, so this static class will hold the data instead (persistent)
/// </summary>
public static class GameSession
{
    public static DungeonType SelectedDungeon = DungeonType.None;
    public static PlayerPartyData playerParty;
}

public class SceneChangerTest : MonoBehaviour
{
    [ContextMenu("Change Scene")]
    public void ChangeScene()
    {
#if UNITY_EDITOR
        string path = "Assets/Scenes/Game/Game.unity";
        //EditorSceneManager.OpenScene(path);
        SceneManager.LoadScene("Game");
#else
        SceneManager.LoadScene("Game");
#endif
    }

    public static void LoadScene(string name)
    {
        SceneManager.LoadScene(name);
    }

    public void LoadGame_BSP() => LoadGame(DungeonType.BSP);
    public void LoadGame_CA() => LoadGame(DungeonType.CA);
    public void LoadGame_GG() => LoadGame(DungeonType.GG);

    /// <summary>
    /// For buttons
    /// </summary>
    /// <param name="type"></param>
    private void LoadGame(DungeonType type)
    {
        GameSession.SelectedDungeon = type; // set the persistent parameter. Will be used in Dungeon manager in the Game scene

        // In Play Mode use SceneManager, in Edit Mode open the asset
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            SceneManager.LoadScene("Game");
        }
        else
        {
            const string path = "Assets/Scenes/Game/Game.unity";
            //EditorSceneManager.OpenScene(path);
            SceneManager.LoadScene("Game");
        }
#else
        SceneManager.LoadScene("Game");
#endif
    }
}
