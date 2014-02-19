using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using log4net;
using log4net.Config;

using matIT.SlideShow.Pickup;
using matIT.SlideShow.Uploader;
using matIT.Generic.Config.AppConfig;
using matIT.SlideShow.DB;
using matIT.SlideShow.Forms;
using System.IO;

namespace matIT.SlideShow
{
    public partial class frmControl : Form
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private BackgroundWorker _parent;

        public frmControl(BackgroundWorker parent)
        {
            _parent = parent;
            InitializeComponent();
            InitCancelCheckTimer();
            InitForm();
            InitData();
        }

        #region CancelAsync-Check

        private void InitCancelCheckTimer()
        {
            Timer _cancelCheckTimer = new Timer();
            _cancelCheckTimer.Interval = 5000;
            _cancelCheckTimer.Tick += new EventHandler(checkAsyncCancel);
            _cancelCheckTimer.Start();
        }

        private void checkAsyncCancel(object sender, EventArgs e)
        {
            Timer _cancelCheckTimer = sender as Timer;
            if (_parent != null && _parent.CancellationPending)
            {
                if (_cancelCheckTimer != null) _cancelCheckTimer.Stop();
                this.Close();
            }
        }

        #endregion

        /// <summary>
        /// Erstinitialiserung des Fensters
        /// </summary>
        private void InitForm()
        {
            this.Text = CApp.getInstance().appName + " - Controler";
            try
            {
                //Screen Settings + Form Position
                String configFormMonitor = MAppConfig.getInstance().getConfigValue("cfgForm", "monitor");
                String slideShowMonitor = MAppConfig.getInstance().getConfigValue("slideShow", "monitor");
                foreach (Screen screen in Screen.AllScreens)
                {
                    //Zur ComboBox hinzufügen
                    _cmbSlideShowMonitor.Items.Add(screen.DeviceName);
                    _cmbControlMonitor.Items.Add(screen.DeviceName);
                    //Wenn null dann letztbesten nehmen
                    if (configFormMonitor == null)
                    {
                        this.Left = screen.Bounds.Left + 12;
                        this.Top = screen.Bounds.Top + 12;
                    }
                    //Wenn nicht null, dann auswählen und nehmen
                    else if (configFormMonitor == screen.DeviceName)
                    {
                        this.Left = screen.Bounds.Left + 12;
                        this.Top = screen.Bounds.Top + 12;
                    }
                    //Wenn null dann letztbesten nehmen
                    if (slideShowMonitor == null)
                    {
                        _cmbSlideShowMonitor.SelectedItem = screen.DeviceName;
                    }
                    //Wenn nicht null, dann auswählen und nehmen
                    else if (slideShowMonitor == screen.DeviceName)
                    {
                        _cmbSlideShowMonitor.SelectedItem = screen.DeviceName;
                    }
                    //Wenn null dann letztbesten nehmen
                    if (configFormMonitor == null)
                    {
                        _cmbControlMonitor.SelectedItem = screen.DeviceName;
                    }
                    //Wenn nicht null, dann auswählen und nehmen
                    else if (configFormMonitor == screen.DeviceName)
                    {
                        _cmbControlMonitor.SelectedItem = screen.DeviceName;
                    }
                }
                this.StartPosition = FormStartPosition.Manual;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Keine Initialisierung des Control-Forms möglich: " + e.Message);
            }
        }

        /// <summary>
        /// Initialize CONFIG data from config file
        /// </summary>
        private void InitData()
        {
            #region Slideshow
            _sliderFadeSpeed.Value = Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "fadeTime", "25"));
            _sliderShowTime.Value = Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "showTime", "3000")) / 1000;
            _sliderFadeCount.Value = Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "fadeValue", "1"));   
            _chkSlideShowAllPictures.Checked = Boolean.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewAllPictures", "false"));
            _chkSlideshowViewAllPicturesIfNoNewOne.Checked = Boolean.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewAllPicturesIfNoNewOne", "false"));
            #endregion
            #region Upload-Process
            _txtRemoteFolder.Text = MAppConfig.getInstance().getConfigValue("uploadProcess", "remoteFolder", "");
            _txtRemoteHost.Text = MAppConfig.getInstance().getConfigValue("uploadProcess", "remoteHost", "");
            _txtRemotePort.Text = MAppConfig.getInstance().getConfigValue("uploadProcess", "remotePort", "");
            _txtRemoteUser.Text = MAppConfig.getInstance().getConfigValue("uploadProcess", "remoteUser", "");
            _txtRemotePassword.Text = MAppConfig.getInstance().getConfigValue("uploadProcess", "remotePassword", "");
            _sliderUploadSleepTime.Value = Int32.Parse(MAppConfig.getInstance().getConfigValue("uploadProcess", "sleepTime", "2000")) / 1000;
            #endregion
            #region Pickup-Process
            _txtPickupPickupFolder.Text = MAppConfig.getInstance().getConfigValue("pickupProcess", "pickupFolder", "");
            _txtPickupSaveFolder.Text = MAppConfig.getInstance().getConfigValue("pickupProcess", "saveFolder", "");
            _sliderPickupSleepTime.Value = Int32.Parse(MAppConfig.getInstance().getConfigValue("pickupProcess", "sleepTime", "1000")) / 1000;
            _chkPickupRotatePicture.Checked = Boolean.Parse(MAppConfig.getInstance().getConfigValue("pickupProcess", "rotate", "true"));
            _chkPickupDeleteCopiedFiles.Checked = Boolean.Parse(MAppConfig.getInstance().getConfigValue("pickupProcess", "deleteCopiedFiles", "false"));
            _sliderPickupWriteTimeDifference.Value = Int32.Parse(MAppConfig.getInstance().getConfigValue("pickupProcess", "writeTimeDifferenceSeconds", "10"));
            #endregion
        }

        /// <summary>
        /// Refreshs PROCESS data from running processes
        /// </summary>
        private void RefreshData()
        {
            SDB db = new SDB();

            //Pickup
            if (SPickup.getInstance().Working)
            {
                _btnPickupStart.Enabled = false;
                _btnPickupStop.Enabled = true;
                _sTSPickupStart.Enabled = false;
                _sTSPickupStop.Enabled = true;
            }
            else
            {
                _btnPickupStart.Enabled = true;
                _btnPickupStop.Enabled = false;
                _sTSPickupStart.Enabled = true;
                _sTSPickupStop.Enabled = false;
            }
            this._txtPickupProcessed.Text = ""+SPickup.getInstance().WorkedFiles;
            
            if (SPickup.getInstance().Work && SPickup.getInstance().Working) { if (File.Exists("img/green_btn_blank.png")) { _pictPickupState.Image = Image.FromFile("img/green_btn_blank.png"); } else { if (log.IsErrorEnabled) log.Error("Image-File 'img/green_btn_blank.png' not found."); } }
            else if (!SPickup.getInstance().Work && SPickup.getInstance().Working) { if (File.Exists("img/yellow_btn_blank.png")) { _pictPickupState.Image = Image.FromFile("img/yellow_btn_blank.png"); } else { if (log.IsErrorEnabled) log.Error("Image-File 'img/yellow_btn_blank.png' not found."); } }
            else { if (File.Exists("img/red_btn_blank.png")) { _pictPickupState.Image = Image.FromFile("img/red_btn_blank.png"); } else { if (log.IsErrorEnabled) log.Error("Image-File 'img/red_btn_blank.png' not found."); } }

            //Uploader
            if (SUploader.getInstance().Working)
            {
                _btnUploadStart.Enabled = false;
                _btnUploadStop.Enabled = true;
                _sTSUploadStart.Enabled = false;
                _sTSUploadStop.Enabled = true;
            }
            else
            {
                _btnUploadStart.Enabled = true;
                _btnUploadStop.Enabled = false;
                _sTSUploadStart.Enabled = true;
                _sTSUploadStop.Enabled = false;

            }

            this._txtUploadUploadedSession.Text = "" + SUploader.getInstance().WorkedFiles;
            this._txtUploadNotUploaded.Text = "" + db.getNonUploadedCount();
            this._txtUploadUploadedTotal.Text = "" + db.getUploadedCount();

            if (SUploader.getInstance().Work && SUploader.getInstance().Working) { if (File.Exists("img/green_btn_blank.png")) { _pictUploaderState.Image = Image.FromFile("img/green_btn_blank.png"); } else { if (log.IsErrorEnabled) log.Error("Image-File 'img/green_btn_blank.png' not found."); } }
            else if (!SUploader.getInstance().Work && SUploader.getInstance().Working) { if (File.Exists("img/yellow_btn_blank.png")) { _pictUploaderState.Image = Image.FromFile("img/yellow_btn_blank.png"); } else { if (log.IsErrorEnabled) log.Error("Image-File 'img/yellow_btn_blank.png' not found."); } }
            else { if (File.Exists("img/red_btn_blank.png")) { _pictUploaderState.Image = Image.FromFile("img/red_btn_blank.png"); } else { if (log.IsErrorEnabled) log.Error("Image-File 'img/red_btn_blank.png' not found."); } }

            //Slideshow
            if (frmMain.getInstance().Showing)
            {
                _btnSlideShowStart.Enabled = false;
                _btnSlideShowPause.Enabled = true;
                _btnSlideShowStop.Enabled = true;
                _sTSSlideShowStart.Enabled = false;
                _sTSSlideShowStop.Enabled = true;
            }
            else
            {
                _btnSlideShowStart.Enabled = true;
                _btnSlideShowPause.Enabled = false;
                _btnSlideShowStop.Enabled = false;
                _sTSSlideShowStart.Enabled = true;
                _sTSSlideShowStop.Enabled = false;

            }

            this._txtSlideShowImagesViewedTotal.Text = "" + db.getViewedCount();
            this._txtSlideShowNotViewed.Text = "" + db.getNonViewedCount();
            this._txtSlideShowImagesViewedSession.Text = "" + frmMain.getInstance().Showed;

            if (frmMain.getInstance().Show && frmMain.getInstance().Showing) { if (File.Exists("img/green_btn_blank.png")) { _pictSlideshowState.Image = Image.FromFile("img/green_btn_blank.png"); } else { if (log.IsErrorEnabled) log.Error("Image-File 'img/green_btn_blank.png' not found."); } }
            else if (!frmMain.getInstance().Show && frmMain.getInstance().Showing) { if (File.Exists("img/yellow_btn_blank.png")) { _pictSlideshowState.Image = Image.FromFile("img/yellow_btn_blank.png"); } else { if (log.IsErrorEnabled) log.Error("Image-File 'img/yellow_btn_blank.png' not found."); } }
            else { if (File.Exists("img/red_btn_blank.png")) { _pictSlideshowState.Image = Image.FromFile("img/red_btn_blank.png"); } else { if (log.IsErrorEnabled) log.Error("Image-File 'img/red_btn_blank.png' not found."); } }


            db.Dispose();
        }

        #region Start/Stop Events

        private void PickupStart(object sender, EventArgs e)
        {
            if (!SPickup.getInstance().Working)
                SPickup.getInstance().start();
            _btnPickupStart.Enabled = false;
            _sTSPickupStart.Enabled = false;
        }

        private void PickupStop(object sender, EventArgs e)
        {
            if (SPickup.getInstance().Working)
                SPickup.getInstance().stop();
            _btnPickupStop.Enabled = false;
            _sTSPickupStop.Enabled = false;
        }

        private void UploadStart(object sender, EventArgs e)
        {
            if (!SUploader.getInstance().Working)
                SUploader.getInstance().start();
            _btnUploadStart.Enabled = false;
            _sTSUploadStart.Enabled = false;
        }

        private void UploadStop(object sender, EventArgs e)
        {
            if (SUploader.getInstance().Working)
                SUploader.getInstance().stop();
            _btnUploadStop.Enabled = false;
            _sTSUploadStop.Enabled = false;
        }

        private void SlideShowStart(object sender, EventArgs e)
        {
            if (!frmMain.getInstance().Showing)
                frmMain.getInstance().Start();
            _btnSlideShowStart.Enabled = false;
            _sTSSlideShowStart.Enabled = false;
        }

        private void SlideShowPause(object sender, EventArgs e)
        {
            if (frmMain.getInstance().Showing)
                frmMain.getInstance().Pause();
            _btnSlideShowStart.Enabled = false;
            _sTSSlideShowStart.Enabled = false;
        }

        private void SlideShowStop(object sender, EventArgs e)
        {
            if (frmMain.getInstance().Showing)
                frmMain.getInstance().Stop();
            _btnSlideShowStart.Enabled = false;
            _sTSSlideShowStart.Enabled = false;
        }

        #endregion

        #region Save Events

        private void _btnSlideShowSave_Click(object sender, EventArgs e)
        {
            MAppConfig.getInstance().setConfigValue("slideShow", "monitor", (String)_cmbSlideShowMonitor.SelectedItem);
            MAppConfig.getInstance().setConfigValue("cfgForm", "monitor", (String)_cmbControlMonitor.SelectedItem);
            MAppConfig.getInstance().setConfigValue("slideShow", "fadeValue", ((Int32)_sliderFadeCount.Value).ToString());
            MAppConfig.getInstance().setConfigValue("slideShow", "showTime", ((Int32)_sliderShowTime.Value * 1000).ToString());
            MAppConfig.getInstance().setConfigValue("slideShow", "fadeTime", ((Int32)_sliderFadeSpeed.Value).ToString());
            MAppConfig.getInstance().setConfigValue("slideShow", "viewAllPictures", ((Boolean)_chkSlideShowAllPictures.Checked).ToString());
            MAppConfig.getInstance().setConfigValue("slideShow", "viewAllPicturesIfNoNewOne", ((Boolean)_chkSlideshowViewAllPicturesIfNoNewOne.Checked).ToString());
            InitData();
        }

        private void _btnUploadSave_Click(object sender, EventArgs e)
        {
            MAppConfig.getInstance().setConfigValue("uploadProcess", "remoteFolder", (String)this._txtRemoteFolder.Text);
            MAppConfig.getInstance().setConfigValue("uploadProcess", "remoteHost", (String)this._txtRemoteHost.Text);
            MAppConfig.getInstance().setConfigValue("uploadProcess", "remotePort", (String)this._txtRemotePort.Text);
            MAppConfig.getInstance().setConfigValue("uploadProcess", "remoteUser", (String)this._txtRemoteUser.Text);
            MAppConfig.getInstance().setConfigValue("uploadProcess", "remotePassword", (String)this._txtRemotePassword.Text);
            MAppConfig.getInstance().setConfigValue("uploadProcess", "sleepTime", ((Int32)this._sliderUploadSleepTime.Value * 1000).ToString());
            InitData();
        }

        private void _btnPickupSave_Click(object sender, EventArgs e)
        {
            MAppConfig.getInstance().setConfigValue("pickupProcess", "pickupFolder", (String)this._txtPickupPickupFolder.Text);
            MAppConfig.getInstance().setConfigValue("pickupProcess", "saveFolder", (String)this._txtPickupSaveFolder.Text);
            MAppConfig.getInstance().setConfigValue("pickupProcess", "sleepTime", ((Int32)this._sliderPickupSleepTime.Value * 1000).ToString());
            MAppConfig.getInstance().setConfigValue("pickupProcess", "rotate", (String)this._chkPickupRotatePicture.Checked.ToString());
            MAppConfig.getInstance().setConfigValue("pickupProcess", "deleteCopiedFiles", (String)this._chkPickupDeleteCopiedFiles.Checked.ToString());
            MAppConfig.getInstance().setConfigValue("pickupProcess", "writeTimeDifferenceSeconds", ((Int32)this._sliderPickupWriteTimeDifference.Value).ToString());
            InitData();
        }

        #endregion

        #region UI Handler

        private void _timerRefresh_Tick(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void infoToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            frmInfo info = new frmInfo();
            info.ShowDialog();
        }

        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void _btnPickupPickupFolderSelect_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog objDialog = new FolderBrowserDialog();
            objDialog.Description = "Auswahl des Pickup-Folder";
            objDialog.SelectedPath = _txtPickupPickupFolder.Text; // Vorgabe Pfad (und danach der gewählte Pfad)
            DialogResult objResult = objDialog.ShowDialog(this);
            if (objResult == DialogResult.OK)
                _txtPickupPickupFolder.Text = objDialog.SelectedPath;
        }

        private void _btnPickupSaveFolderSelect_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog objDialog = new FolderBrowserDialog();
            objDialog.Description = "Auswahl des Save-Folder";
            objDialog.SelectedPath = _txtPickupSaveFolder.Text; // Vorgabe Pfad (und danach der gewählte Pfad)
            DialogResult objResult = objDialog.ShowDialog(this);
            if (objResult == DialogResult.OK)
                _txtPickupSaveFolder.Text = objDialog.SelectedPath;
        }

        #endregion

    }
}
