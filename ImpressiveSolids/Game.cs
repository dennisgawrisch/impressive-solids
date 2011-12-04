using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace ImpressiveSolids {
    class Game : GameWindow {
        [STAThread]
        static void Main() {
            using (var Game = new Game()) {
                Game.Run(30);
            }
        }

        public Game()
            : base(700, 500, GraphicsMode.Default, "Impressive Solids") {
            VSync = VSyncMode.On;
        }

        protected override void OnLoad(EventArgs E) {
            base.OnLoad(E);
        }

        protected override void OnResize(EventArgs E) {
            base.OnResize(E);
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
        }

        protected override void OnUpdateFrame(FrameEventArgs E) {
            base.OnUpdateFrame(E);
        }

        protected override void OnRenderFrame(FrameEventArgs E) {
            base.OnRenderFrame(E);

            GL.ClearColor(Color4.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            SwapBuffers();
        }
    }
}