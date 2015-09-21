using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Fanorona
{
    // Denotes whether a piece can move to only adjacent squares 
    // or to diagonal squares as well
    public enum TileMvt
    {
        Adjacent,
        Diagonal
    };

    // Denotes what kind of piece is on the point
    public enum TileState
    {
        Black,
        White,
        Selected, // White piece that is selected
        Empty
    };

    [Flags]
    public enum Direction
    {
        Up = 1, 
        Down = 2, 
        Left = 4, 
        Right = 8
    }

    // Represents a place where pieces can land on the board
    public class Tile
    {
        public TileMvt Type { get; set; }
        public TileState State { get; set; }

        public int Row { get; set; }
        public int Col { get; set; }

        public HashSet<Direction> PossibleDirections { get; set; } 

        public Tile(TileMvt t, int r, int c)
        {
            Type = t;
            State = TileState.Empty;
            Row = r;
            Col = c;
        }

        public Tile(Tile t)
        {
            Type = t.Type;
            State = t.State;
            Row = t.Row;
            Col = t.Col;
            PossibleDirections = t.PossibleDirections;
        }
        public override bool Equals(System.Object obj)
        {
            var tile = (Tile) obj;
            return tile.Row == this.Row && tile.Col == this.Col;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }


}
