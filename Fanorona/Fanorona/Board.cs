using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Fanorona
{
    public enum BoardType
    {
        Three = 0,
        Five = 1
    };


    public class Board
    {
        public BoardType Type { get; set; }

        // [rows,columns]
        public Tile[,] Pieces { get; set; }

        public int NumRows { get { return Pieces.GetLength(0); } }
        public int NumCols { get { return Pieces.GetLength(1); } }


        public Tile SelectedPiece { get; set; }

        public Board(BoardType bt)
        {
            Type = bt;
            switch (bt)
            {
                case BoardType.Three:
                    BuildBoard(3,3);
                    break;
                case BoardType.Five:
                    BuildBoard(5,5);
                    break;
            }
            SelectedPiece = null;
        }

        public Board(Board b)
        {
            Type = b.Type;
            Pieces = new Tile[b.NumRows,b.NumCols];

            for (int i = 0; i < NumRows; i++)
            {
                for (int j = 0; j < NumCols; j++)
                {
                    Pieces[i,j] = new Tile(b.Pieces[i,j]);
                }
            }
        }

        public enum MoveType 
        {
            Capturing,
            Paika
        }

        public class Move
        {
            public Tile startTile { get; set; }
            public Direction direction { get; set; }
            public IDictionary<Direction, ICollection<Tile>> CapturedTiles { get; set; }
            public Direction captureDirection { get; set; }
            public Move(Tile t, Direction d, IDictionary<Direction, ICollection<Tile>> captured)
            {
                startTile = t;
                direction = d;
                CapturedTiles = captured;
            }

            public override bool Equals(object obj)
            {
                Move move = (Move) obj;
                return startTile.Equals(move.startTile) &&
                       direction == move.direction;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }



        public bool PieceExists(int r, int c)
        {
            return (r >= 0 && r < NumRows && c >= 0 && c < NumCols);
        }

        public bool HasWinner()
        {
            bool hasBlack = false;
            bool hasWhite = false;
            foreach (var tile in Pieces)
            {
                if (tile.State == TileState.Selected || tile.State == TileState.White)
                {
                    hasWhite = true;
                }
                if (tile.State == TileState.Black)
                {
                    hasBlack = true;
                }
            }
            return !(hasWhite && hasBlack);
        }

        public Tile GetTileInDirection(Tile start, Direction d)
        {
            int xChange = 0;
            int yChange = 0;


            if (start.Type == TileMvt.Adjacent
                && GetTileMvt(d) == TileMvt.Diagonal)
            {
                return null;
            }

            if (d.HasFlag(Direction.Up))
            {
                yChange += -1;
            }
            if (d.HasFlag(Direction.Down))
            {
                yChange += 1;
            }
            if (d.HasFlag(Direction.Left))
            {
                xChange += -1;
            }
            if (d.HasFlag(Direction.Right))
            {
                xChange += 1;
            }

            int newRow = start.Row + yChange;
            int newCol = start.Col + xChange;

            if (!PieceExists(newRow, newCol)) return null;

            return Pieces[newRow, newCol];
        }

        public bool TileExistsInDirection(Tile start, Direction d)
        {
            int xChange = 0;
            int yChange = 0;


            if (start.Type == TileMvt.Adjacent
                && GetTileMvt(d) == TileMvt.Diagonal)
            {
                return false;
            }

            if (d.HasFlag(Direction.Up))
            {
                yChange += -1;
            }
            if (d.HasFlag(Direction.Down))
            {
                yChange += 1;
            }
            if (d.HasFlag(Direction.Left))
            {
                xChange += -1;
            }
            if (d.HasFlag(Direction.Right))
            {
                xChange += 1;
            }

            int newRow = start.Row + yChange;
            int newCol = start.Col + xChange;

            if (!PieceExists(newRow, newCol)) return false;

            return true;
            
        }
        public bool IsPaika(Tile start, Tile dest)
        {
            Direction d = GetDirection(start, dest);
            var tileInDir = GetTileInDirection(dest, d);
            var tileInOppDir = GetTileInDirection(start, GetOppositeDirection(d));
            // Check if in both directions tiles are empty or nonexistant
            return !( IsCapturablePiece(start, tileInDir)
                || IsCapturablePiece(start, tileInOppDir));
        }
        public bool IsPaika(Tile start, Direction dir)
        {
            return IsPaika(start, GetTileInDirection(start, dir));
        }

        public bool IsCapturablePiece(Tile start, Tile target)
        {
            if (target == null) return false;

            TileState a = start.State;
            TileState b = target.State;
            if (a == TileState.Selected) a = TileState.White;
            if (b == TileState.Selected) b = TileState.White;

            return (a == TileState.White && b == TileState.Black
                    || a == TileState.Black && b == TileState.White);
        }
        public Direction GetOppositeDirection(Direction d)
        {
            Direction dir = new Direction();
            if(d.HasFlag(Direction.Up))     dir |= Direction.Down;
            if (d.HasFlag(Direction.Down)) dir |= Direction.Up;
            if (d.HasFlag(Direction.Left)) dir |= Direction.Right;
            if (d.HasFlag(Direction.Right)) dir |= Direction.Left;

            return dir;
        }

        public void CaptureTile(int r, int c)
        {
            Pieces[r, c].State = TileState.Empty;
        }

        public MoveType GetMoveType(Tile start, Tile dest)
        {
            if (IsPaika(start, dest))
            {
                return MoveType.Paika;
            }
            else return MoveType.Capturing;
        }

        public IDictionary<Direction, ICollection<Tile>> GetCapturablePieces(Tile start, Direction dir)
        {
            var capturable = new Dictionary<Direction, ICollection<Tile>>();
            var piece = GetTileInDirection(start, dir);
            if(piece != null)
            {
                while (IsCapturablePiece(start, GetTileInDirection(piece, dir)))
                {
                    var target = GetTileInDirection(piece, dir);
                    if (!capturable.ContainsKey(dir))
                    {
                        capturable[dir] = new List<Tile>();
                    }
                    capturable[dir].Add(target);

                    piece = target;
                }
            }

            var oppDir = GetOppositeDirection(dir);
            piece = start;
            if(piece != null)
            {
                while (IsCapturablePiece(start, GetTileInDirection(piece, oppDir)))
                {
                    var target = GetTileInDirection(piece, oppDir);
                    if (!capturable.ContainsKey(oppDir))
                    {
                        capturable[oppDir] = new List<Tile>();
                    }
                    capturable[oppDir].Add(target);

                    piece = target;
                }
            }

            return capturable;
        }

        public Move MovePiece(Tile start, Tile dest)
        {
            var state = start.State == TileState.Selected || start.State == TileState.White ?
                TileState.White : TileState.Black;
            var dir = GetDirection(start, dest);
            var move = new Move(start, dir, GetCapturablePieces(start, dir));
            // Validate
            if (GetPossibleMoves(state).Contains(move))
            {
                int startR = move.startTile.Row;
                int startC = move.startTile.Col;
                int destR = dest.Row;
                int destC = dest.Col;
                Pieces[startR, startC].State = TileState.Empty;
                Pieces[destR, destC].State = state;

                return move;
            }
            return null;
        }

        public void MakeMove(Move move, Direction captureDir)
        {
            foreach (var tile in move.CapturedTiles[captureDir])
            {
                CaptureTile(tile.Row, tile.Col);
            }
            
        }

        /// <summary>
        /// Finds all the possible moves for a given <paramref>tileState</paramref>.
        /// <paramref>tileState</paramref> should NOT be "Empty"
        /// </summary>
        /// <returns>Returns a list of Moves</returns>
        public ICollection<Move> GetPossibleMoves(TileState tileState)
        {
            var possibleMoves = new List<Move>();
            if (tileState == TileState.Empty) return possibleMoves;
            if (tileState == TileState.Selected) tileState = TileState.White;

            bool existsCaptureMove = false;

            foreach (var tile in Pieces)
            {
                var state = tile.State;
                if (state == TileState.Selected) state = TileState.White;
                if (state == tileState)
                {
                    var dests = GetPossibleDestinations(tile);
                    foreach (var dest in dests)
                    {
                        var dir = GetDirection(tile, dest);
                        var moveType = GetMoveType(tile, dest);
                        if (moveType == MoveType.Capturing) existsCaptureMove = true;

                        var move = new Move(tile, dir, GetCapturablePieces(tile, dir));
                        possibleMoves.Add(move);
                    }
                }
            }
            if (existsCaptureMove)
            { // remove all Paika moves
                possibleMoves.RemoveAll(m => !m.CapturedTiles.Any());
            }
            return possibleMoves;
        }

        private ICollection<Tile> GetPossibleDestinations(Tile t)
        {
            return GetPossibleDestinations(t.Row, t.Col);
        }

        public ICollection<Tile> GetPossibleDestinations(int r, int c)
        {
            // return the empty neighbors
            return GetNeighbors(r, c).Where(n => n.State == TileState.Empty).ToList();
        }

        public bool AreNeighbors(Tile a, Tile b)
        {
            // TODO: Use better implementation
            return (GetNeighbors(a.Row, a.Col).Contains(b));
        }

        public static Direction GetDirection(Tile start, Tile dest)
        {
            Direction d = new Direction();
            int x = dest.Col - start.Col;
            int y = dest.Row - start.Row;
            if (x < 0) // left
            {
                d |= Direction.Left;
            }
            else if (x > 0) // right
            {
                d |= Direction.Right;
            }

            if (y < 0) // up
            {
                d |= Direction.Up;
            }
            else if (y > 0) // down
            {
                d |= Direction.Down;
            }

            return d;
        }


        public static TileMvt GetTileMvt(Direction d)
        {
            if ((d & (d -1)) == 0)
            {
                return TileMvt.Adjacent;
            }
            else return TileMvt.Diagonal;
        }

        private IEnumerable<Tile> GetNeighbors(int r, int c)
        {
            bool isAdjacent = Pieces[r, c].Type == TileMvt.Adjacent;
            var neighbors = new List<Tile>();
            for (int i = r - 1; i <= r + 1; i++)
            {
                for (int j = c - 1; j <= c + 1; j++)
                {
                    if (i >= 0 && i < NumRows && j >= 0 && j < NumCols // not out of bounds
                        && !(i==r && j==c) // and not itself
                        && (!isAdjacent || !(i != r && j != c))) // if can move diagonally
                    {
                        neighbors.Add(Pieces[i, j]);
                    }
                }
            }
            return neighbors;
        }

        private bool IsDiagonal(int r, int c)
        {
            // The point can move diagonally only 
            // if it is an [even, even] or [odd, odd] point
            return r % 2 == 0 && c % 2 == 0 ||
                   r % 2 != 0 && c % 2 != 0;
        }

        private void BuildBoard(int r, int c)
        {
            Pieces = new Tile[r, c];

            PopulateMovements();

            PopulatePieces();
        }

        private void PopulateMovements()
        {
            // Initialize board movements
            for (var i = 0; i < NumRows; i++)
            {
                for (var j = 0; j < NumCols; j++)
                {
                    Pieces[i, j] = IsDiagonal(i, j) ? new Tile(TileMvt.Diagonal, i, j) { PossibleDirections = GetBothDirections() }
                                    : new Tile(TileMvt.Adjacent, i,j){PossibleDirections =GetAdjacentDirections()};
                    // Remove impossible directions
                    Pieces[i, j].PossibleDirections.RemoveWhere(d =>
                        !TileExistsInDirection(Pieces[i, j], d));
                    /*foreach (var dir in Pieces[i, j].PossibleDirections)
                    {
                        if (GetTileInDirection(Pieces[i, j], dir) == null)
                        {
                            Pieces[i, j].PossibleDirections.Remove(dir);
                        }
                    }*/
                }
            }
        }

        private HashSet<Direction> GetAdjacentDirections()
        {
            return new HashSet<Direction>
            {
                Direction.Up,
                Direction.Down,
                Direction.Left,
                Direction.Right
            };
        }

        private HashSet<Direction> GetDiagonalDirections()
        {
            return new HashSet<Direction>
            {
                Direction.Up | Direction.Left,
                Direction.Up | Direction.Right,
                Direction.Down | Direction.Left,
                Direction.Down | Direction.Right,
            };
        }

        private HashSet<Direction> GetBothDirections()
        {
            HashSet<Direction> list = GetAdjacentDirections();
            list.UnionWith(GetDiagonalDirections());
            return list;
        }

        private void PopulatePieces()
        {
            int cr = (NumRows / 2) ; // center row
            // Populate the top half black
            for (var i = 0; i < cr; i++)
            {
                for (var j = 0; j < NumCols; j++)
                {
                    Pieces[i, j].State = TileState.Black;
                }
            }

            int cc = (NumCols / 2) ; // center column
            // Populate the center row alternating (Black, White)
            // and make the center piece empty
            for (var j = 0; j < NumCols; j++)
            {
                if (j < cc)
                    Pieces[cr, j].State = j % 2 == 0 ? TileState.Black : TileState.White;
                else
                    if (j == cc)
                    {
                        Console.WriteLine("Center at " + cr + ", " + j);
                        Pieces[cr, j].State = TileState.Empty;
                    }
                 else Pieces[cr, j].State = j % 2 == 0 ? TileState.White : TileState.Black;   
            }

            // Populate last half white
            for (var i = cr+1; i < NumRows; i++)
            {
                for (var j = 0; j < NumCols; j++)
                {
                    Pieces[i, j].State = TileState.White;
                }
            }
        }

    }
}
