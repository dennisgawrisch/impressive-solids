using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
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

        private const int NominalWidth = 500;
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
        private int[] NextStickColors;

        private const float FallSpeed = 0.07f;
        private const float FastFallSpeed = 0.5f;

        private const int ColorsCount = 5;
        private Texture[] ColorTextures = new Texture[ColorsCount];

        private enum GameStateEnum {
            Fall,
            Impact,
            GameOver
        }
        private GameStateEnum GameState;

        private const int DestroyableLength = 3;
        private Stack<Vector2> Destroyables = new Stack<Vector2>();

        private Texture TextureBackground;

        private int Score;
        private int TotalDestroyedThisMove;

        private TextRenderer NextStickLabel, ScoreLabel, ScoreRenderer, HighScoreLabel, HighScoreRenderer;

        public Game()
            : base(NominalWidth, NominalHeight, GraphicsMode.Default, "Impressive Solids") {
            VSync = VSyncMode.On;
            
            Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(OnKeyDown);
            
            TextureBackground = new Texture(new Bitmap("textures/background.png"));
            for (var i = 0; i < ColorsCount; i++) {
                ColorTextures[i] = new Texture(new Bitmap("textures/solids/" + i + ".png"));
            }

            var LabelFont = new Font(new FontFamily(GenericFontFamilies.SansSerif), 20, GraphicsUnit.Pixel);
            var LabelColor = Color4.SteelBlue;
            NextStickLabel = new TextRenderer(LabelFont, LabelColor, "Next");
            ScoreLabel = new TextRenderer(LabelFont, LabelColor, "Score");
            HighScoreLabel = new TextRenderer(LabelFont, LabelColor, "High score");

            var ScoreFont = new Font(new FontFamily(GenericFontFamilies.SansSerif), 50, GraphicsUnit.Pixel);
            var ScoreColor = Color4.Tomato;
            ScoreRenderer = new TextRenderer(ScoreFont, ScoreColor);
            HighScoreRenderer = new TextRenderer(ScoreFont, ScoreColor);
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
            NextStickColors = new int[StickLength];
            GenerateNextStick();
            GenerateNextStick(); // because 1st call makes current stick all zeros
            GameState = GameStateEnum.Fall;

            Score = 0;
            TotalDestroyedThisMove = 0;
        }

        private void GenerateNextStick() {
            for (var i = 0; i < StickLength; i++) {
                StickColors[i] = NextStickColors[i];
                NextStickColors[i] = Rand.Next(ColorsCount);
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

            var CurrentFallSpeed = Keyboard[Key.Down] ? FastFallSpeed : FallSpeed;

            if (GameStateEnum.Fall == GameState) {
                StickPosition.Y += CurrentFallSpeed;

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
                            ImpactFallOffset[X, Y] += CurrentFallSpeed;
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
                        Score += (int)Math.Ceiling(Destroyables.Count + Math.Pow(1.5, Destroyables.Count - 3) - 1) + TotalDestroyedThisMove;
                        TotalDestroyedThisMove += Destroyables.Count;
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
                        TotalDestroyedThisMove = 0;
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

            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            RenderBackground();

            var PipeMargin = (ProjectionHeight - MapHeight * SolidSize) / 2f;
            GL.Translate(PipeMargin, PipeMargin, 0);

            RenderPipe();
            
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

            // HUD offset
            GL.Translate(MapWidth * SolidSize + PipeMargin, 0, 0);

            NextStickLabel.Render();
            GL.Translate(0, NextStickLabel.Height, 0);
            RenderNextStick();
            GL.Translate(0, -NextStickLabel.Height, 0);

            GL.Translate(0, MapHeight * SolidSize / 4f, 0);
            // TODO render Pause / New game button

            GL.Translate(0, MapHeight * SolidSize / 4f, 0);
            ScoreLabel.Render();
            GL.Translate(0, ScoreLabel.Height, 0);
            ScoreRenderer.Label = Score.ToString();
            ScoreRenderer.Render();
            GL.Translate(0, -ScoreLabel.Height, 0);

            GL.Translate(0, MapHeight * SolidSize / 4f, 0);
            HighScoreLabel.Render();
            GL.Translate(0, HighScoreLabel.Height, 0);
            HighScoreRenderer.Label = "100500"; // TODO
            HighScoreRenderer.Render();
            GL.Translate(0, -HighScoreLabel.Height, 0);

            SwapBuffers();
        }

        private void RenderBackground() {
            TextureBackground.Bind();
            GL.Begin(BeginMode.Quads);

            GL.TexCoord2(0, 0);
            GL.Vertex2(0, 0);

            GL.TexCoord2((float)ClientRectangle.Width / TextureBackground.Width, 0);
            GL.Vertex2(ProjectionWidth, 0);

            GL.TexCoord2((float)ClientRectangle.Width / TextureBackground.Width, (float)ClientRectangle.Height / TextureBackground.Height);
            GL.Vertex2(ProjectionWidth, ProjectionHeight);

            GL.TexCoord2(0, (float)ClientRectangle.Height / TextureBackground.Height);
            GL.Vertex2(0, ProjectionHeight);

            GL.End();
        }

        private void RenderPipe() {
            GL.Disable(EnableCap.Texture2D);
            GL.Color4(Color4.Black);

            GL.Begin(BeginMode.Quads);
            GL.Vertex2(0, 0);
            GL.Vertex2(MapWidth * SolidSize, 0);
            GL.Vertex2(MapWidth * SolidSize, MapHeight * SolidSize);
            GL.Vertex2(0, MapHeight * SolidSize);
            GL.End();

            GL.Enable(EnableCap.Texture2D);
        }

        private void RenderSolid(float X, float Y, int Color) {
            ColorTextures[Color].Bind();
            GL.Color4(Color4.White);
            GL.Begin(BeginMode.Quads);

            GL.TexCoord2(0, 0);
            GL.Vertex2(X * SolidSize, Y * SolidSize);

            GL.TexCoord2(1, 0);
            GL.Vertex2((X + 1) * SolidSize, Y * SolidSize);

            GL.TexCoord2(1, 1);
            GL.Vertex2((X + 1) * SolidSize, (Y + 1) * SolidSize);

            GL.TexCoord2(0, 1);
            GL.Vertex2(X * SolidSize, (Y + 1) * SolidSize);

            GL.End();
        }

        public void RenderNextStick() {
            GL.Disable(EnableCap.Texture2D);
            GL.Color4(Color4.Black);

            GL.Begin(BeginMode.Quads);
            GL.Vertex2(0, 0);
            GL.Vertex2(StickLength * SolidSize, 0);
            GL.Vertex2(StickLength * SolidSize, SolidSize);
            GL.Vertex2(0, SolidSize);
            GL.End();

            GL.Enable(EnableCap.Texture2D);

            for (var i = 0; i < StickLength; i++) {
                RenderSolid(i, 0, NextStickColors[i]);
            }
        }
    }
}