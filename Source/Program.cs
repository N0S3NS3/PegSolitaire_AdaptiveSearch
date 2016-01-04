using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

//TO-DO: Implement the AI (MiniMax / AB-Pruning), Add Player Transitions, Add Win States

namespace PegSolitair
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            int input;
            int difficulty = -1, startingPlayer = -1;
            Console.WriteLine("Please Input a Level of Difficulty: 1) Easy 2) Normal 3) Hard");
            while (difficulty < 0)
            {
                input = Console.Read();
                switch (input)
                {
                    case 49:
                        difficulty = 1;         //Easy Difficulty (Dumb)
                        break;
                    case 50:
                        difficulty = 2;         //Medium Difficulty (SomeWhat)
                        break;
                    case 51:
                        difficulty = 3;         //Hard Difficulty (Intelligent)
                        break;
                }
            }
            Console.WriteLine("Please Input the Starting Player: 1) Player 2) CPU");
            while (startingPlayer < 0)
            {
                input = Console.Read();
                switch (input)
                {
                    case 49:
                        startingPlayer = 1;   //Player
                        break;
                    case 50:
                        startingPlayer = 0;   //CPU
                        break;
                }
            }
            Renderable render = new Renderable(difficulty, startingPlayer);
        }
    }
    class Renderable : GameWindow
    {
        private int[][] AIMove { get; set; }                    //Handler for AI Moves being returned from LogicThread
        private Thread logicThread { get; set; }                //Thread which handles the LogicBase AI
        private LogicBase logicBase { get; set; }               //Handles the Logic for the AI
        private SolitairBase solitairBase { get; set; }         //Handles Rendering the Game
        
        private int[] viewport = new int[4];                    //Viewport Matrix
        private double[] projectionMatrix = new double[16];     //Projection Matrix
        private double[] modelViewMatrix = new double[16];      //ModelView Matrix

        private int currentX;                                   //Handles the Current X Coordinate in Client Space
        private int currentY;                                   //Handles the Current Y Coordinate in Client Space
        private int currentTurn = -1;
        public int CurrentTurn { get { return currentTurn; }    //Handles the Game State Switch Between Player and AI
            set
            {
                if (currentTurn != value && value == 0)
                {
                    logicThread = new Thread(new ThreadStart(ExecuteLogicThread));
                    logicThread.Start();
                }
                currentTurn = value;
            }
        } // 0 = AI, 1 = Player
        public Renderable(int difficulty, int startingPlayer)
        {
            solitairBase = new SolitairBase();
            solitairBase.InitializeGameBoard();
            logicBase = new LogicBase();
            switch (difficulty)
            {
                case 1:
                    logicBase.InitializeLogic(LogicBase.Difficulty.Dumb);
                    break;
                case 2:
                    logicBase.InitializeLogic(LogicBase.Difficulty.Somewhat);
                    break;
                case 3:
                default:
                    logicBase.InitializeLogic(LogicBase.Difficulty.Very);
                    break;
            }
            CurrentTurn = startingPlayer;
            base.Run(30);
        }
        protected override void OnLoad(EventArgs eventargs)
        {
            currentX = 0;
            currentY = 0;
            base.OnLoad(eventargs);
            base.Title = "Peg Solitaire";
            base.VSync = VSyncMode.On;
            this.Mouse.ButtonDown += new EventHandler<MouseButtonEventArgs>(OnMouseDownHandler);
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Enable(EnableCap.ColorMaterial);
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();         
            GL.Ortho(0, 100.0, 100.0, 0.0, 0.0, 4.0);
            GL.GetDouble(GetPName.ProjectionMatrix, projectionMatrix);
            
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.GetDouble(GetPName.ModelviewMatrix, modelViewMatrix);
            GL.GetInteger(GetPName.Viewport, viewport);
            solitairBase.DrawBoard();                           //Draw the Current State of the Board
            SwapBuffers();                                      //Render
        }
        protected override void OnResize(EventArgs e)
        {   //Handles Resizing the Board to maintain Aspect Ratio Integrity
            base.OnResize(e);
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);  
        }
        protected void OnMouseDownHandler(object sender, OpenTK.Input.MouseButtonEventArgs e)
        {   //Handles MouseDown Events
            if (e.IsPressed && (currentX != e.X && currentY != e.Y))
                UpdateMouse(e.X, e.Y);       
        }
        private void UpdateMouse(int x, int y)
        {   //Handles Updating Mouse Coordinates, and Selecting Pieces
            if (currentX != x && currentY != y && CurrentTurn == 1)
            {
                WindowToClient(x, ref currentX, y, ref currentY);
                CurrentTurn = solitairBase.SelectPiece(currentX, currentY) ? 0 : 1;
            }
        }
        private void WindowToClient(int x, ref int currentX, int y, ref int currentY)
        {   //Translates the Client Coordinates to Appropriate World Coordinates 
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(projectionMatrix);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(modelViewMatrix);

            var box = (float)0.0;
            GL.ReadPixels(x, viewport[3] - y, 1, 1, OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent, PixelType.Float, ref box);
            Vector3 results;
            OpenTK.Graphics.Glu.UnProject(new Vector3(x, viewport[3] - y, 0.0f), modelViewMatrix, projectionMatrix, viewport, out results);
            currentX = (int)results.X;
            currentY = (int)results.Y;
        }
        private void ExecuteLogicThread()
        {   
            AIMove = logicBase.HandleAIMove(solitairBase.Board);
            if (AIMove != null)
            {
                solitairBase.TranslatePiece(AIMove);
            }
            currentTurn = 1;
        }
    }
}
