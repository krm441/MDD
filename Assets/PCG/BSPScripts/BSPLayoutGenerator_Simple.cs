using System;
using UnityEngine;

[Obsolete]
public class BSPLayoutGenerator_Simple : BSPLayoutGenerator
{
    [ContextMenu("Generate Layout Simple")]
    public void GenerateLayoutSimple() => Generate();

    protected override void ConnectStrategy(DungeonLayout layout)
    {
        ConnectRooms(root, layout);
    }

    void ConnectRooms(BSPNode node, DungeonLayout layout)
    {
        if (node == null || node.IsLeaf) return;

        ConnectRooms(node.left, layout);
        ConnectRooms(node.right, layout);

        if (node.left == null || node.right == null) return;

        var L = node.left.bounds;
        var R = node.right.bounds;

        if (L.xMax == R.xMin) // vertical split (left | right)
        {
            int y0 = Mathf.Clamp((L.yMin + L.yMax + R.yMin + R.yMax) / 4,
                                  Mathf.Max(L.yMin, R.yMin),
                                  Mathf.Min(L.yMax, R.yMax) - 1);
            Vector2Int leftDoorTarget = new Vector2Int(L.xMax - 1, y0);
            Vector2Int rightDoorTarget = new Vector2Int(R.xMin, y0);

            Vector2Int a = NearestRoomTile(leftDoorTarget, node.left, layout);
            Vector2Int b = NearestRoomTile(rightDoorTarget, node.right, layout);

            CarvePath(a, leftDoorTarget, layout);
            CarvePath(rightDoorTarget, b, layout);
            layout.floorTiles.Add(new Vector2Int(R.xMin, y0));
        }
        else if (L.yMax == R.yMin) // horizontal split (bottom | top)
        {
            int x0 = Mathf.Clamp((L.xMin + L.xMax + R.xMin + R.xMax) / 4,
                                  Mathf.Max(L.xMin, R.xMin),
                                  Mathf.Min(L.xMax, R.xMax) - 1);
            Vector2Int bottomDoorTarget = new Vector2Int(x0, L.yMax - 1);
            Vector2Int topDoorTarget = new Vector2Int(x0, R.yMin);

            Vector2Int a = NearestRoomTile(bottomDoorTarget, node.left, layout);
            Vector2Int b = NearestRoomTile(topDoorTarget, node.right, layout);

            CarvePath(a, bottomDoorTarget, layout);
            CarvePath(topDoorTarget, b, layout);
            layout.floorTiles.Add(new Vector2Int(x0, R.yMin));
        }
        else
        {
            //Vector2Int a = node.left.GetClosestRoomCenter();
            //Vector2Int b = node.right.GetClosestRoomCenter();
            //CreateCorridor(a, b, layout);
        }
    }
}
