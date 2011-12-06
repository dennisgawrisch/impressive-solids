using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL;

namespace ImpressiveSolids {
    public class Texture : IDisposable {
        public int GlHandle { get; protected set; }
        public int Width { get; protected set; }
        public int Height { get; protected set; }

        public Texture(Bitmap Bitmap) {
            GlHandle = GL.GenTexture();
            Bind();

            Width = Bitmap.Width;
            Height = Bitmap.Width;

            var BitmapData = Bitmap.LockBits(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, BitmapData.Width, BitmapData.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, BitmapData.Scan0);
            Bitmap.UnlockBits(BitmapData);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }

        public void Bind() {
            GL.BindTexture(TextureTarget.Texture2D, GlHandle);
        }

        #region Disposable

        private bool Disposed = false;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool Disposing) {
            if (!Disposed) {
                if (Disposing) {
                    GL.DeleteTexture(GlHandle);
                }
                Disposed = true;
            }
        }

        ~Texture() {
            Dispose(false);
        }

        #endregion
    }
}