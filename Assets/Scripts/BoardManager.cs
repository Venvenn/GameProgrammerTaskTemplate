using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gpt4All;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public const int BOARD_SIZE_X = 8;
    public const int BOARD_SIZE_Y = 8;
    
    private const float BOUNCE_DURATION = 0.2f;
    private const float TILE_SIZE = 1.0f;
    private const float TILE_OFFSET = 0.5f;

    public bool isWhiteTurn = true;
    public Material selectedMat;
    public List<GameObject> chessmanPrefabs;

    [SerializeField]
    private BounceMechanicSettings _bounceMechanicSettings;
    [SerializeField]
    private LlmManager _llmManager;
    
    private int _selectionX = -1;
    private int _selectionY = -1;
    
    private Quaternion whiteOrientation = Quaternion.Euler(0, 270, 0);
    private Quaternion blackOrientation = Quaternion.Euler(0, 90, 0);
   
    private Material _previousMat;
    private List<Chessman> _activeChessman;
    private Chessman _selectedChessman;
    private BounceSystem _bounceSystem;
    
    public static BoardManager Instance { get; set; }
    
    public int[] EnPassantMove { get; set; }
    public Chessman[,] Chessmans { get; set; }
    
    private bool[,] _allowedMoves { get; set; }

    // Use this for initialization
    void Start()
    {
        _bounceSystem = new BounceSystem(_bounceMechanicSettings);
        
        Instance = this;
        SpawnAllChessmans();
        EnPassantMove = new int[2] { -1, -1 };
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSelection();

        if (Input.GetMouseButtonDown(0))
        {
            if (_selectionX >= 0 && _selectionY >= 0)
            {
                if (_selectedChessman == null)
                {
                    // Select the chessman
                    SelectChessman(_selectionX, _selectionY);
                }
                else
                {
                    // Move the chessman
                    MoveChessman(_selectionX, _selectionY);
                }
            }
        }

        if (Input.GetKey("escape"))
            Application.Quit();
    }

    private void SelectChessman(int x, int y)
    {
        if (Chessmans[x, y] == null) return;

        if (Chessmans[x, y].isWhite != isWhiteTurn) return;

        bool hasAtLeastOneMove = false;

        _allowedMoves = Chessmans[x, y].PossibleMoves();
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (_allowedMoves[i, j])
                {
                    hasAtLeastOneMove = true;
                    i = 8;
                    break;
                }
            }
        }

        if (!hasAtLeastOneMove)
            return;

        _selectedChessman = Chessmans[x, y];
        _previousMat = _selectedChessman.GetComponent<MeshRenderer>().material;
        selectedMat.mainTexture = _previousMat.mainTexture;
        _selectedChessman.GetComponent<MeshRenderer>().material = selectedMat;

        BoardHighlights.Instance.HighLightAllowedMoves(_allowedMoves);
    }

    private async Task<FightResult> CalculateVictory(Chessman attacker, Chessman defender)
    {
        FightResult fightResult = _bounceSystem.ResolveFight(attacker, defender);
        
        var freeSpaces = GetAdjacentFreeSpaces(defender.CurrentX, defender.CurrentY);
        bool spacesAvailable = freeSpaces.Count > 0;
        
        switch (fightResult)
        {
            case FightResult.Win:
            {
                if (spacesAvailable)
                {
                    attacker.ToggleSpinner(true);
                    var defenderTile = await _bounceSystem.BounceToTile(_llmManager, freeSpaces);
                    attacker.ToggleSpinner(false);
                    await BounceChessman(defender, (defender.CurrentX, defender.CurrentY), defenderTile);
                }
                else
                {
                    KillChessman(defender);  
                }

                if (!attacker.IsAlive())
                {
                    KillChessman(attacker);
                    _selectedChessman = null;
                }
                break;   
            }
            case FightResult.Lose:
            {
                if (spacesAvailable)
                {
                    attacker.ToggleSpinner(true);
                    var attackerTile = await _bounceSystem.BounceToTile(_llmManager, freeSpaces);
                    attacker.ToggleSpinner(false);
                    await BounceChessman(attacker, (defender.CurrentX, defender.CurrentY), attackerTile);
            
                }
                else
                {
                    KillChessman(attacker);
                    _selectedChessman = null;
                }

                if (!defender.IsAlive())
                {
                    KillChessman(defender);
                }
                break;
            }
            case FightResult.Draw:
            {
                if (spacesAvailable)
                {
                    if (defender.IsAlive())
                    {
                        attacker.ToggleSpinner(true);
                        var defenderTileDraw = await _bounceSystem.BounceToTile(_llmManager, freeSpaces);
                        Task bounce1 = BounceChessman(defender, (defender.CurrentX, defender.CurrentY), defenderTileDraw);
                        freeSpaces.Remove(defenderTileDraw);
                        var attackerTileDraw = await _bounceSystem.BounceToTile(_llmManager, freeSpaces);
                        attacker.ToggleSpinner(false);
                        Task bounce2 = BounceChessman(attacker, (defender.CurrentX, defender.CurrentY), attackerTileDraw);
                        await Task.WhenAll(bounce1, bounce2);
           
                    }
                    else
                    {
                        KillChessman(defender);
                    }
                }
                else
                {
                    KillChessman(attacker);
                    KillChessman(defender);
                    _selectedChessman = null;
                }

                break;
            }
        }

        attacker.ToggleSpinner(false);
        
        return fightResult;
    }

    private async Task BounceChessman(Chessman chessman, (int x, int y) startTile, (int x, int y) targetTile)
    {
        if (chessman.IsAlive())
        {
            Chessmans[chessman.CurrentX, chessman.CurrentY] = null;
            await LerpBouncePosition(chessman, GetTileCenter(startTile.x, startTile.y), GetTileCenter(targetTile.x, targetTile.y), BOUNCE_DURATION);
            chessman.SetPosition(targetTile.x, targetTile.y);
            Chessmans[targetTile.x, targetTile.y] = chessman;
        }
        else
        {
            KillChessman(chessman);
        }
    }

    private void KillChessman(Chessman chessman)
    {
        Chessmans[chessman.CurrentX, chessman.CurrentY] = null;
        _activeChessman.Remove(chessman);
        chessman.Kill();
    }

    private async void MoveChessman(int x, int y)
    {
        BoardHighlights.Instance.HideHighlights();
        
        if (_allowedMoves[x, y])
        {
            Chessman c = Chessmans[x, y];
            FightResult fightResult = FightResult.Win;

            if (c != null && c.isWhite != isWhiteTurn)
            {
                // Capture a piece
                if (c.GetType() == typeof(King))
                {
                    // End the game
                    EndGame();
                    return;
                }

                await LerpBouncePosition(_selectedChessman, GetTileCenter(_selectedChessman.CurrentX, _selectedChessman.CurrentY), GetTileCenter(c.CurrentX, c.CurrentY), BOUNCE_DURATION);
                fightResult = await CalculateVictory(_selectedChessman, c);
            }
            if (x == EnPassantMove[0] && y == EnPassantMove[1])
            {
                if (isWhiteTurn)
                    c = Chessmans[x, y - 1];
                else
                    c = Chessmans[x, y + 1];
                
                KillChessman(c);
            }
            EnPassantMove[0] = -1;
            EnPassantMove[1] = -1;
            if (_selectedChessman != null && _selectedChessman.GetType() == typeof(Pawn))
            {
                if (y == 7) // White Promotion
                {
                    KillChessman(_selectedChessman);
                    SpawnChessman(1, x, y, true);
                    _selectedChessman = Chessmans[x, y];
                }
                else if (y == 0) // Black Promotion
                {
                    KillChessman(_selectedChessman);
                    SpawnChessman(7, x, y, false);
                    _selectedChessman = Chessmans[x, y];
                }
                EnPassantMove[0] = x;
                if (_selectedChessman.CurrentY == 1 && y == 3)
                    EnPassantMove[1] = y - 1;
                else if (_selectedChessman.CurrentY == 6 && y == 4)
                    EnPassantMove[1] = y + 1;
            }

            if (_selectedChessman != null && fightResult == FightResult.Win)
            {
                Chessmans[_selectedChessman.CurrentX, _selectedChessman.CurrentY] = null;
                _selectedChessman.transform.position = GetTileCenter(x, y);
                _selectedChessman.SetPosition(x, y);
                Chessmans[x, y] = _selectedChessman;
            }

            isWhiteTurn = !isWhiteTurn;
        }

        if (_selectedChessman != null)
        {
            _selectedChessman.GetComponent<MeshRenderer>().material = _previousMat;
        }
        
        _selectedChessman = null;
    }

    private void UpdateSelection()
    {
        if (!Camera.main) return;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 50.0f, LayerMask.GetMask("ChessPlane")))
        {
            _selectionX = (int)hit.point.x;
            _selectionY = (int)hit.point.z;
        }
        else
        {
            _selectionX = -1;
            _selectionY = -1;
        }
    }

    private async void SpawnChessman(int index, int x, int y, bool isWhite)
    {
        Vector3 position = GetTileCenter(x, y);
        GameObject go;

        if (isWhite)
        {
            go = Instantiate(chessmanPrefabs[index], position, whiteOrientation) as GameObject;
        }
        else
        {
            go = Instantiate(chessmanPrefabs[index], position, blackOrientation) as GameObject;
        }

        ChessmanBounceData bounceData = await _bounceSystem.CreateBounceData(_llmManager);
        
        go.transform.SetParent(transform);
        Chessmans[x, y] = go.GetComponent<Chessman>();
        Chessmans[x, y].SetPosition(x, y);
        Chessmans[x,y].SetBounceData(bounceData);
        
        _activeChessman.Add(Chessmans[x, y]);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        Vector3 origin = Vector3.zero;
        origin.x += (TILE_SIZE * x) + TILE_OFFSET;
        origin.z += (TILE_SIZE * y) + TILE_OFFSET;

        return origin;
    }

    private List<(int x, int y)> GetAdjacentFreeSpaces(int centerX, int centerY)
    {
        List<(int x, int y)> validTiles = new List<(int x, int y)>();
        for (int y = centerY - 1; y <= centerY + 1; y++)
        {
            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                if (x >= 0 && x < BOARD_SIZE_X && y >= 0 && y < BOARD_SIZE_Y)
                {
                    if (Chessmans[x,y] == null)
                    {
                        validTiles.Add((x,y)); 
                    }
                }
            }
        }

        return validTiles;
    }

    private async Task LerpBouncePosition(Chessman chessman, Vector3 startPos,  Vector3 targetPos, float duration)
    {
        float time = 0;
        while (chessman.transform.position != targetPos)
        {
            time += Time.deltaTime;
            chessman.transform.position = Vector3.Lerp(startPos, targetPos, time / duration);
            await Task.Yield();
        }
    }

    private void SpawnAllChessmans()
    {
        _activeChessman = new List<Chessman>();
        Chessmans = new Chessman[8, 8];

        /////// White ///////

        // King
        SpawnChessman(0, 3, 0, true);

        // Queen
        SpawnChessman(1, 4, 0, true);

        // Rooks
        SpawnChessman(2, 0, 0, true);
        SpawnChessman(2, 7, 0, true);

        // Bishops
        SpawnChessman(3, 2, 0, true);
        SpawnChessman(3, 5, 0, true);

        // Knights
        SpawnChessman(4, 1, 0, true);
        SpawnChessman(4, 6, 0, true);

        // Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(5, i, 1, true);
        }
        
        /////// Black ///////

        // King
        SpawnChessman(6, 4, 7, false);

        // Queen
        SpawnChessman(7, 3, 7, false);

        // Rooks
        SpawnChessman(8, 0, 7, false);
        SpawnChessman(8, 7, 7, false);

        // Bishops
        SpawnChessman(9, 2, 7, false);
        SpawnChessman(9, 5, 7, false);

        // Knights
        SpawnChessman(10, 1, 7, false);
        SpawnChessman(10, 6, 7, false);

        // Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(11, i, 6, false);
        }
    }

    private void EndGame()
    {
        if (isWhiteTurn)
            Debug.Log("White wins");
        else
            Debug.Log("Black wins");

        foreach (Chessman chessman in _activeChessman)
        {
            chessman.Kill();
        }

        isWhiteTurn = true;
        BoardHighlights.Instance.HideHighlights();
        SpawnAllChessmans();
    }
}


