using System;
using System.Drawing;
using System.Drawing.Text;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace ImpressiveSolids {
    class TextRenderer {
        private Font FontValue;
        private string LabelValue;
        private bool NeedToCalculateSize, NeedToRenderTexture;
        private Texture Texture;
        private int CalculatedWidth, CalculatedHeight;

        public Font Font {
            get {
                return FontValue;
            }

            set {
                FontValue = value;
                NeedToCalculateSize = true;
                NeedToRenderTexture = true;
            }
        }

        public string Label {
            get {
                return LabelValue;
            }

            set {
                if (value != LabelValue) {
                    LabelValue = value;
                    NeedToCalculateSize = true;
                    NeedToRenderTexture = true;
                }
            }
        }

        public int Width {
            get {
                if (NeedToCalculateSize) {
                    CalculateSize();
                }
                return CalculatedWidth;
            }
        }

        public int Height {
            get {
                if (NeedToCalculateSize) {
                    CalculateSize();
                }
                return CalculatedHeight;
            }
        }

        public Color4 Color = Color4.Black;

        public TextRenderer(Font Font) {
            this.Font = Font;
        }

        public TextRenderer(Font Font, Color4 Color) {
            this.Font = Font;
            this.Color = Color;
        }

        public TextRenderer(Font Font, string Label) {
            this.Font = Font;
            this.Label = Label;
        }

        public TextRenderer(Font Font, Color4 Color, string Label) {
            this.Font = Font;
            this.Color = Color;
            this.Label = Label;
        }

        private void CalculateSize() {
            using (var Bitmap = new Bitmap(1, 1)) {
                using (Graphics Graphics = Graphics.FromImage(Bitmap)) {
                    var Measures = Graphics.MeasureString(Label, Font);
                    CalculatedWidth = (int)Math.Ceiling(Measures.Width);
                    CalculatedHeight = (int)Math.Ceiling(Measures.Height);
                }
            }
            NeedToCalculateSize = false;
        }

        public void Render() {
            if ((null == Label) || ("" == Label)) {
                return;
            }

            if (NeedToRenderTexture) {
                using (var Bitmap = new Bitmap(Width, Height)) {
                    var Rectangle = new Rectangle(0, 0, Bitmap.Width, Bitmap.Height);
                    using (Graphics Graphics = Graphics.FromImage(Bitmap)) {
                        Graphics.Clear(System.Drawing.Color.Transparent);
                        Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                        Graphics.DrawString(Label, Font, Brushes.White, Rectangle);

                        if (null != Texture) {
                            Texture.Dispose();
                        }
                        Texture = new Texture(Bitmap);
                    }
                }
                NeedToRenderTexture = false;
            }

            GL.PushAttrib(AttribMask.AllAttribBits);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            GL.Enable(EnableCap.Texture2D);
            Texture.Bind();

            GL.Color4(Color);
            GL.Begin(BeginMode.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(0, 0);
            GL.TexCoord2(1, 0); GL.Vertex2(Width, 0);
            GL.TexCoord2(1, 1); GL.Vertex2(Width, Height);
            GL.TexCoord2(0, 1); GL.Vertex2(0, Height);
            GL.End();

            GL.PopAttrib();
        }
    }
}