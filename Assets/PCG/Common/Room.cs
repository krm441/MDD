using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CellLabel { none, table, chair, closet }

[System.Serializable]
public class Cell
{
    public CellLabel label = CellLabel.none;
    public Vector2Int size = new Vector2Int(1, 1);
    public Vector3 position;                        // world pos

    // Meta for placement logic
    public Vector2Int tile;     // grid tile
    public bool isWallEdge;     // touches a wall
    public bool isDoorway;      // on the edge, opening to a corridor
    public int distToWall;      // 0 at edge, grows inward
}

public enum RoomLabel { Unassigned, Start, Boss, A, B, C }

[System.Serializable]
public class Room
{
    public int id;                          
    public RoomLabel label = RoomLabel.Unassigned;

    public Vector3 worldPos;

    public List<Cell> cells;
}

// TODO - coupling ; tile scale should be injected, not defined
public static class BSPRoomCellBuilder
{
    //const float TileScale = 4f;

    // 4-neighbour offsets
    static readonly Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public static List<Cell> BuildCellsForRoom(DungeonLayout layout, RectInt roomRect, float TileScale = 4f)
    {
        // 1) Collect floor tiles that belong to this room carve
        var tiles = new List<Vector2Int>();
        for (int x = roomRect.xMin; x < roomRect.xMax; x++)
            for (int y = roomRect.yMin; y < roomRect.yMax; y++)
            {
                var p = new Vector2Int(x, y);
                if (layout.roomTiles.Contains(p)) tiles.Add(p);  // carved floor of this room
            }

        // 2) Classify edge tiles and doorways
        var inRoom = new HashSet<Vector2Int>(tiles);
        var wallEdge = new HashSet<Vector2Int>();
        var doorways = new HashSet<Vector2Int>();

        foreach (var p in tiles)
        {
            bool edge = false;
            foreach (var d in Dirs)
            {
                var q = p + d;
                // Edge
                if (!inRoom.Contains(q)) edge = true;

                // Doorway
                if (!inRoom.Contains(q) && layout.floorTiles.Contains(q))
                    doorways.Add(p);
            }
            if (edge) wallEdge.Add(p);
        }

        // 3) Distance to wall
        var dist = ComputeDistToWall(inRoom, wallEdge);

        // 4) Emit Cells
        var cells = new List<Cell>(tiles.Count);
        foreach (var t in tiles)
        {
            cells.Add(new Cell
            {
                label = CellLabel.none,
                size = new Vector2Int(1, 1),
                tile = t,
                position = new Vector3(t.x * TileScale, 0f, t.y * TileScale),
                isWallEdge = wallEdge.Contains(t),
                isDoorway = doorways.Contains(t),
                distToWall = dist.TryGetValue(t, out var d) ? d : 0
            });
        }

        return cells;
    }

    static Dictionary<Vector2Int, int> ComputeDistToWall(HashSet<Vector2Int> room, HashSet<Vector2Int> wallEdge)
    {
        var dist = new Dictionary<Vector2Int, int>(room.Count);
        var q = new Queue<Vector2Int>();

        // init
        foreach (var p in wallEdge)
        {
            dist[p] = 0;
            q.Enqueue(p);
        }

        // BFS
        while (q.Count > 0)
        {
            var p = q.Dequeue();
            int d = dist[p];

            foreach (var dir in Dirs)
            {
                var n = p + dir;
                if (!room.Contains(n)) continue;
                if (dist.ContainsKey(n)) continue;

                dist[n] = d + 1;
                q.Enqueue(n);
            }
        }

        //foreach (var p in room) if (!dist.ContainsKey(p)) dist[p] = 0;
        return dist;
    }
}