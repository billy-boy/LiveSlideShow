using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using matIT.Generic.Config.AppConfig;

using log4net.Config;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = @"App.config", Watch = true)]
namespace matIT.SlideShow
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]

        static void Main()
        {
  //          frmMain frmMain = new frmMain();
              Application.Run(frmMain.getInstance());
    //        Application.Run(new frmControl());
  //          Application.Exit();
        }
    }
}
