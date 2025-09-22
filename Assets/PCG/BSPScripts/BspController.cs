using System.Collections.Generic;
using UnityEngine;

public class BspController : MonoBehaviour, IDungeon
{
    [Header("Meta")]
    [SerializeField] private int seed = 0;
    [SerializeField] private int rooms = 0;

    [Header("Generator & Mesher")]
    public BSPLayoutGenerator generator;
    public BSPMeshing mesher;    
    public RoomDecorator roomDecorator;

    public void Generate(int seed)
    {
        this.seed = seed;
        Generate();
    }

    [ContextMenu("Generate + Build")]
    public void Generate()
    {
        Debug.Log("GenerateAndBuild");
        if (generator == null || mesher == null)
        {
            return;
        }

        generator.seed = seed;
        generator.Generate();            
        mesher.generator = generator;
        mesher.trimToRoomCount = rooms;
        mesher.Rebuild();

        // decorate
        roomDecorator?.Gen();
    }

    [ContextMenu("Clean All")]
    public void Clean()
    {
        if (mesher == null || roomDecorator == null)
        {
            return;
        }

        mesher.Clear();

        roomDecorator.Clear();
    }

    public Room GetPlayerStart() => generator.startRoom;
    public Room GetBossLocation() => generator.bossRoom;

    public List<Room> GetRoomsA() => null;
}
