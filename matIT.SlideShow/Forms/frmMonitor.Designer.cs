namespace matIT.SlideShow.Forms
{
    partial class frmMonitor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMonitor));
            this._ltbMonitor = new System.Windows.Forms.ListBox();
            this._btnToggleAutoScroll = new System.Windows.Forms.Button();
            this._lblLevel = new System.Windows.Forms.Label();
            this._cmbLevel = new System.Windows.Forms.ComboBox();
            this._lblFacility = new System.Windows.Forms.Label();
            this._cmbFacility = new System.Windows.Forms.ComboBox();
            this._txtBacklog = new System.Windows.Forms.TextBox();
            this._lblBacklog = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _ltbMonitor
            // 
            this._ltbMonitor.FormattingEnabled = true;
            this._ltbMonitor.Location = new System.Drawing.Point(0, 0);
            this._ltbMonitor.Name = "_ltbMonitor";
            this._ltbMonitor.Size = new System.Drawing.Size(752, 381);
            this._ltbMonitor.TabIndex = 1;
            // 
            // _btnToggleAutoScroll
            // 
            this._btnToggleAutoScroll.Location = new System.Drawing.Point(12, 411);
            this._btnToggleAutoScroll.Name = "_btnToggleAutoScroll";
            this._btnToggleAutoScroll.Size = new System.Drawing.Size(127, 23);
            this._btnToggleAutoScroll.TabIndex = 2;
            this._btnToggleAutoScroll.Text = "Disable Auto-Scroll";
            this._btnToggleAutoScroll.UseVisualStyleBackColor = true;
            this._btnToggleAutoScroll.Click += new System.EventHandler(this._btnToggleAutoScroll_Click);
            // 
            // _lblLevel
            // 
            this._lblLevel.AutoSize = true;
            this._lblLevel.Location = new System.Drawing.Point(145, 389);
            this._lblLevel.Name = "_lblLevel";
            this._lblLevel.Size = new System.Drawing.Size(77, 13);
            this._lblLevel.TabIndex = 3;
            this._lblLevel.Text = "Logging-Level:";
            // 
            // _cmbLevel
            // 
            this._cmbLevel.FormattingEnabled = true;
            this._cmbLevel.Items.AddRange(new object[] {
            "OFF",
            "FATAL",
            "ERROR",
            "WARN",
            "INFO",
            "DEBUG",
            "ALL"});
            this._cmbLevel.Location = new System.Drawing.Point(234, 386);
            this._cmbLevel.Name = "_cmbLevel";
            this._cmbLevel.Size = new System.Drawing.Size(121, 21);
            this._cmbLevel.TabIndex = 4;
            this._cmbLevel.Text = "DEBUG";
            this._cmbLevel.SelectedIndexChanged += new System.EventHandler(this._cmbLevel_SelectedIndexChanged);
            // 
            // _lblFacility
            // 
            this._lblFacility.AutoSize = true;
            this._lblFacility.Location = new System.Drawing.Point(145, 416);
            this._lblFacility.Name = "_lblFacility";
            this._lblFacility.Size = new System.Drawing.Size(83, 13);
            this._lblFacility.TabIndex = 5;
            this._lblFacility.Text = "Logging-Facility:";
            // 
            // _cmbFacility
            // 
            this._cmbFacility.FormattingEnabled = true;
            this._cmbFacility.Location = new System.Drawing.Point(234, 413);
            this._cmbFacility.Name = "_cmbFacility";
            this._cmbFacility.Size = new System.Drawing.Size(506, 21);
            this._cmbFacility.TabIndex = 6;
            // 
            // _txtBacklog
            // 
            this._txtBacklog.Location = new System.Drawing.Point(419, 386);
            this._txtBacklog.Name = "_txtBacklog";
            this._txtBacklog.ReadOnly = true;
            this._txtBacklog.Size = new System.Drawing.Size(88, 20);
            this._txtBacklog.TabIndex = 7;
            // 
            // _lblBacklog
            // 
            this._lblBacklog.AutoSize = true;
            this._lblBacklog.Location = new System.Drawing.Point(364, 390);
            this._lblBacklog.Name = "_lblBacklog";
            this._lblBacklog.Size = new System.Drawing.Size(49, 13);
            this._lblBacklog.TabIndex = 8;
            this._lblBacklog.Text = "Backlog:";
            // 
            // frmMonitor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(752, 441);
            this.Controls.Add(this._lblBacklog);
            this.Controls.Add(this._txtBacklog);
            this.Controls.Add(this._cmbFacility);
            this.Controls.Add(this._lblFacility);
            this.Controls.Add(this._cmbLevel);
            this.Controls.Add(this._lblLevel);
            this.Controls.Add(this._btnToggleAutoScroll);
            this.Controls.Add(this._ltbMonitor);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmMonitor";
            this.Text = "Monitoring";
            this.ResizeEnd += new System.EventHandler(this.frmMonitor_ResizeEnd);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox _ltbMonitor;
        private System.Windows.Forms.Button _btnToggleAutoScroll;
        private System.Windows.Forms.Label _lblLevel;
        private System.Windows.Forms.ComboBox _cmbLevel;
        private System.Windows.Forms.Label _lblFacility;
        private System.Windows.Forms.ComboBox _cmbFacility;
        private System.Windows.Forms.TextBox _txtBacklog;
        private System.Windows.Forms.Label _lblBacklog;


    }
}