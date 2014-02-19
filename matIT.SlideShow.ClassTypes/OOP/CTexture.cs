using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Microsoft.DirectX.Direct3D;

namespace matIT.SlideShow.ClassTypes
{
    public class CTexture : IDisposable
    {
        private bool isDisposed = false;

        private CImage _image;
        private Bitmap _picture;
        private Texture _texture;
        private int _opacity;

        public CTexture()
        {
            _opacity = 0;
        }
        ~CTexture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                //Managed stuff
            }
            //Unmanaged stuff
            if (_picture != null)
                _picture.Dispose();
            _picture = null;
            if (_texture != null)
                _texture.Dispose();
            _texture = null;
            _image = null;
            //Save
            isDisposed = true;
        }

        public bool Disposed
        {
            get { return isDisposed; }
        }

        public Bitmap Picture
        {
            get { return _picture; }
            set { _picture = value; }
        }

        public Texture Texture
        {
            get { return _texture; }
            set { _texture = value; }
        }

        public CImage Image
        {
            get { return _image; }
            set { _image = value; }
        }

        public int Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }
    }
}
