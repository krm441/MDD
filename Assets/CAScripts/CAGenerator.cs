using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CAGenerator : MonoBehaviour
{
    [Range(0,100)]
	public int randomFillPercent = 50;

	int[,] map;

    public int mapHeight = 64, mapWidth = 64;


    private int floor = 0;
    private int wall = 1;

    public int seed = 1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ContextMenu("Generate Dungeon")]
    public void GenerateDungeon()
    {
        // init seed
        Random.InitState(seed);

        // init map
        map = new int[mapHeight, mapWidth];

        MakeNoiseGrid(randomFillPercent);
        Debug.Log(randomFillPercent);
    }

    void MakeNoiseGrid(int density)
    {
        for(int i = 0; i < mapHeight; i++)
        {
            for(int j = 0; j < mapWidth; j++)
            {
                int random = Random.Range(1, 100);
                if(random > density)
                    map[i, j] = floor;
                else
                    map[i, j] = wall;
            }
        }
    }

    // DEBUG - visual debugging 
    public float tileSize = 1f; // size of each cell for visualization

    void OnDrawGizmos()
    {
        if (map == null) return;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0f, y * tileSize);

                if (map[y, x] == floor)
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.red;

                Gizmos.DrawCube(pos, Vector3.one * tileSize * 0.9f);
            }
        }
    }

}
