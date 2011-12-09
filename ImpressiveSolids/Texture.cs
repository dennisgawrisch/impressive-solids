using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL;

namespace ImpressiveSolids {
    public class Texture : IDisposable {
        public int GlHandle { get; protected set; }

        public int Width { get; protected set; }
        public int Height { get; protected set; }

        #region NPOT

        private static bool? CalculatedSupportForNpot;
        public static bool NpotIsSupported {
            get {
                if (!CalculatedSupportForNpot.HasValue) {
                    CalculatedSupportForNpot = false;
                    int ExtensionsCount;
                    GL.GetInteger(GetPName.NumExtensions, out ExtensionsCount);
                    for (var i = 0; i < ExtensionsCount; i++) {
                        if ("GL_ARB_texture_non_power_of_two" == GL.GetString(StringName.Extensions, i)) {
                            CalculatedSupportForNpot = true;
                            break;
                        }
                    }
                }
                return CalculatedSupportForNpot.Value;
            }
        }

        public int PotWidth {
            get {
                return NpotIsSupported ? Width : (int)Math.Pow(2, Math.Ceiling(Math.Log(Width, 2)));
            }
        }
        public int PotHeight {
            get {
                return NpotIsSupported ? Height : (int)Math.Pow(2, Math.Ceiling(Math.Log(Height, 2)));
            }
        }

        #endregion

        public Texture(Bitmap Bitmap) {
            GlHandle = GL.GenTexture();
            Bind();

            Width = Bitmap.Width;
            Height = Bitmap.Height;

            var BitmapData = Bitmap.LockBits(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, PotWidth, PotHeight, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, BitmapData.Width, BitmapData.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, BitmapData.Scan0);
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