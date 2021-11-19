using System;
using System.Collections.Generic;
using UnityEngine;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}


public class GameHandler : MonoBehaviour
{
    
    [SerializeField] private float dragOffset = 1.0f;
    [SerializeField] private ChessPieceHandler cpHandler;
    [SerializeField] private Chessboard chessboard;
    [SerializeField] private Camera whiteCamera;
    [SerializeField] private Camera blackCamera;
    [SerializeField] private GameObject victoryScreen;

    //  LOGIC   
    private ChessPiece[,] chessPieces;
    
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();

    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private SpecialMove specialMove;
    private Camera currentCamera;
    
    private Vector2Int currentHover;
    private bool isWhiteTurn;
    
    private void Awake()
    {
        whiteCamera.GetComponent<AudioListener> ().enabled  =  false;
        blackCamera.GetComponent<AudioListener> ().enabled  =  false;
        chessboard.GenerateAllTiles();
        StartGame();
    }

    public void StartGame()
    {
        isWhiteTurn = true;
        whiteCamera.enabled = true;
        blackCamera.enabled = false;
        currentCamera = whiteCamera;
        
        chessPieces = cpHandler.SpawnAllPieces();
        cpHandler.PositionAllPieces(chessPieces);
    }

    private void Update()
    {
        
        //creating Raycast for mouse over tiles 
        RaycastHit info; 
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray,out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            //Get the indexes of the tile hit with the ray 
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            //if hovering a tile after not hovering any tile
            if(currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                chessboard.tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //if already hovering a tile, change the previous one
            if(currentHover != hitPosition)
            {
                chessboard.tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                chessboard.tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //if press down on mouse
            if(Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    //is it our turn?
                    if((chessPieces[hitPosition.x, hitPosition.y]. team == 0 && isWhiteTurn) ||
                        (chessPieces[hitPosition.x, hitPosition.y]. team == 1 && !isWhiteTurn))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                        //Get a list of where I can go
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, ChessParent.TILE_COUNT_X, ChessParent.TILE_COUNT_Y);
                        //Get a list of special moves 
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
                        //highlight tiles as well
                        PreventCheck();
                        chessboard.HighlightTiles(availableMoves);
                    }
                }
            }

            //if release up on mouse 
            if(currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                
                if(!validMove)
                {
                    currentlyDragging.SetPosition(chessboard.GetTileCenter(previousPosition.x, previousPosition.y));
                    currentlyDragging = null;
                }
                currentlyDragging = null;
                chessboard.RemoveHighlightTiles(availableMoves);
            }
        }
        else
        {
            if(currentHover != -Vector2Int.one)
            {
                chessboard.tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if(currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(chessboard.GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                chessboard.RemoveHighlightTiles(availableMoves);
            }
        }

        if(currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * chessboard.yOffset);
            float distance = 0.0f;
            if(horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
    }

    private void PreventCheck()
    {
        ChessPiece targetKing = null;
        for(int x = 0; x < ChessParent.TILE_COUNT_X; x++)
        {
            for(int y = 0; y < ChessParent.TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y] != null)
                {
                    if(chessPieces[x,y].type == ChessPieceType.King)
                    {
                        if(chessPieces[x,y].team == currentlyDragging.team)
                        {
                            targetKing = chessPieces[x,y];
                        }
                    }
                }
            }
        }
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }

    private void SimulateMoveForSinglePiece(ChessPiece cp,ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        //Save current values, to reset after the function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        //Going trough all the moves, simulate them and chcek if we're in check
        for(int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            //Did we simulate the king's move
            if(cp.type == ChessPieceType.King)
            {
                kingPositionThisSim = new Vector2Int(simX, simY);
            }
            
            ChessPiece[,] simulation = new ChessPiece[ChessParent.TILE_COUNT_X, ChessParent.TILE_COUNT_Y];
            List<ChessPiece> simAttackingPieces = new List<ChessPiece>();
            
            for(int x = 0; x < ChessParent.TILE_COUNT_X; x++)
            {
                for(int y = 0; y < ChessParent.TILE_COUNT_Y; y++)
                {
                    if(chessPieces[x,y] != null)
                    {
                        simulation[x,y] = chessPieces[x,y];

                        if(simulation[x,y].team != cp.team)
                        {
                            simAttackingPieces.Add(simulation[x,y]);
                        }
                    }
                }
            }

            //Simulate that move 
            simulation[actualX,actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;

            //Did one of the piece got taken down during our simulation
            var deadPiece = simAttackingPieces.Find(c => c.currentX == simX && c.currentY == simY);
            if(deadPiece != null)
            {
                simAttackingPieces.Remove(deadPiece);
            }

            //Get all the simulated attacking pieces move
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for(int a = 0; a < simAttackingPieces.Count; a++)
            {
                var pieceMoves = simAttackingPieces[a].GetAvailableMoves(ref simulation, ChessParent.TILE_COUNT_X, ChessParent.TILE_COUNT_Y);
                for(int b = 0; b < pieceMoves.Count; b++)
                {
                    simMoves.Add(pieceMoves[b]);
                }
            }

            if(ContainsValidMove(ref simMoves, kingPositionThisSim))
            {
                movesToRemove.Add(moves[i]);
            }

            //Restore the actual cp data
            cp.currentX = actualX;
            cp.currentY = actualY;
        }

        //remove from the current available move list
        for(int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }

    
    private void changeTeam()
    {
        isWhiteTurn = !isWhiteTurn;
        if(isWhiteTurn)
        {
            whiteCamera.enabled = true;
            blackCamera.enabled = false;
            currentCamera = whiteCamera;
        }
        else
        {
            whiteCamera.enabled = false;
            blackCamera.enabled = true;
            currentCamera = blackCamera;
        }
    }
    private void CleanUp()
    {
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();
        
        for(int x = 0; x < ChessParent.TILE_COUNT_X; x++)
        {
            for(int y = 0; y < ChessParent.TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y] != null)
                    Destroy(chessPieces[x,y].gameObject);

                chessPieces[x,y] = null;
            }
        }
    }
   
    
    //End of game
    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }

    public void OnResetButton()
    {
        //UI
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        CleanUp();
        StartGame();

    }   

    public void OnExitButton()
    {
        Application.Quit();
    }
    //Special move
    private void ProcessSpecialMove()
    {
        if(specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if(myPawn.currentX == enemyPawn.currentX)
            {
                if(myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    Destroy(enemyPawn.gameObject);
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }

        if(specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            //Left rook
            if(lastMove[1].x == 2)
            {
                if(lastMove[1].y == 0) //White side
                {
                    ChessPiece rook = chessPieces[0,0];
                    chessPieces[3,0] = rook;
                    cpHandler.PositionSinglePiece(rook,3,0);
                    chessPieces[0,0] = null;
                }
                else if(lastMove[1].y == 7) //Black side
                {
                    ChessPiece rook = chessPieces[0,7];
                    chessPieces[3,7] = rook;
                    cpHandler.PositionSinglePiece(rook,3,7);
                    chessPieces[0,7] = null;
                }
            } 
            else if(lastMove[1].x == 6)
            {
                if(lastMove[1].y == 0) //White side
                {
                    ChessPiece rook = chessPieces[7,0];
                    chessPieces[5,0] = rook;
                    cpHandler.PositionSinglePiece(rook,5,0);
                    chessPieces[7,0] = null;
                }
                else if(lastMove[1].y == 7) //Black side
                {
                    ChessPiece rook = chessPieces[7,7];
                    chessPieces[5,7] = rook;
                    cpHandler.PositionSinglePiece(rook,5,7);
                    chessPieces[7,7] = null;
                }
            }
        }

        if(specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if(targetPawn.type == ChessPieceType.Pawn)
            {
                if(targetPawn.team == 0 && lastMove[1].y == 7)
                {
                    ChessPiece newQueen = cpHandler.SpawnSinglePiece(ChessPieceType.Queen, 0);
                    int tmpX = lastMove[1].x;
                    int tmpY = lastMove[1].y;
                    Destroy(chessPieces[tmpX, tmpY].gameObject);
                    chessPieces[tmpX, tmpY] = newQueen;
                    cpHandler.PositionSinglePiece(chessPieces[tmpX, tmpY], tmpX, tmpY, true);
                }

                if(targetPawn.team == 1 && lastMove[1].y == 0)
                {
                    ChessPiece newQueen = cpHandler.SpawnSinglePiece(ChessPieceType.Queen, 1);
                    int tmpX = lastMove[1].x;
                    int tmpY = lastMove[1].y;
                    Destroy(chessPieces[tmpX, tmpY].gameObject);
                    chessPieces[tmpX, tmpY] = newQueen;
                    cpHandler.PositionSinglePiece(chessPieces[tmpX, tmpY], tmpX, tmpY, true);
                }
            }
        }
    }
    
    private bool CheckForCheckmate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        for(int x = 0; x < ChessParent.TILE_COUNT_X; x++)
        {
            for(int y = 0; y < ChessParent.TILE_COUNT_Y; y++)
            {
                if(chessPieces[x,y] != null)
                {
                    if(chessPieces[x,y].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x,y]);
                        if(chessPieces[x,y].type == ChessPieceType.King)
                        {
                            targetKing = chessPieces[x,y];
                        }
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x,y]);
                    }
                }
            }
        }

        //is the King attacked right now?
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for(int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, ChessParent.TILE_COUNT_X, ChessParent.TILE_COUNT_Y);
            for(int b = 0; b < pieceMoves.Count; b++)
            {
                currentAvailableMoves.Add(pieceMoves[b]);
            }
        }

        //Are we in check right now?
        if(ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            for(int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, ChessParent.TILE_COUNT_X, ChessParent.TILE_COUNT_Y);
                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                if(defendingMoves.Count != 0)
                {
                    return false;
                }
            }    
            return true; //King is in check mate
        }
        return false;
    }
    //Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if(moves[i].x == pos.x && moves[i].y == pos.y)
                return true;
        }
        return false;
    }
    
    private Vector2Int LookupTileIndex(GameObject hitInfo) //Returns the tile index
    {
        for (int x = 0; x < ChessParent.TILE_COUNT_X; x++)
        {
            for (int y = 0; y < ChessParent.TILE_COUNT_Y; y++)
            {
                if(chessboard.tiles[x,y] == hitInfo)
                return new Vector2Int(x,y);
            }
        }
        return -Vector2Int.one; //Invalid
    }
    
     private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if(!ContainsValidMove(ref availableMoves, new Vector2Int(x,y)))
            return false;
            
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        //is there another piece on the target position?
        if(chessPieces[x,y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if(cp.team == ocp.team)
            {
                return false;
            }
            else
            {
                Destroy(ocp.gameObject);
            }
        }

        chessPieces[x,y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        cpHandler.PositionSinglePiece(chessPieces[x,y],x, y);

        changeTeam();
        moveList.Add(new Vector2Int[]{previousPosition, new Vector2Int(x,y)});

        ProcessSpecialMove();

        if(CheckForCheckmate())
        {
            DisplayVictory(cp.team);
        }

        return true;
    }

}
