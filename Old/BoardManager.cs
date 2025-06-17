/*
=================================
This code may be freely distributed under the MIT License
=================================
*/

using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int boardRows = 64;
    public int boardColumns = 64;
    public int minRoomSize = 6;
    public int maxRoomSize = 20;

    [Header("Tile Prefabs")]
    public GameObject floorTile;
    public GameObject corridorTile;

	public GameObject wallTile;


    [Header("Tile Size (scale assumed uniform)")]
    public float tileSize = 4f;

    private GameObject[,] boardPositionsFloor;

    private GameObject dungeonParent;

    public class SubDungeon
    {
        public SubDungeon left, right;
        public Rect rect;
        public Rect room = new Rect(-1, -1, 0, 0);
        public List<Rect> corridors = new List<Rect>();
        public int debugId;
        private static int debugCounter = 0;

        public SubDungeon(Rect mrect)
        {
            rect = mrect;
            debugId = debugCounter++;
        }

        public bool IAmLeaf() => left == null && right == null;

        public bool Split(int minRoomSize, int maxRoomSize)
        {
            if (!IAmLeaf()) return false;

            bool splitH = rect.width / rect.height < 1.25f
                       ? rect.height / rect.width >= 1.25f || Random.value > 0.5f
                       : false;

            if (Mathf.Min(rect.height, rect.width) / 2 < minRoomSize)
                return false;

            if (splitH)
            {
                int split = Random.Range(minRoomSize, (int)(rect.height - minRoomSize));
                left = new SubDungeon(new Rect(rect.x, rect.y, rect.width, split));
                right = new SubDungeon(new Rect(rect.x, rect.y + split, rect.width, rect.height - split));
            }
            else
            {
                int split = Random.Range(minRoomSize, (int)(rect.width - minRoomSize));
                left = new SubDungeon(new Rect(rect.x, rect.y, split, rect.height));
                right = new SubDungeon(new Rect(rect.x + split, rect.y, rect.width - split, rect.height));
            }

            return true;
        }

        public void CreateRoom()
        {
            if (left != null) left.CreateRoom();
            if (right != null) right.CreateRoom();
            if (left != null && right != null) CreateCorridorBetween(left, right);

            if (IAmLeaf())
            {
                int roomWidth = Random.Range((int)(rect.width / 2), (int)(rect.width - 2));
                int roomHeight = Random.Range((int)(rect.height / 2), (int)(rect.height - 2));
                int roomX = Random.Range(1, (int)(rect.width - roomWidth - 1));
                int roomY = Random.Range(1, (int)(rect.height - roomHeight - 1));

                room = new Rect(rect.x + roomX, rect.y + roomY, roomWidth, roomHeight);
            }
        }

        public void CreateCorridorBetween(SubDungeon left, SubDungeon right)
        {
            Rect lroom = left.GetRoom();
            Rect rroom = right.GetRoom();

            Vector2 lpoint = new Vector2(Random.Range(lroom.x + 1, lroom.xMax - 1), Random.Range(lroom.y + 1, lroom.yMax - 1));
            Vector2 rpoint = new Vector2(Random.Range(rroom.x + 1, rroom.xMax - 1), Random.Range(rroom.y + 1, rroom.yMax - 1));

            if (lpoint.x > rpoint.x) (lpoint, rpoint) = (rpoint, lpoint);

            int w = (int)(rpoint.x - lpoint.x);
            int h = (int)(rpoint.y - lpoint.y);

            if (Random.value > 0.5f)
            {
                //corridors.Add(new Rect(lpoint.x, lpoint.y, w + 1, 1));
                corridors.Add(new Rect(lpoint.x, lpoint.y, w , 1));
                corridors.Add(new Rect(rpoint.x, Mathf.Min(lpoint.y, rpoint.y), 1, Mathf.Abs(h)));
            }
            else
            {
                corridors.Add(new Rect(lpoint.x, Mathf.Min(lpoint.y, rpoint.y), 1, Mathf.Abs(h)));
                //corridors.Add(new Rect(lpoint.x, rpoint.y, w + 1, 1));
                corridors.Add(new Rect(lpoint.x, rpoint.y, w , 1));
            }
        }

        public Rect GetRoom()
        {
            if (IAmLeaf()) return room;
            return left?.GetRoom() ?? right?.GetRoom() ?? new Rect(-1, -1, 0, 0);
        }
    }

	[ContextMenu("Generate Dungeon")]
	public void GenerateDungeon()
	{
		ClearPreviousDungeon();

		var root = new SubDungeon(new Rect(0, 0, boardRows, boardColumns));
		CreateBSP(root);
		root.CreateRoom();

		boardPositionsFloor = new GameObject[boardRows, boardColumns];
		dungeonParent = new GameObject("GeneratedDungeon");

		DrawRooms(root);
		DrawCorridors(root);
		PlaceWalls();
    }

    void CreateBSP(SubDungeon subDungeon)
    {
        if (!subDungeon.IAmLeaf())
            return;

        if (subDungeon.rect.width > maxRoomSize || subDungeon.rect.height > maxRoomSize || Random.value > 0.25f)
        {
            if (subDungeon.Split(minRoomSize, maxRoomSize))
            {
                CreateBSP(subDungeon.left);
                CreateBSP(subDungeon.right);
            }
        }
    }

    void DrawRooms(SubDungeon subDungeon)
    {
        if (subDungeon == null) return;

        if (subDungeon.IAmLeaf())
        {
            for (int i = (int)subDungeon.room.x; i < subDungeon.room.xMax; i++)
            {
                for (int j = (int)subDungeon.room.y; j < subDungeon.room.yMax; j++)
                {
                    Vector3 pos = new Vector3(i * tileSize, 0, j * tileSize);
                    GameObject tile = Instantiate(floorTile, pos, Quaternion.identity, dungeonParent.transform);
                    tile.name = $"Floor_{i}_{j}";
                    boardPositionsFloor[i, j] = tile;
                }
            }
        }
        else
        {
            DrawRooms(subDungeon.left);
            DrawRooms(subDungeon.right);
        }
    }

    void DrawCorridors(SubDungeon subDungeon)
    {
        if (subDungeon == null) return;

        DrawCorridors(subDungeon.left);
        DrawCorridors(subDungeon.right);

        foreach (var corridor in subDungeon.corridors)
        {
            for (int i = (int)corridor.x; i < corridor.xMax; i++)
            {
                for (int j = (int)corridor.y; j < corridor.yMax; j++)
                {
                    if (boardPositionsFloor[i, j] == null)
                    {
                        Vector3 pos = new Vector3(i * tileSize, 0, j * tileSize);
                        GameObject tile = Instantiate(corridorTile, pos, Quaternion.identity, dungeonParent.transform);
                        tile.name = $"Corridor_{i}_{j}";
                        boardPositionsFloor[i, j] = tile;
                    }
                }
            }
        }
    }

	void PlaceWalls()
	{
		for (int x = 0; x < boardRows; x++)
		{
			for (int y = 0; y < boardColumns; y++)
			{
				if (boardPositionsFloor[x, y] != null) // This is a floor tile
				{
					TryPlaceWall(x + 1, y); // East
					TryPlaceWall(x - 1, y); // West
					TryPlaceWall(x, y + 1); // North
					TryPlaceWall(x, y - 1); // South
				}
			}
		}
	}

	void TryPlaceWall(int x, int y)
	{
		if (x < 0 || y < 0 || x >= boardRows || y >= boardColumns)
			return;

		if (boardPositionsFloor[x, y] == null)
		{
			GameObject wall = Instantiate(wallTile, new Vector3(x, 0f, y), Quaternion.identity);
			wall.transform.SetParent(transform);
			boardPositionsFloor[x, y] = wall; // Optional: avoid placing more walls here again
		}
	}


    void ClearPreviousDungeon()
    {
        var existing = GameObject.Find("GeneratedDungeon");
        if (existing != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(existing);
#else
            Destroy(existing);
#endif
        }
    }
}

