using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a node in a Binary Space Partitioning tree.
/// Used for splitting dungeon space and assigning rooms.
/// </summary>s
[System.Serializable]
public class BSPNode
{
    public RectInt bounds;
    public Vector2Int roomCenter;

    public BSPNode left, right;

    /// <summary> Returns true if this node has no children. </summary>
    public bool IsLeaf => left == null && right == null;

    public BSPNode(RectInt bounds)
    {
        this.bounds = bounds;
    }

    /// <summary>
    /// Shrinks the node bounds by a margin and returns it as a room rectangle.
    /// Also calculates the center of the room for corridor connection.
    /// </summary>
    public RectInt GetRoomBounds(int margin)
    {
        RectInt room = new RectInt(
            bounds.x + margin,
            bounds.y + margin,
            bounds.width - 2 * margin,
            bounds.height - 2 * margin
        );
        roomCenter = new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
        return room;
    }

    /// <summary>
    /// Returns the center of the closest room in this subtree.
    /// Used to find valid corridor connection points.
    /// </summary>
    public Vector2Int GetClosestRoomCenter()
    {
        if (IsLeaf) return roomCenter;

        // Prefer non-null children
        if (left != null) return left.GetClosestRoomCenter();
        if (right != null) return right.GetClosestRoomCenter();

        return Vector2Int.zero; // fallback
    }

}
