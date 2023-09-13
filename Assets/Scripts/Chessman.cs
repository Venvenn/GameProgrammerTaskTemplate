using System;
using UnityEngine;

public abstract class Chessman : MonoBehaviour
{
    public int CurrentX { set; get; }
    public int CurrentY { set; get; }

    public bool isWhite;

    public ChessmanBounceData bounceData;
    
    private ChessmanUI m_ui;

    public void Awake()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        ChessmanUI prefab = Resources.Load<ChessmanUI>("Prefabs/UI/ChessmanUI");
        m_ui = Instantiate(prefab, canvas.transform);
    }

    public void SetBounceData(ChessmanBounceData newBounceData)
    {
        bounceData = newBounceData;
        m_ui.SetStrength(bounceData.strength);
    }

    public void Update()
    {
        m_ui.SetPositionInScreenSpace(transform.position);
        m_ui.SetHealth(bounceData.currentHealth, bounceData.maxHealth);
    }

    public void SetPosition(int x, int y)
    {
        CurrentX = x;
        CurrentY = y;
    }

    public void ToggleSpinner(bool enable)
    {
        m_ui.ToggleSpinner(enable);
    }

    public virtual bool[,] PossibleMoves()
    {
        return new bool[8, 8];
    }

    public bool Move(int x, int y, ref bool[,] r)
    {
        if (x >= 0 && x < BoardManager.BOARD_SIZE_X && y >= 0 && y < BoardManager.BOARD_SIZE_Y)
        {
            Chessman c = BoardManager.Instance.Chessmans[x, y];
            if (c == null)
                r[x, y] = true;
            else
            {
                if (isWhite != c.isWhite)
                    r[x, y] = true;
                return true;
            }
        }
        return false;
    }

    public bool IsAlive()
    {
        return bounceData.currentHealth > 0;
    }

    public void Kill()
    {
       Destroy(m_ui.gameObject); 
       Destroy(gameObject);
    }
    
}
