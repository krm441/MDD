using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum DungeonType
{
    None,
    BSP, 
    CA, 
    GG
}

public class DungeonManager : MonoBehaviour
{
    [SerializeField] int seed;

    private DungeonType current = DungeonType.None;

    private IDungeon dungeon;

    private void Start()
    {
        
    }

    private void OnDestroy()
    {
        RemoveDungeon(GameSession.SelectedDungeon);
    }

    public void StartDungeon(DungeonType type)
    {
        RemoveDungeon(current);
        current = type;
        switch (type)
        {
            case DungeonType.None: break;
            case DungeonType.BSP:
                {
                    var BSP = GetComponentInChildren<BspController>();
                    BSP.Generate(seed);
                    dungeon = BSP;
                }
                break;
            case DungeonType.CA:
                {
                    var CA = GetComponentInChildren<CAGenerator>();
                    CA.Generate(seed);
                    dungeon = CA;
                }
                break;
            case DungeonType.GG:
                {
                    var GG = GetComponentInChildren<VoronoiGrammarController>();
                    GG.Generate(seed);
                    dungeon = GG;
                }
                break;
            default:
                break;
        }
    }

    public void RemoveDungeon(DungeonType type)
    {
        switch (type)
        {
            case DungeonType.None: break;
            case DungeonType.BSP:
                {
                    var BSP = GetComponentInChildren<BspController>();
                    BSP.Clean();
                }
                break;
            case DungeonType.CA:
                {
                    var CA = GetComponentInChildren<CAGenerator>();
                    CA.Clean();
                }
                break;
            case DungeonType.GG:
                {
                    var GG = GetComponentInChildren<VoronoiGrammarController>();
                    GG.Clean();
                }
                break;
            default:
                break;
        }
    }


    // ------------ Get party player start ---------- //
    public Room GetPlayerStart()
    {
        Assert.IsNotNull(dungeon);
        return dungeon.GetPlayerStart();
    }
}
