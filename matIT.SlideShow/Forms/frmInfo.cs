using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace matIT.SlideShow.Forms
{
    public partial class frmInfo : Form
    {
        public frmInfo()
        {
            InitializeComponent();
            this.Text = CApp.getInstance().appName + " - Info";
        }

        private void _btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
