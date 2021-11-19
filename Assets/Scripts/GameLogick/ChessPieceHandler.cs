using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessPieceHandler : ChessParent
{

    [SerializeField] private GameObject[] prefabs_W;
    [SerializeField] private GameObject[] prefabs_B;

    //Spawning of the pieces
    public ChessPiece[,] SpawnAllPieces()
    {
        ChessPiece[,] chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0, blackTeam = 1;
        
        //White team
        chessPieces[0,0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1,0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2,0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3,0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4,0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5,0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6,0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7,0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for(int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i,1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);

        //Black team
        chessPieces[0,7] = SpawnSinglePiece(ChessPieceType.Rook,blackTeam);
        chessPieces[1,7] = SpawnSinglePiece(ChessPieceType.Knight,blackTeam);
        chessPieces[2,7] = SpawnSinglePiece(ChessPieceType.Bishop,blackTeam);
        chessPieces[3,7] = SpawnSinglePiece(ChessPieceType.Queen,blackTeam);
        chessPieces[4,7] = SpawnSinglePiece(ChessPieceType.King,blackTeam);
        chessPieces[5,7] = SpawnSinglePiece(ChessPieceType.Bishop,blackTeam);
        chessPieces[6,7] = SpawnSinglePiece(ChessPieceType.Knight,blackTeam);
        chessPieces[7,7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for(int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i,6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);


        return chessPieces;
    }

    public ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp;
        if(team == 0)
        {
            cp = Instantiate(prefabs_W[(int)type - 1], transform).GetComponent<ChessPiece>();
        }
        else
        {
            cp = Instantiate(prefabs_B[(int)type - 1], transform).GetComponent<ChessPiece>();
        }
        cp.type = type;
        cp.team = team;
        

        return cp;
    }

    //Positioning of the pieces
    public void PositionAllPieces(ChessPiece[,] chessPieces)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y] != null)
                {
                    PositionSinglePiece(chessPieces[x,y], x, y, true);
                }
            }
        }
    }

    public void PositionSinglePiece(ChessPiece cp, int x, int y, bool force = false)
    {
        cp.currentX = x;
        cp.currentY = y;
        cp.SetPosition(GetTileCenter(x,y),force);
    }
    
    
}
