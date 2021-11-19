using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessParent : MonoBehaviour
{
    public const int TILE_COUNT_X = 8;
    public const int TILE_COUNT_Y = 8;
    protected const float tileSize = 0.6f;
    public float yOffset = 0.025f;


    protected Vector3 bounds =  new Vector3((TILE_COUNT_X / 2) * tileSize, 0, (TILE_COUNT_Y / 2) * tileSize) + Vector3.zero;


    public Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0 , tileSize / 2);
    }
}
