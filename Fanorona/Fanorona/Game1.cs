using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Policy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

/*
 * 
 * Shannon Li
 * CS6613 Artificial Intelligence
 * Final Project
 * Fanorona
 * 12/14/2014
 * 
 **/

namespace Fanorona
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private enum Player { Human, Computer };
        private enum PlayerTurnState {NoMove, PieceSelected, SelectCapture, Moved}
        private enum GameState { Menu, Playing, GameOver };
        public enum Difficulty { Easy = 1, Medium = 2, Hard = 3 };

        // Number of difficulties available to choose from
        private int DIFFS = Enum.GetValues(typeof (Difficulty)).Length;

        // Number of board types to choose from
        private int BOARDS = Enum.GetValues(typeof (BoardType)).Length;

        private const int HEIGHT = 600;
        private const int WIDTH = 800;

        private Player CurrentPlayer;
        private GameState CurrentState;
        private PlayerTurnState CurrentPlayerTurnState;
        private bool CanGoToNextTurn;
        private Tile SelectedPiece;
        private HashSet<Direction> CurrentMoveChain;
        private Difficulty GameDifficulty;
        private Board GameBoard;
        
        private MouseState prevMouseState;

        Texture2D menuScreen;
        Texture2D piece;
        Texture2D pieceOutline;
        Texture2D boardThree;
        Texture2D boardFive;

        Texture2D boardToUse;

        Color selectedColor = Color.Yellow;
        private SpriteFont sf;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferHeight = HEIGHT;
            graphics.PreferredBackBufferWidth = WIDTH;
            IsMouseVisible = true;

        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            CurrentPlayer = Player.Human;
            CurrentState = GameState.Menu;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            menuScreen = Content.Load<Texture2D>("sprites/menu");
            piece = Content.Load<Texture2D>("sprites/white");
            pieceOutline = Content.Load<Texture2D>("sprites/outline");
            boardThree = Content.Load<Texture2D>("sprites/board_3");
            boardFive = Content.Load<Texture2D>("sprites/board_5");

            sf = Content.Load<SpriteFont>("fonts/SF");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            MouseState curMouse = Mouse.GetState(); // Check if mouse was clicked
            if (this.IsActive && prevMouseState.LeftButton != ButtonState.Pressed &&
                curMouse.LeftButton == ButtonState.Pressed)
            {
                int x = curMouse.X, y = curMouse.Y;
                switch (CurrentState)
                {
                    case GameState.Menu:
                        // check for button clicks
                        MenuButtonSelect(x,y);
                        break;
                    case GameState.Playing:
                        // Check for clicks on pieces
                        if (CurrentPlayer == Player.Human)
                        {
                            HandlePieceClick(x, y);
                        }
                        else
                        {
                            MakeBestMove();
                        }
                        // Check for win state
                        if (GameBoard.HasWinner())
                        {
                            CurrentState = GameState.GameOver;
                        }
                        break;
                    case GameState.GameOver:
                        CurrentState = GameState.Menu;
                        break;
                }
            }

            prevMouseState = curMouse;

            base.Update(gameTime);
        }

        private const int D = 5;

        private void MakeBestMove()
        {
            var possibleMoves = GameBoard.GetPossibleMoves(TileState.Black);
            var move = AlphaBetaSearch(new Board(GameBoard), (int)GameDifficulty*D);
            GameBoard.MovePiece(move.startTile, GameBoard.GetTileInDirection(move.startTile, move.direction));
            if (move.CapturedTiles.Keys.Any())
            {
                GameBoard.MakeMove(move, move.CapturedTiles.Keys.First());
            }

            CurrentPlayer = Player.Human;
            CurrentPlayerTurnState = PlayerTurnState.NoMove;
        }

        /// <summary>
        /// Returns information about the AlphaBeta search
        /// </summary>
        private class ABStruct
        {
            public int val { get; set; }
            public Board.Move move { get; set; }
            public int depth { get; set; }
            public bool cutOff = false;

        }
        int maxPrunes = 0;
        int minPrunes = 0;

        /// <summary>
        /// Performs an AlphaBeta Search with a given state and cut-off value.
        /// </summary>
        /// <param name="state">A copy of the current game board state</param>
        /// <param name="expand">The max number of depths to go through</param>
        /// <returns></returns>
        private Board.Move AlphaBetaSearch(Board state, int expand)
        {
            numNodes = 0;
            // 1 = black wins, -1 = black loses
            var v = MaxValue(state, -1, 1, expand);
            Console.WriteLine("1. Cutoff: " + (v.cutOff));
            Console.WriteLine("2. Max Depth: " + ((int)GameDifficulty*D - (v.depth)));
            Console.WriteLine("3. Total number of nodes: " + numNodes);
            Console.WriteLine("5. NumPrunes-Min: " + minPrunes);
            Console.WriteLine("6. NumPrunes-Max: " + maxPrunes);
            return v.move;
        }

        int numNodes;

        #region Alpha Beta Search Algorithm

        private ABStruct MaxValue(Board state, int a, int b, int expand)
        {
            numNodes++;
            var abst = new ABStruct() {val = -1, move = null, depth = expand};
            var moves = state.GetPossibleMoves(TileState.Black);
            if (!moves.Any())
                return abst;
            var v = -1;
            expand --;
            if (expand > 0)
            {
                foreach (var move in moves)
                {
                    var result = new Board(state);
                    result.MovePiece(move.startTile, GameBoard.GetTileInDirection(move.startTile, move.direction));

                    if (move.CapturedTiles.Keys.Any())
                    {
                        foreach (var dir in move.CapturedTiles.Keys)
                        {
                            move.captureDirection = dir;
                            result.MakeMove(move, dir);

                            numNodes++;
                            abst = MinValue(result, a, b, expand);
                            v = Math.Max(v, abst.val);
                            abst.move = move;
                            if (v >= b)
                            {
                                maxPrunes++;
                                return abst;
                            }
                            a = Math.Max(a, v);
                        }
                    }
                    else
                    {
                        numNodes++;
                        abst = MinValue(result, a, b, expand);
                        v = Math.Max(v, abst.val);
                        abst.move = move;
                        if (v >= b)
                        {
                            maxPrunes++;
                            return abst;
                        }
                        a = Math.Max(a, v);
                    }
                }
            }
            else
            {
                abst.move = moves.First();
                abst.cutOff = true;
            }

            return abst;
        }

        private ABStruct MinValue(Board state, int a, int b, int expand)
        {
            var abst = new ABStruct() {val = 1, move = null, depth = expand};
            var moves = state.GetPossibleMoves(TileState.White);
            if (!moves.Any())
                return abst;
            var v = 1;
            expand --;
            if (expand > 0)
            {
                foreach (var move in moves)
                {
                    var result = new Board(state);
                    result.MovePiece(move.startTile, GameBoard.GetTileInDirection(move.startTile, move.direction));

                    if (move.CapturedTiles.Keys.Any())
                    {
                        foreach (var dir in move.CapturedTiles.Keys)
                        {
                            result.MakeMove(move, dir);
                            move.captureDirection = dir;

                            numNodes++;
                            abst = MaxValue(result, a, b, expand);
                            abst.move = move;
                            v = Math.Min(v, abst.val);
                            if (v <= a)
                            {
                                minPrunes++;
                                return abst;
                            }
                            b = Math.Min(b, v);
                        }

                    }
                    else
                    {
                        numNodes++;
                        abst = MaxValue(result, a, b, expand);
                        abst.move = move;
                        v = Math.Min(v, abst.val);
                        if (v <= a)
                        {
                            minPrunes++;
                            return abst;
                        }
                        b = Math.Min(b, v);
                    }

                }
            }
            else
            {
                abst.move = moves.First();
                //abst.val = 
                abst.cutOff = true;
            }
            return abst;
        }


        private Board.Move possibleMove;

        #endregion


        private void HandlePieceClick(int x, int y)
        {
            var tile = ClickedOnTile(x, y);
            switch (CurrentPlayerTurnState)
            {
                case PlayerTurnState.NoMove:
                    Console.WriteLine("NoMove");
                    if (tile != null && tile.State != TileState.Empty) // Player clicked on a piece
                    {
                        tile.State = TileState.Selected;
                        CurrentPlayerTurnState = PlayerTurnState.PieceSelected;
                        SelectedPiece = tile;
                    }
                    break;
                case PlayerTurnState.PieceSelected:
                    Console.WriteLine("PieceSelected");
                    if (tile != null && tile.State == TileState.Empty) // Player clicked on empty spot
                    {
                        bool legal = MakePlayerMove(SelectedPiece, tile);
                        if (!legal)
                        {
                            // TODO: Display error message
                            Console.WriteLine("Illegal move");
                            return;
                        }
                        CurrentPlayerTurnState = PlayerTurnState.Moved;
                    }
                break;
                case PlayerTurnState.SelectCapture:
                    Console.WriteLine("SelectCapture");
                    if (possibleMove != null)
                    {
                        
                    }
                break;
                case PlayerTurnState.Moved:
                    if(tile != null){}
                break;
            }
            
        }

        private void EndTurn()
        {
            CurrentPlayerTurnState = PlayerTurnState.NoMove;
            SelectedPiece = null;
            CurrentPlayer = Player.Computer;
            possibleMove = null;
        }

        private bool MakePlayerMove(Tile start, Tile dest)
        {
            Direction d = Board.GetDirection(start, dest);

            // check whether the direction has already been used
            // and whether moving was successful
            var move = GameBoard.MovePiece(start, dest);
            if (!CurrentMoveChain.Contains(d) && move!=null)
            {

                //CurrentMoveChain.Add(d);
                if (move.CapturedTiles.Keys.Count > 1)
                {
                    possibleMove = move;
                    GameBoard.MakeMove(move, move.CapturedTiles.Keys.First());
                }
                else
                {
                    //CurrentPlayerTurnState = PlayerTurnState.Moved;
                    if (move.CapturedTiles.Keys.Any())
                    {
                        var dir = move.CapturedTiles.Keys.First();
                        GameBoard.MakeMove(move, dir);
                    }
                }
                EndTurn();
                Console.WriteLine("Added "+ d.ToString());
                return true;
            }
            
            return false;
        }

        private Tile ClickedOnTile(int x, int y)
        {
            foreach (var tile in GameBoard.Pieces)
            {
                Vector2 startLoc = GetLocation(tile.Row, tile.Col);
                Vector2 endLoc = startLoc + new Vector2(pieceSize);

                if (x > startLoc.X && y > startLoc.Y
                    && x < endLoc.X && y < endLoc.Y)
                {
                    Console.WriteLine("Clicked on " + tile.Row + "," + tile.Col);
                    return tile;
                }
            }
            return null;
        }

        private Vector2 GetLocation(int r, int c)
        {
            return new Vector2(c*pieceDistance + boardOffset - pieceSize/2, 
                r*pieceDistance + boardOffset - pieceSize/2);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
            switch (CurrentState)
            {
                case GameState.Menu:
                    spriteBatch.Draw(menuScreen, new Vector2(), Color.White);
                    break;
                case GameState.Playing:
                    DrawGameboard();
                    break;
            }

            base.Draw(gameTime);
            spriteBatch.End();
        }

        // Game board locations
        private int boardOffset = 100;
        Vector2 boardStart = new Vector2(100,100);
        private int pieceDistance = 50;
        private int pieceSize = 20;
        private void DrawGameboard()
        {
            spriteBatch.Draw(boardToUse, boardStart, Color.White );
            for (int r = 0; r < GameBoard.NumRows; r++)
            {
                for (int c = 0; c < GameBoard.NumCols; c++)
                {
                    Color pieceColor = Color.White;
                    switch (GameBoard.Pieces[r, c].State)
                    {
                        case TileState.White:
//                            Console.WriteLine("White at " + r + ", " + c);
                            break;
                        case TileState.Black:
//                            Console.WriteLine("Black at " + r + ", " + c);
                            pieceColor = Color.Black;
                            break;
                        case TileState.Selected:
                            pieceColor = Color.Yellow;
                            break;
                        case TileState.Empty:
//                            Console.WriteLine("Empty at " + r + ", " + c);
                            continue;
                    }
                    Vector2 location = new Vector2(c*pieceDistance+boardOffset - pieceSize/2, r*pieceDistance+boardOffset - pieceSize/2);
                    spriteBatch.Draw(piece, location, pieceColor);
                }
            }

        }

        // Button location info
        private const int buttonX = 480,
            // starting x value 
            buttonWidth = 80,
            buttonHeight = 35;
        private readonly int[] buttonY = 
        {
            // starting Y value for first cluster (3x3) of buttons
            215,
            // starting Y value for second cluster (5x5)
            390
        };

        /*
         * 3x3:
         *  easy - 480 x 215 -- 560 x 250 (80px x 35px)
         *  med -  480 x 250 -- 560 x 285
         *  hard - 480 x 285 -- 560 x 320
         *  
         * 5x5:
         *  easy - 480 x 390 -- 560 x 425
         *  med -  480 x 
        */
        private void MenuButtonSelect(int x, int y)
        {
            if (ClickedOnButton(x, y))
            {
                for(int b = 0; b < BOARDS; b++)
                {
                    for (int d = 0; d < DIFFS; d++)
                    {
                        if (y < buttonY[b] + (d+1)*buttonHeight)
                        {
                            var bt = (BoardType) Enum.GetValues(typeof (BoardType)).GetValue(b);
                            GameBoard = new Board(bt);
                            GameDifficulty = (Difficulty)Enum.GetValues(typeof(Difficulty)).GetValue(d);
                            
                            InitializeGameStateVars();

                            switch (GameBoard.Type)
                            {
                                case BoardType.Three:
                                    boardToUse = boardThree;
                                    break;
                                case BoardType.Five:
                                    boardToUse = boardFive;
                                    break;
                            }
                            return;
                        }
                    }
                }
            }
        }

        private void InitializeGameStateVars()
        {
            CurrentState = GameState.Playing;
            CurrentPlayer = Player.Human;
            CurrentPlayerTurnState = PlayerTurnState.NoMove;

            CanGoToNextTurn = false;
            CurrentMoveChain = new HashSet<Direction>();
        }


        /// <summary>
        /// Check whether mouse location is within boundaries for any buttons
        /// </summary> 
        private bool ClickedOnButton(int x, int y)
        {
            // If not outside the boundaries
            return !(x < buttonX || x > buttonX + buttonWidth ||
                    y < buttonY[0] ||
                    (y > buttonY[0] + DIFFS * buttonHeight && y < buttonY[1]) ||
                    y > buttonY[1] + DIFFS * buttonHeight);
        }
    }
}
