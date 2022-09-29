using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Chessboard : MonoBehaviour
{

    [Header("Board Designs")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private GameObject victoryScreen;


    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // Start is called before the first frame update

    //Logic
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private GameObject[,] tiles;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();


    private Camera currentCamera;
    private Vector2Int currentHover;

    private Vector3 bounds;
    private bool isWhiteTurn;
    private void Awake()
    {
        isWhiteTurn = true;
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        

        SpawnAllPieces();

       
        PositionAllPieces();
    }
 
    private void Update(){

        if(!currentCamera){
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight"))){
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);
//if hovering after not hovering a tile
            if(currentHover == -Vector2Int.one){
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //if already hovering
            if(currentHover != hitPosition){
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            
            //if press/touch
            if (Input.GetMouseButtonDown(0)){
                    if(chessPieces[hitPosition.x, hitPosition.y] != null){
                        //is it our turn?
                        if((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn)){
                            currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                            //get a list of where i can go, highlight as well
                            availableMoves = currentlyDragging.GetAvailableMove(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                            HighlightTiles();
                        }
                    }
            }

            //if release press/touch
            if (currentlyDragging != null && Input.GetMouseButtonUp(0)){
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                
                
                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if(!validMove){
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                }
                
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        
        }

        else{

            if(currentHover != -Vector2Int.one){
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging && Input.GetMouseButton(0)){
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        //if we're dragging a piece
        if (currentlyDragging){
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if(horizontalPlane.Raycast(ray, out distance)){
                currentlyDragging.SetPosition(ray.GetPoint(distance));
            }

        }
    }


    
     //Generate the board
    void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {

        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;
        
        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++){
            for (int y = 0; y < tileCountY; y++){
                tiles[x,y] = GenerateSingleTile(tileSize,x, y);
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y){
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize);
        vertices[1] = new Vector3(x * tileSize, yOffset, (y+1) * tileSize);
        vertices[2] = new Vector3((x+1) * tileSize, yOffset, y * tileSize);
        vertices[3] = new Vector3((x+1) * tileSize, yOffset, (y+1) * tileSize);


        int[] tris = new int[] { 0, 1, 2, 1, 3, 2};

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");


        tileObject.AddComponent<BoxCollider>();
        return tileObject;
    }


    //spawning the pieces
    private void SpawnAllPieces(){
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0;
        int blackTeam = 1;

        //white team

        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);


        //black team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team){
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();

        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }



//Positioning
private void PositionAllPieces(){
for (int x = 0; x < TILE_COUNT_X; x++)
    for (int y = 0; y < TILE_COUNT_Y; y++)
        if (chessPieces[x, y] != null)
            PositionSinglePiece(x, y, true);
}

private void PositionSinglePiece(int x, int y, bool force = false){

chessPieces[x, y].currentX = x;
chessPieces[x, y].currentY = y;
chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
}


private Vector3 GetTileCenter(int x, int y){
    return new Vector3(x * tileSize, yOffset, y * tileSize) + new Vector3(tileSize / 2, 0, tileSize / 2);
}

 //Highlight tiles

private void HighlightTiles(){
    for(int i = 0; i < availableMoves.Count; i++)
        tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
}

private void RemoveHighlightTiles(){
    for(int i = 0; i < availableMoves.Count; i++)
       tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
    availableMoves.Clear();
}
    // operations
    private Vector2Int LookupTileIndex(GameObject hitInfo){
        for(int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x,y] == hitInfo)
                    return new Vector2Int(x,y);
                
            
        return -Vector2Int.one;
    }
private bool MoveTo(ChessPiece cp, int x, int y){

    if(!ContainsValidMove(ref availableMoves, new Vector2(x, y))){
        return false;
    }
    Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);


    //is there another piece on the target position?
    if (chessPieces[x, y] != null){
        ChessPiece ocp = chessPieces[x, y];
        if(cp.team == ocp.team){  
            return false;
        }

        

        //if its enemy team
        if(ocp.team == 0){

            if(ocp.type == ChessPieceType.King)
                CheckMate(1);
            deadWhites.Add(ocp);
            ocp.SetScale(Vector3.one *deathSize);
            ocp.SetPosition(new Vector3(8*tileSize, yOffset, -1 * tileSize) + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.forward * deathSpacing) * deadWhites.Count);


        }
        else{

            if(ocp.type == ChessPieceType.King)
                CheckMate(0);
            deadBlacks.Add(ocp);
            ocp.SetScale(Vector3.one *deathSize);
            ocp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.back * deathSpacing) * deadBlacks.Count);


        }
    }    
    chessPieces[x, y] = cp;
    chessPieces[previousPosition.x, previousPosition.y] = null;

    PositionSinglePiece(x, y);

    isWhiteTurn = !isWhiteTurn;

    return true;
}

// Checkmate 
private void CheckMate(int team){
    DisplayVictory(team);
}

private void DisplayVictory(int winningTeam){
    victoryScreen.SetActive(true);
    victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
}

public void OnResetButton(){

    SceneManager.LoadScene(0);

    //UI
    // victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
    // victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
    // victoryScreen.SetActive(false);


    // //fields reset
    // currentlyDragging = null;
    // availableMoves = new List<Vector2Int>();


    // //clean up
    // for(int x = 0; x < TILE_COUNT_X; x++){
    //     for(int y = 0; y < TILE_COUNT_Y; y++){
    //         if(chessPieces[x, y] != null)
    //             Destroy(chessPieces[x, y].gameObject);
            
    //         chessPieces[x, y] = null;
    //     }
    // }

    // for(int i = 0; i < deadWhites.Count; i++)
    //     Destroy(deadWhites[i].gameObject);
    // for(int i = 0; i < deadWhites.Count; i++)
    //     Destroy(deadBlacks[i].gameObject);

    // deadWhites.Clear();
    // deadBlacks.Clear();


    // SpawnAllPieces();
    // PositionAllPieces();
    // isWhiteTurn = true;
}

public void OnExitButton(){
    Application.Quit();
}
private bool ContainsValidMove( ref List<Vector2Int> moves, Vector2 pos){
    for(int i = 0; i < moves.Count; i++){
        if(moves[i].x == pos.x && moves[i].y == pos.y){
            return true;
        }
    
    }
    return false;
}

} 
