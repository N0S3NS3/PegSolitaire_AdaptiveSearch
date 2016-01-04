﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using PegSolitair.HelperBase;

namespace PegSolitair
{
    class SolitairBase
    {
        public class Piece                                          //Piece represents a single 'Square' location on the board
        {
            private int X               { get; set; }               //X Index
            private int Y               { get; set; }               //Y Index
            public int[][] ValidMoves   { get; set; }               //The Valid Moves Available at Current Location
            public int[][] Coordinates  { get; private set; }       //The coordinates of Piece based on Displacement Magnitude

            public Piece(int initialX, int column, int initialY, int row)
            {
                X = column;
                Y = row;
                Coordinates = new int[4][];
                Coordinates[0] = new int[] { initialX + (Displacement * column), initialY + (Displacement * row) };
                Coordinates[1] = new int[] { initialX + (Displacement * (column + 1)), initialY + (Displacement * row) };
                Coordinates[2] = new int[] { initialX + (Displacement * (column + 1)), initialY + (Displacement * (row + 1)) };
                Coordinates[3] = new int[] { initialX + (Displacement * column), initialY + (Displacement * (row + 1)) };
                ValidMoves = new int[4][];
            }
        };
        public GameBucket<int, int, Piece>[][] Board { get; private set; }      //Euclidean Subset Representation of Board 
        private int[] CurrentlySelected;                                        //Currently Selected Location based on X, Y index
        private const int Cols = 9;                                             
        private const int Rows = 9;
        private const int Displacement = 8;                                     //Magnitude of Displacement, Allows appropriate Grid Rendering.
        private const int InitialX = 10;                                        //Initial Starting X location of Grid
        private const int InitialY = 10;                                        //Initial Starting Y location of Grid

        private static MouseState mouseState { get; set; }                      //MouseState Handlr
        public enum MouseState
        {
            Picking,            //Picking State:        User is currently selecting a piece
            Translating         //Translating State:    User is able to translate piece to available location
        };

        public void InitializeGameBoard()
        {
            //Initializes the Board
            Board = new GameBucket<int, int, Piece>[Cols][];
            for (int i = 0; i < Cols; i++)
            {
                Board[i] = (new GameBucket<int,int,Piece>[Rows]);
            }
            for (int y = 0; y < Cols; y++)
            { 
                for (int x = 0; x < Rows; x++)
                {
                    if (((x >= 0 && x <= 2) && (y >= 0 && y <= 2)) || ((x >= 0 && x <= 2) && (y >= 6 && y <= 8))
                        || ((x >= 6 && x <= 8) && (y >= 0 && y <= 2)) || ((x >= 6 && x <= 8) && (y >= 6 && y <= 8)))
                    {
                        Board[x][y] = null;
                        continue;
                    }
                    else if (x == 4 && y == 4)
                        Board[x][y] = (new GameBucket<int, int, Piece>(0, 0, new Piece(InitialX, x, InitialY, y)));

                    else
                        Board[x][y] = (new GameBucket<int, int, Piece>(1, 0, new Piece(InitialX, x, InitialY, y)));
                }
            }
            mouseState = MouseState.Picking;
        }
        public void DrawBoard()
        {
            //Board Rendering 
            for (int x = 0; x < Cols; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Board[x][y] == null)
                        continue;
                    if (Board[x][y].Item2 == 1)         //If Current Piece is Selected, Highlight the Selected and Valid Locations
                    {
                        GL.Color3(new byte[] { 255, 0, 0 });
                        GL.Begin(PrimitiveType.Quads);
                        GL.Vertex3(Board[x][y].Item3.Coordinates[0][0], Board[x][y].Item3.Coordinates[0][1], 0);
                        GL.Vertex3(Board[x][y].Item3.Coordinates[1][0], Board[x][y].Item3.Coordinates[1][1], 0);
                        GL.Vertex3(Board[x][y].Item3.Coordinates[2][0], Board[x][y].Item3.Coordinates[2][1], 0);
                        GL.Vertex3(Board[x][y].Item3.Coordinates[3][0], Board[x][y].Item3.Coordinates[3][1], 0);
                        GL.End();

                        foreach (int[] validCoordinates in Board[x][y].Item3.ValidMoves)
                        {
                            if (validCoordinates != null)
                            {
                                GL.Begin(PrimitiveType.Quads);
                                GL.Vertex3(Board[validCoordinates[0]][validCoordinates[1]].Item3.Coordinates[0][0], Board[validCoordinates[0]][validCoordinates[1]].Item3.Coordinates[0][1], 0);
                                GL.Vertex3(Board[validCoordinates[0]][validCoordinates[1]].Item3.Coordinates[1][0], Board[validCoordinates[0]][validCoordinates[1]].Item3.Coordinates[1][1], 0);
                                GL.Vertex3(Board[validCoordinates[0]][validCoordinates[1]].Item3.Coordinates[2][0], Board[validCoordinates[0]][validCoordinates[1]].Item3.Coordinates[2][1], 0);
                                GL.Vertex3(Board[validCoordinates[0]][validCoordinates[1]].Item3.Coordinates[3][0], Board[validCoordinates[0]][validCoordinates[1]].Item3.Coordinates[3][1], 0);
                                GL.End();
                            }
                        }                       
                    }
                    if (Board[x][y].Item1 == 1)
                    {
                        GL.Color3(new byte[] { 0, 255, 255 });
                        GL.Begin(PrimitiveType.TriangleFan);
                        for (int t = 0; t <= 360; t++)
                        {
                            GL.Vertex3((((Board[x][y].Item3.Coordinates[1][0] + Board[x][y].Item3.Coordinates[0][0]) / 2) + Math.Cos(t * Math.PI / 180)),
                                (((Board[x][y].Item3.Coordinates[0][1] + Board[x][y].Item3.Coordinates[2][1]) / 2) + Math.Sin(t * Math.PI / 180)), 0.0);
                        }
                        GL.End();
                    }
                    GL.Color3(new byte[] { 255, 255, 255 });
                    GL.Begin(PrimitiveType.LineLoop);
                    GL.Vertex3(Board[x][y].Item3.Coordinates[0][0], Board[x][y].Item3.Coordinates[0][1], 0);
                    GL.Vertex3(Board[x][y].Item3.Coordinates[1][0], Board[x][y].Item3.Coordinates[1][1], 0);
                    GL.Vertex3(Board[x][y].Item3.Coordinates[2][0], Board[x][y].Item3.Coordinates[2][1], 0);
                    GL.Vertex3(Board[x][y].Item3.Coordinates[3][0], Board[x][y].Item3.Coordinates[3][1], 0);
                    GL.End();
                }
            }
        }
        public bool SelectPiece(int currentX, int currentY)
        {
            for (int x = 0; x < Cols; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Board[x][y] != null)
                    {
                        if ((currentX > Board[x][y].Item3.Coordinates[0][0] && currentX < Board[x][y].Item3.Coordinates[1][0])          //If currentX and current Y are Within
                            && (currentY > Board[x][y].Item3.Coordinates[0][1] && currentY < Board[x][y].Item3.Coordinates[2][1]))      //Valid Coordinates
                        {
                            switch (mouseState)
                            {
                                case MouseState.Picking:
                                    {
                                        if (Board[x][y].Item1 == 1)
                                        {
                                            if (CurrentlySelected == null)
                                            {
                                                CurrentlySelected = new int[] { x, y };
                                            }
                                            else
                                            {
                                                CurrentlySelected[0] = x;
                                                CurrentlySelected[1] = y;
                                            }
                                            Board[x][y].Item2 = 1;
                                            Board[x][y].Item3.ValidMoves = CalculateValidMoves(x, y);
                                            mouseState = MouseState.Translating;
                                        }
                                        break;
                                    }
                                case MouseState.Translating:
                                    {
                                        if (x == CurrentlySelected[0] && y == CurrentlySelected[1])
                                        {
                                            Board[CurrentlySelected[0]][CurrentlySelected[1]].Item2 = 0;
                                            CurrentlySelected = null;
                                            mouseState = MouseState.Picking;
                                            break;
                                        }
                                        if (Board[x][y].Item1 == 1)
                                        {
                                            Board[CurrentlySelected[0]][CurrentlySelected[1]].Item2 = 0;
                                            Board[x][y].Item2 = 1;
                                            Board[x][y].Item3.ValidMoves = CalculateValidMoves(x, y);
                                            CurrentlySelected[0] = x;
                                            CurrentlySelected[1] = y;
                                        }
                                        else
                                        {
                                            foreach (int[] validMoves in Board[CurrentlySelected[0]][CurrentlySelected[1]].Item3.ValidMoves)
                                            {
                                                if (validMoves != null)
                                                {
                                                    if (x == validMoves[0] && y == validMoves[1])
                                                    {
                                                        TranslatePiece(CurrentlySelected, new int[] { x, y });
                                                        mouseState = MouseState.Picking;
                                                        return true;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    }
                            }
                        }

                    }
                }
            }
            return false;
        }
        public int[][] CalculateValidMoves(int X, int Y)
        {
            int[][] validMoves = new int[4][];
            if ((Y - 2 >= 0) && Board[X][Y - 2] != null && Board[X][Y - 2].Item1 == 0)         //North
            {
                if (Board[X][Y - 1].Item1 == 1)
                {
                    validMoves[0] = new int[] { X, Y - 2 };
                }
            }
            if ((X + 2 < Cols) && Board[X + 2][Y] != null && Board[X + 2][Y].Item1 == 0)      //East
            {
                if (Board[X + 1][Y].Item1 == 1)
                {
                    validMoves[1] = new int[] { X + 2, Y };
                }
            }
            if ((X - 2 > 0) && Board[X - 2][Y] != null && Board[X - 2][Y].Item1 == 0)         //West
            {
                if (Board[X - 1][Y].Item1 == 1)
                {
                    validMoves[2] = new int[] { X - 2, Y };
                }
            }
            if ((Y + 2 < Rows) && Board[X][Y + 2] != null && Board[X][Y + 2].Item1 == 0)      //South
            {
                if (Board[X][Y + 1].Item1 == 1)
                {
                    validMoves[3] = new int[] { X, Y + 2 };
                }
            }
            return validMoves;
        }
        public void TranslatePiece(int[] source, int[] destination)
        {
            Board[source[0]][source[1]].Item1 = 0;
            Board[source[0]][source[1]].Item2 = 0;
            int[] translation = new int[]{(source[0] < destination[0]) ? destination[0] - 1 : (source[0] > destination[0]) ? destination[0] + 1 : destination[0],
                (source[1] < destination[1]) ? destination[1] - 1 : (source[1] > destination[1]) ? destination[1] + 1 : destination[1]};
            Board[translation[0]][translation[1]].Item1 = 0;
            Board[destination[0]][destination[1]].Item1 = 1;
            CurrentlySelected = null;
        }
        public void TranslatePiece(int[][] jump)
        {
            Board[jump[0][0]][jump[0][1]].Item1 = 0;
            Board[jump[0][0]][jump[0][1]].Item2 = 0;
            Board[jump[1][0]][jump[1][1]].Item1 = 0;
            Board[jump[2][0]][jump[2][1]].Item1 = 1;
        }
    }
}