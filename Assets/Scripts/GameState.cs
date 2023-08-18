using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class GameState
{
    public const int Rows = 8;
    public const int Columns = 8;

    //This is a 2D Array.
    public Player[,] Board { get; }

    //Dictionary to keep player score count.
    public Dictionary<Player, int> DiscCount { get; }

    //Which player's turn is it?
    public Player CurrentPlayer { get; private set; }
    public bool GameOver { get; private set; }
    public Player Winner { get; private set; }

    //Store Which moves the current player can make in this state.
    public Dictionary<Position, List<Position>> LegalMoves { get; private set; }

    public GameState()
    {
        Board = new Player[Rows, Columns];

        //This creates/initializes the initial four pieces in the middle of board. 
        Board[3, 3] = Player.White;
        Board[3, 4] = Player.Black;
        Board[4, 3] = Player.Black;
        Board[4, 4] = Player.White;

        //Initializes the DiscCount.
        DiscCount = new Dictionary<Player, int>()
        { 
            { Player.Black, 2 },
            { Player.White, 2 }
        };

        CurrentPlayer = Player.Black;
        LegalMoves = FindLegalMoves(CurrentPlayer);
    }

    public bool MakeMove(Position pos, out MoveInfo moveInfo)
    {
        if(!LegalMoves.ContainsKey(pos))
        {
            moveInfo = null;
            Debug.Log("Not Legal");
            return false;
        }

        Player movePlayer = CurrentPlayer;
        List<Position> outFlanked = LegalMoves[pos];

        Board[pos.Row, pos.Col] = movePlayer;
        FlipDiscs(outFlanked);
        UpdateDiscCounts(movePlayer, outFlanked.Count);
        PassTurn();

        moveInfo = new MoveInfo { Player = movePlayer, Position = pos, OutFlanked = outFlanked };
        return true;
    }

    public IEnumerable<Position> OccupiedPositions()
    {
        for (int r = 0; r < Rows; r++)
        {
            for(int c = 0; c < Columns; c++)
            {
                if (Board[r, c] != Player.None)
                {
                    yield return new Position(r, c);
                }
            }
        }
    }
    
    private void FlipDiscs(List<Position> positions)
    {
        foreach(Position pos in positions)
        {
            Board[pos.Row, pos.Col] = Board[pos.Row, pos.Col].Opponent();
        }
    }

    private void UpdateDiscCounts(Player movePlayer, int outFlankedCount)
    {
        DiscCount[movePlayer] += outFlankedCount + 1;
        DiscCount[movePlayer.Opponent()] -= outFlankedCount;
    }

    private void ChangePlayer()
    {
        CurrentPlayer = CurrentPlayer.Opponent();
        LegalMoves = FindLegalMoves(CurrentPlayer);
    }

    private Player FindWinner()
    {
        if (DiscCount[Player.Black] > DiscCount[Player.White])
        {
            return Player.Black;
        }
        if (DiscCount[Player.White] > DiscCount[Player.Black])
        {
            return Player.White;
        }

        //Game ends at a Tie.
        return Player.None;
    }

    private void PassTurn()
    {
        ChangePlayer();

        if(LegalMoves.Count > 0)
        {
            return;
        }

        //The first player has no legal moves so the other player gets to move again.
        ChangePlayer();

        if(LegalMoves.Count == 0)
        {
            CurrentPlayer = Player.None;
            GameOver = true;
            Winner = FindWinner();
        }
    }

    private bool IsInsideBoard(int r, int c)
    {
        return r >= 0 && r < Rows && c >= 0 && c < Columns;
    }

    /*
     DIRECTION rDelta cDelta
     North       -1      0
     South        1      0
     West         0     -1
     East         0      1
     North-West  -1     -1
     North-East  -1      1
     South-West   1     -1
     South-East   1      1
     */

    private List<Position> OutFlankedInDir(Position pos, Player player, int rDelta, int cDelta)
    {
        List<Position> outflanked = new List<Position>();
        int r = pos.Row + rDelta;
        int c = pos.Col + cDelta;

        while (IsInsideBoard(r, c) && Board[r, c] != Player.None)
        {
            if (Board[r, c] == player.Opponent())
            {
                outflanked.Add(new Position(r, c));
                r += rDelta;
                c += cDelta;
            }
            else /*if (Board[r, c] == player)*/
            {
                return outflanked;
            }
        }

        return new List<Position>();
    }

    private List<Position> OutFlanked(Position pos, Player player)
    {
        List<Position> outFlanked = new List<Position>();
        for (int rDelta = -1; rDelta <= 1; rDelta++)
        {
            for (int cDelta = -1; cDelta <= 1; cDelta++)
            {
                if (rDelta == 0 && cDelta == 0)
                {
                    continue;
                }

                outFlanked.AddRange(OutFlankedInDir(pos, player, rDelta, cDelta));
            }
        }

        return outFlanked;
    }

    //Returns the Player's legal moves as a Dictionary

    private bool IsMoveLegal(Player player, Position pos, out List<Position> outFlanked)
    {
        if (Board[pos.Row, pos.Col] != Player.None)
        {
            outFlanked = null;
            return false;
        }

        outFlanked = OutFlanked(pos, player);
        Debug.Log("Reached here!");
        return outFlanked.Count > 0;
    }

    //Dictionary Key is the position of new Disc.
    //Dictionary Value is a List of outflanked Discs positions.

    private Dictionary<Position, List<Position>> FindLegalMoves(Player player)
    {
        Dictionary<Position, List<Position>> legalMoves = new Dictionary<Position, List<Position>>();

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                Position pos = new Position(r, c);

                if(IsMoveLegal(player, pos, out List<Position> outFlanked))
                {
                    legalMoves[pos] = outFlanked;
                }
            }
        }

        return legalMoves;
    }

}
