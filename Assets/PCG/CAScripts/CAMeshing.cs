using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CAMeshing : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject fourPrefab;
    public GameObject threePrefab;
    public GameObject twoPrefab;
    public GameObject onePrefab;

    public Transform parent; // mesh holder - for easy removal

    public float scale = 4f; // size of the default floor prefab

    public void GenerateMeshes(int[,] map)
    {
        // clear on start
        ClearPrevious();

        for (int y = 0; y < map.GetLength(0) - 1; y++)
        {
            for (int x = 0; x < map.GetLength(1) - 1; x++)
            {
                int config = GetMarchingConfig(x, y, map);

                Vector3 pos = new Vector3(x * scale, 0f, y * scale);

                GameObject prefab = null;
                Quaternion rotation = Quaternion.identity;

                bool noFloorCase = true; // special case when we need no floor

                switch (config)
                {
                    case 15:
                        prefab = fourPrefab; // full
                        rotation = Quaternion.identity;
                        noFloorCase = false; // special case
                        break;

                    case 14: prefab = threePrefab; rotation = Quaternion.Euler(0, 90, 0); break; // correct
                    case 13: prefab = threePrefab; rotation = Quaternion.Euler(0, 0, 0); break; // correct
                    case 11: prefab = threePrefab; rotation = Quaternion.Euler(0, -90, 0); break; // correct
                    case 7: prefab = threePrefab; rotation = Quaternion.Euler(0, 180, 0); break; // correct

                    case 12: prefab = twoPrefab; rotation = Quaternion.Euler(0, 90, 0); break; // correct
                    case 6: prefab = twoPrefab; rotation = Quaternion.Euler(0, 180, 0); break; // correct
                    case 3: prefab = twoPrefab; rotation = Quaternion.Euler(0, -90, 0); break; // correct
                    case 9: prefab = twoPrefab; rotation = Quaternion.Euler(0, 0, 0); break; // correct

                    case 8: prefab = onePrefab; rotation = Quaternion.Euler(0, 90, 0); break;
                    case 4: prefab = onePrefab; rotation = Quaternion.Euler(0, 180, 0); break;
                    case 2: prefab = onePrefab; rotation = Quaternion.Euler(0, -90, 0); break;
                    case 1: prefab = onePrefab; rotation = Quaternion.Euler(0, 0, 0); break;

                    //case 0: prefab = floorPrefab; break;
                    default: break;
                        // case 0 = empty; do nothing
                        //prefab = floorPrefab;
                        //continue;
                }

                if (prefab != null)
                {
                    Instantiate(prefab, pos, rotation, parent);
                }

                if (noFloorCase) { Instantiate(floorPrefab, pos, rotation, parent); }
            }
        }
    }

    

    int GetMarchingConfig(int x, int y, int[,] map)
    {
        int val = 0;
        if (map[y, x] == 1) val |= 1;
        if (map[y, x + 1] == 1) val |= 2;
        if (map[y + 1, x + 1] == 1) val |= 4;
        if (map[y + 1, x] == 1) val |= 8;
        return val;
    }

    public void ClearPrevious()
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(parent.GetChild(i).gameObject);
        }

        //return;
        //
        //foreach (Transform child in parent)
        //{
        //    DestroyImmediate(child.gameObject);
        //}
    }

}
