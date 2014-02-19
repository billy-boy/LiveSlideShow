using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Drawing;

using log4net;
using log4net.Config;
using Goheer.EXIF;

using matIT.Generic;
using matIT.Generic.Config.AppConfig;
using matIT.SlideShow.DB;
using matIT.SlideShow;
using matIT.SlideShow.ClassTypes;

namespace matIT.SlideShow.Pickup
{
    public class SPickup
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Singleton

        private static SPickup _instance;
        public static SPickup getInstance()
        {
            if (_instance == null)
                _instance = new SPickup();
            return _instance;
        }

        #endregion
       
        private BackgroundWorker _worker = new BackgroundWorker();
        private SPickupWorker _pickupWorker = new SPickupWorker();

        #region Konstrukt & Destrukt

        public SPickup()
        {

        }

        ~SPickup()
        {
            stop();
            //Wait for exit
            DateTime waitVal = DateTime.Now;
            while (Working && DateTime.Now.Subtract(waitVal).TotalSeconds <= 5)
            {
                stop();
            }
            try
            {
                if (_worker != null)
                {
                    if (_worker.WorkerSupportsCancellation) _worker.CancelAsync();
                }
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Couldn't destruct this class fully: " + ex.Message);
            }
        }

        #endregion

        #region Getter/Setter

        /// <summary>
        /// Abbruch-Variable
        /// </summary>
        public bool Work
        {
            get { return ((_pickupWorker != null && _pickupWorker.Work)); }
        }

        /// <summary>
        /// Rückmelde-Variable
        /// </summary>
        public bool Working
        {
            get { return ((_pickupWorker != null && _pickupWorker.Working) && (_worker != null && _worker.IsBusy)); }
        }

        /// <summary>
        /// Status-Variable
        /// </summary>
        public int WorkedFiles
        {
            get { return _pickupWorker.WorkedFiles; }
        }

        #endregion

        #region Steuerung

        public void start()
        {          
            if (_pickupWorker == null || (_pickupWorker != null && !_pickupWorker.Working) || _worker == null || (_worker != null && !_worker.IsBusy))
            {
                if (log.IsInfoEnabled) log.Info("Pickup-Process initializing...");
                //Recreate the PickupWorker
                if (_pickupWorker == null) _pickupWorker = new SPickupWorker();
                //Reset the PickupWorker
                _pickupWorker.WorkedFiles = 0;
                _pickupWorker.Work = true;
                //Recreate the BackgroundWorker
                _worker = new BackgroundWorker();
                _worker.WorkerSupportsCancellation = true;
                _worker.WorkerReportsProgress = false;
                _worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnRunWorkerCompleted);
                _worker.DoWork += new DoWorkEventHandler(_pickupWorker.DoWork);
                if (log.IsInfoEnabled) log.Info("...Pickup-Process initialized");
                if (log.IsInfoEnabled) log.Info("Pickup-Process starting...");
                _worker.RunWorkerAsync();
                if (log.IsInfoEnabled) log.Info("...Pickup-Process started");
            }
        }

        /// <summary>
        /// Asynchrone Stop-Methode
        /// Achtung: mnauell prüfen, wann gestoppt wurde!
        /// </summary>
        public void stop()
        {
            if (_pickupWorker != null)
            {
                _pickupWorker.Work = false;
                if (log.IsInfoEnabled) log.Info("Sent stop signal to pickup process.");
            }
            else
            {
                if (log.IsInfoEnabled) log.Info("Pickup process is not working, couldn't send stop signal.");
            }
        }

        #endregion

        #region Processing

        void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Sicherheit: Work sollte bereits false sein, wenn Prozess abegschloßen!
            if (_pickupWorker != null) _pickupWorker.Work = false;
        }

        #endregion
    }

    /// <summary>
    /// Work -> Abbruch-Variable von außen (Prozess läuft nur solange Work = true)
    /// Working -> Rückmelde-Variable des Prozesses ("ich arbeite noch")
    /// Worked -> Anzahl verarbeiteter Files
    /// </summary>
    public class SPickupWorker
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private bool _work = false;
        private int _worked = 0;
        private bool _working = false;

        private String _pickupFolder;
        private String _saveFolder;
        private bool _rotate = false;
        private int _writeTimeDifferenceSeconds = 10;
        private int _sleepTime = 2000;
        private bool _deleteCopiedFiles = false;
        private List<string> _validExtensions = new List<string>();

        public SPickupWorker()
        {
            _working = false;
            _worked = 0;
        }

        #region Steuerung

        public void reloadConfig()
        {
            _sleepTime = Int32.Parse(MAppConfig.getInstance().getConfigValue("pickupProcess", "sleepTime", @"2000"));
            _pickupFolder = MAppConfig.getInstance().getConfigValue("pickupProcess", "pickupFolder", @"C:\");
            _saveFolder = MAppConfig.getInstance().getConfigValue("pickupProcess", "saveFolder", @"C:\");
            _rotate = Boolean.Parse(MAppConfig.getInstance().getConfigValue("pickupProcess", "rotate", "false"));
            _validExtensions = MAppConfig.getInstance().getConfigValues("pickupProcess", "validExtension");
            _deleteCopiedFiles = Boolean.Parse(MAppConfig.getInstance().getConfigValue("pickupProcess", "deleteCopiedFiles", "false"));
            _writeTimeDifferenceSeconds = Int32.Parse(MAppConfig.getInstance().getConfigValue("pickupProcess", "writeTimeDifferenceSeconds", @"10"));
        }

        #endregion

        #region Processing

        public void DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                //Working enabled?
                while (_work)
                {
                    //Reload the Config
                    reloadConfig();

                    //Start the Pickup
                    doPickup();

                    //Cleanup memory
                    GC.Collect();

                    //Sleeping
                    Thread.Sleep(_sleepTime);
                }
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled) log.Error("Error while doing background work: " + ex.Message);
            }
            _working = false;
        }

        private void doPickup()
        {
            //Set Rückgabe-Variable
            _working = true;

            //Loop through all files
            if (log.IsDebugEnabled) log.Debug("Looping through files in '" + _pickupFolder + "'...");
            foreach (String fileName in Directory.GetFiles(_pickupFolder))
            {
                //Determining FileInfo
                FileInfo file = new FileInfo(fileName);
                //Check files extension
                if (log.IsDebugEnabled) log.Debug("-> Checking if lower-case file extension is in valid extensions list: '" + file.Extension.ToLower() + "'.");
                if (_validExtensions.Contains(file.Extension.ToLower()))
                {
                    if (log.IsDebugEnabled) log.Debug("   -> It is valid.");



                    //File allready saved in new directory
                    if (log.IsDebugEnabled) log.Debug("-> Checking if file '" + fileName + "' exists in target folder '" + _saveFolder + "'.");
                    if (!File.Exists(_saveFolder + Path.DirectorySeparatorChar + file.Name))
                    {
                        if (log.IsDebugEnabled) log.Debug("   -> It does not exist.");

                        //Check if file is written for longer than 10 seconds
                        if (log.IsDebugEnabled) log.Debug("-> Checking if file is written for longer than '" + _writeTimeDifferenceSeconds + "' seconds ago (because of SMB-File-Writing-Problem).");
                        Double WriteTimeDifferenceSeconds = DateTime.Now.Subtract(file.LastWriteTime).TotalSeconds;
                        if (log.IsDebugEnabled) log.Debug("   -> " + WriteTimeDifferenceSeconds.ToString() + " s");
                        if (WriteTimeDifferenceSeconds >= _writeTimeDifferenceSeconds)
                        {
                            if (log.IsDebugEnabled) log.Debug("   -> It is.");
                            //Try to modify and save it
                            try
                            {
                                //Get Database-Connection
                                SDB db = new SDB();
                                //Crete an image from the file
                                Bitmap bmp = new Bitmap(fileName);
                                //Create exif info from image
                                EXIFextractor exif = new EXIFextractor(ref bmp, @"\n");
                                //Debug output exif-data
                                if (log.IsDebugEnabled)
                                {
                                    foreach (System.Web.UI.Pair exifPair in exif)
                                    {
                                        if (exifPair != null && exifPair.First != null && exifPair.Second != null) log.Debug("EXIF-Meta '" + exifPair.First + "' -> '" + exifPair.Second + "'");
                                    }
                                }
                                //Create rotation infos
                                RotateFlipType flip = RotateFlipType.RotateNoneFlipNone;
                                if (exif["Orientation"] != null)
                                {
                                    flip = CImage.OrientationToFlipType(exif["Orientation"].ToString());
                                }
                                //Rotation needed?
                                if (log.IsDebugEnabled) log.Debug("-> Checking if file must be rotated.");
                                if (_rotate && flip != RotateFlipType.RotateNoneFlipNone)
                                {
                                    if (log.IsDebugEnabled) log.Debug("   -> It must.");
                                    bmp.RotateFlip(flip);
                                    exif.setTag(0x112, "1"); // Optional: reset orientation tag
                                    bmp.Save(_saveFolder + Path.DirectorySeparatorChar + file.Name, System.Drawing.Imaging.ImageFormat.Jpeg);

                                    if (log.IsInfoEnabled) log.Info("-> Rotated and picked up '" + fileName + "'");
                                }
                                else
                                {
                                    if (log.IsDebugEnabled) log.Debug("   -> It's not needed.");
                                    //Copy the file from source to target
                                    File.Copy(fileName, _saveFolder + Path.DirectorySeparatorChar + file.Name);
                                    if (log.IsInfoEnabled) log.Info("-> Picked up '" + fileName + "'");
                                }
                                bmp.Dispose();

                                //Delete the file from the source
                                if (_deleteCopiedFiles)
                                {
                                    if (log.IsDebugEnabled) log.Debug("-> Trying to delete source file...");
                                    File.Delete(fileName);
                                    if (!File.Exists(fileName))
                                    {
                                        if (log.IsDebugEnabled) log.Debug("   -> Successfull.");
                                        if (log.IsInfoEnabled) log.Info("-> Deleted source file '" + fileName + "'.");
                                    }
                                    else
                                    {
                                        if (log.IsDebugEnabled) log.Debug("   -> Not successfull.");
                                        if (log.IsInfoEnabled) log.Info("-> Deleted source file '" + fileName + "' has failed.");
                                    }
                                }
                                else { if (log.IsInfoEnabled) log.Info("-> Delete of source file is disabled."); }

                                //Creates an CImage from the source
                                CImage image = new CImage();
                                image.FilePath = _saveFolder + Path.DirectorySeparatorChar + file.Name;
                                //Insert that CImage to sDB
                                db.insertImage(image);
                                db.Dispose();
                                //Write finish message
                                if (log.IsInfoEnabled) log.Info("Picked-up '" + image.FilePath + "' and saved successfull.");
                                //Count processed files
                                _worked++;
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled) log.Error("Error: " + e.Message);
                            }
                        }
                    }
                    else
                    {
                        if (log.IsDebugEnabled) log.Debug("   -> It does exist. Aborting.");
                    }
                }
                else
                {
                    if (log.IsDebugEnabled) log.Debug("   -> It is not valid. Aborting.");
                }
            }
        }

        #endregion

        #region Getter/Setter

        /// <summary>
        /// Abbruch-Variable
        /// </summary>
        public bool Work
        {
            get { return _work; }
            set { _work = value; }
        }

        /// <summary>
        /// Rückmelde-Variable
        /// </summary>
        public bool Working
        {
            get { return _working; }
        }

        /// <summary>
        /// Status-Variable
        /// </summary>
        public int WorkedFiles
        {
            get { return _worked; }
            set { _worked = value; }
        }
        
        #endregion
    }
}
