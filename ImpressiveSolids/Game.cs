using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace ImpressiveSolids {
    class Game : GameWindow {
        [STAThread]
        static void Main() {
            using (var Game = new Game()) {
                Game.Run(30);
            }
        }

        private const int NominalWidth = 700;
        private const int NominalHeight = 500;

        private float ProjectionWidth;
        private float ProjectionHeight;

        private const int SolidSize = 35;

        private Random Rand;

        private const int MapWidth = 7;
        private const int MapHeight = 13;

        private int[,] Map;
        private float[,] ImpactFallOffset;

        private const int StickLength = 3;
        private int[] StickColors;
        private Vector2 StickPosition;

        private const float FallSpeed = 0.2f;

        private const int ColorsCount = 5;
        private Color4[] Colors = {Color4.PaleVioletRed, Color4.LightSeaGreen, Color4.CornflowerBlue, Color4.RosyBrown, Color4.LightGoldenrodYellow};

        private enum GameStateEnum {
            Fall,
            Impact,
            GameOver
        }
        private GameStateEnum GameState;

        private const int DestroyableLength = 3;
        private Stack<Vector2> Destroyables = new Stack<Vector2>();

        public Game()
            : base(NominalWidth, NominalHeight, GraphicsMode.Default, "Impressive Solids") {
            VSync = VSyncMode.On;
            Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(OnKeyDown);
        }

        protected override void OnLoad(EventArgs E) {
            base.OnLoad(E);
            New();
        }

        private void New() {
            Rand = new Random();

            Map = new int[MapWidth, MapHeight];
            for (var X = 0; X < MapWidth; X++) {
                for (var Y = 0; Y < MapHeight; Y++) {
                    Map[X, Y] = -1;
                }
            }

            ImpactFallOffset = new float[MapWidth, MapHeight];

            StickColors = new int[StickLength];
            GenerateNextStick();
            GameState = GameStateEnum.Fall;
        }

        private void GenerateNextStick() {
            for (var i = 0; i < StickLength; i++) {
                StickColors[i] = Rand.Next(ColorsCount);
            }
            StickPosition.X = (float)Math.Floor((MapWidth - StickLength) / 2d);
            StickPosition.Y = 0;
        }
        
        protected override void OnResize(EventArgs E) {
            base.OnResize(E);
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            ProjectionWidth = NominalWidth;
            ProjectionHeight = (float)ClientRectangle.Height / (float)ClientRectangle.Width * ProjectionWidth;
            if (ProjectionHeight < NominalHeight) {
                ProjectionHeight = NominalHeight;
                ProjectionWidth = (float)ClientRectangle.Width / (float)ClientRectangle.Height * ProjectionHeight;
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs E) {
            base.OnUpdateFrame(E);

            if (GameStateEnum.Fall == GameState) {
                StickPosition.Y += FallSpeed;

                var FellOnFloor = (StickPosition.Y >= MapHeight - 1);

                var FellOnBlock = false;
                if (!FellOnFloor) {
                    var Y = (int)Math.Floor(StickPosition.Y + 1);
                    for (var i = 0; i < StickLength; i++) {
                        var X = (int)StickPosition.X + i;
                        if (Map[X, Y] >= 0) {
                            FellOnBlock = true;
                            break;
                        }
                    }
                }

                if (FellOnFloor || FellOnBlock) {
                    var Y = (int)Math.Floor(StickPosition.Y);
                    for (var i = 0; i < StickLength; i++) {
                        var X = (int)StickPosition.X + i;
                        Map[X, Y] = StickColors[i];
                    }
                    GameState = GameStateEnum.Impact;
                }
            } else if (GameStateEnum.Impact == GameState) {
                var Stabilized = true;
                for (var X = 0; X < MapWidth; X++) {
                    for (var Y = MapHeight - 2; Y >= 0; Y--) {
                        if ((Map[X, Y] >= 0) && ((Map[X, Y + 1] < 0) || (ImpactFallOffset[X, Y + 1] > 0))) {
                            Stabilized = false;
                            ImpactFallOffset[X, Y] += FallSpeed;
                            if (ImpactFallOffset[X, Y] >= 1) {
                                Map[X, Y + 1] = Map[X, Y];
                                Map[X, Y] = -1;
                                ImpactFallOffset[X, Y] = 0;
                            }
                        }
                    }
                }

                if (Stabilized) {
                    Destroyables.Clear();
                    
                    for (var X = 0; X < MapWidth; X++) {
                        for (var Y = 0; Y < MapHeight; Y++) {
                            CheckDestroyableLine(X, Y, 1, 0);
                            CheckDestroyableLine(X, Y, 0, 1);
                            CheckDestroyableLine(X, Y, 1, 1);
                            CheckDestroyableLine(X, Y, 1, -1);
                        }
                    }

                    if (Destroyables.Count > 0) {
                        foreach (var Coords in Destroyables) {
                            Map[(int)Coords.X, (int)Coords.Y] = -1;
                        }
                        Stabilized = false;
                    }
                }

                if (Stabilized) {
                    var GameOver = false;
                    for (var X = 0; X < MapWidth; X++) {
                        if (Map[X, 0] >= 0) {
                            GameOver = true;
                            break;
                        }
                    }

                    if (GameOver) {
                        GameState = GameStateEnum.GameOver;
                    } else {
                        GenerateNextStick();
                        GameState = GameStateEnum.Fall;
                    }
                }
            }
        }

        private void CheckDestroyableLine(int X1, int Y1, int DeltaX, int DeltaY) {
            if (Map[X1, Y1] < 0) {
                return;
            }

            int X2 = X1, Y2 = Y1;
            var LineLength = 0;
            while ((X2 >= 0) && (Y2 >= 0) && (X2 < MapWidth) && (Y2 < MapHeight) && (Map[X2, Y2] == Map[X1, Y1])) {
                ++LineLength;
                X2 += DeltaX;
                Y2 += DeltaY;
            }

            if (LineLength >= DestroyableLength) {
                for (var i = 0; i < LineLength; i++) {
                    Destroyables.Push(new Vector2(X1 + i * DeltaX, Y1 + i * DeltaY));
                }
            }
        }

        protected void OnKeyDown(object Sender, KeyboardKeyEventArgs E) {
            if (GameStateEnum.Fall == GameState) {
                if ((Key.Left == E.Key) && (StickPosition.X > 0)) {
                    --StickPosition.X;
                } else if ((Key.Right == E.Key) && (StickPosition.X + StickLength < MapWidth)) {
                    ++StickPosition.X;
                } else if (Key.Up == E.Key) {
                    var T = StickColors[0];
                    for (var i = 0; i < StickLength - 1; i++) {
                        StickColors[i] = StickColors[i + 1];
                    }
                    StickColors[StickLength - 1] = T;
                }
            } else if (GameStateEnum.GameOver == GameState) {
                if ((Key.Enter == E.Key) || (Key.KeypadEnter == E.Key)) {
                    New();
                }
            }
        }

        protected override void OnRenderFrame(FrameEventArgs E) {
            base.OnRenderFrame(E);

            GL.ClearColor(Color4.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var Projection = Matrix4.CreateOrthographic(-ProjectionWidth, -ProjectionHeight, -1, 1);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref Projection);
            GL.Translate(ProjectionWidth / 2, -ProjectionHeight / 2, 0);

            var Modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref Modelview);

            GL.Begin(BeginMode.Quads);

            for (var X = 0; X < MapWidth; X++) {
                for (var Y = 0; Y < MapHeight; Y++) {
                    if (Map[X, Y] >= 0) {
                        RenderSolid(X, Y + ImpactFallOffset[X, Y], Map[X, Y]);
                    }
                }
            }

            if (GameStateEnum.Fall == GameState) {
                for (var i = 0; i < StickLength; i++) {
                    RenderSolid(StickPosition.X + i, StickPosition.Y, StickColors[i]);
                }
            }

            GL.End();

            SwapBuffers();
        }

        private void RenderSolid(float X, float Y, int Color) {
            GL.Color4(Colors[Color]);
            GL.Vertex2(X * SolidSize, Y * SolidSize);
            GL.Vertex2((X + 1) * SolidSize, Y * SolidSize);
            GL.Vertex2((X + 1) * SolidSize, (Y + 1) * SolidSize);
            GL.Vertex2(X * SolidSize, (Y + 1) * SolidSize);
        }
    }
}