namespace matIT.SlideShow.Forms
{
    partial class frmInfo
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmInfo));
            this._rtbInfo = new System.Windows.Forms.RichTextBox();
            this._btnClose = new System.Windows.Forms.Button();
            this._pictLogo = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this._pictLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // _rtbInfo
            // 
            this._rtbInfo.BackColor = System.Drawing.SystemColors.Control;
            this._rtbInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._rtbInfo.Location = new System.Drawing.Point(12, 118);
            this._rtbInfo.Name = "_rtbInfo";
            this._rtbInfo.Size = new System.Drawing.Size(438, 150);
            this._rtbInfo.TabIndex = 0;
            this._rtbInfo.Text = resources.GetString("_rtbInfo.Text");
            // 
            // _btnClose
            // 
            this._btnClose.BackColor = System.Drawing.SystemColors.ControlLight;
            this._btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClose.Location = new System.Drawing.Point(186, 274);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(75, 23);
            this._btnClose.TabIndex = 1;
            this._btnClose.Text = "Close";
            this._btnClose.UseVisualStyleBackColor = false;
            this._btnClose.Click += new System.EventHandler(this._btnClose_Click);
            // 
            // _pictLogo
            // 
            this._pictLogo.Image = global::matIT.SlideShow.Properties.Resources.m__100;
            this._pictLogo.Location = new System.Drawing.Point(174, 12);
            this._pictLogo.Name = "_pictLogo";
            this._pictLogo.Size = new System.Drawing.Size(100, 100);
            this._pictLogo.TabIndex = 2;
            this._pictLogo.TabStop = false;
            // 
            // frmInfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 310);
            this.Controls.Add(this._pictLogo);
            this.Controls.Add(this._btnClose);
            this.Controls.Add(this._rtbInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmInfo";
            this.Text = "frmInfo";
            ((System.ComponentModel.ISupportInitialize)(this._pictLogo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox _rtbInfo;
        private System.Windows.Forms.Button _btnClose;
        private System.Windows.Forms.PictureBox _pictLogo;
    }
}