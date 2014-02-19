using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace matIT.SlideShow
{
    class CApp
    {
        private String _appName = "mat-IT - Slideshow (for Live Events)";
        private static CApp _instance;

        public static CApp getInstance()
        {
            if (_instance == null)
                _instance = new CApp();
            return _instance;
        }

        public CApp()
        {
        }

        public String appName
        {
            get { return _appName; }
        }

    }
}
