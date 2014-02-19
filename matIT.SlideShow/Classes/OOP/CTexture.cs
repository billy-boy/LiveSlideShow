using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace matIT.SlideShow
{
    class CTexture
    {
        private Texture _texture;
        private int _opacity;

        public CTexture()
        {
            _opacity = 0;
        }

        public Texture Texture
        {
            get { return _texture; }
            set { _texture = value; }
        }

        public int Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }
    }
}
