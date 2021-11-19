using System;
using System.Collections.Generic;
using UnityEngine;

public class Chessboard : ChessParent
{

    [Header("Board settings")]
    [SerializeField] private Material tileMaterial;
    
    public GameObject[,] tiles;

    
    //Generate the board
    public void GenerateAllTiles() //Creates a board
    {
        yOffset += transform.position.y;
        
        tiles = new GameObject[TILE_COUNT_X, TILE_COUNT_Y];
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++) //iterates trough tiles 8 * 8
            {
                tiles[x,y] = GenerateSingleTile(x,y);
            }
        }
    }

    private GameObject GenerateSingleTile(int x, int y) //Creates a single tile 
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform; //makes a tile child of the board

        Mesh mesh = new Mesh(); //creats a mesh for the tile 
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        //A mesh consists of triangles arranged in 3D space to create the impression of a solid object
        Vector3[] vertices = new Vector3[4]; //creating corner vectors for mesh
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y+1) * tileSize) - bounds;
        vertices[2] = new Vector3((x+1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x+1) * tileSize, yOffset, (y+1) * tileSize) - bounds;

        int[] tris = new int[] {0, 1, 2, 1, 3, 2}; //in the Mesh class, the vertices are all stored in a single array

        mesh.vertices = vertices;
        mesh.triangles  = tris;
        mesh.RecalculateNormals();


        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

     //Highlight Tiles
    public void HighlightTiles(List<Vector2Int> availableMoves)
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }

    public void RemoveHighlightTiles(List<Vector2Int> availableMoves)
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }
}
