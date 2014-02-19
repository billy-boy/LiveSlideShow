using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Net;

using log4net;
using log4net.Config;

using matIT.Generic.Config.AppConfig;
using matIT.SlideShow.DB;
using matIT.SlideShow;
using matIT.SlideShow.ClassTypes;
using matIT.Generic.FTP;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace matIT.SlideShow.Uploader
{
    public class SUploader
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Singleton

        private static SUploader _instance;
        public static SUploader getInstance()
        {
            if (_instance == null)
                _instance = new SUploader();
            return _instance;
        }

        #endregion

        private SUploaderWorker _uploaderWorker = new SUploaderWorker();
        private BackgroundWorker _worker = new BackgroundWorker();

        #region Konstrukt & Destrukt

        public SUploader()
        {

        }

        ~SUploader()
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
            get { return ((_uploaderWorker != null && _uploaderWorker.Work)); }
        }

        /// <summary>
        /// Rückmelde-Variable
        /// </summary>
        public bool Working
        {
            get { return ((_uploaderWorker != null && _uploaderWorker.Working) && (_worker != null && _worker.IsBusy)); }
        }

        /// <summary>
        /// Status-Variable
        /// </summary>
        public int WorkedFiles
        {
            get { return _uploaderWorker.WorkedFiles; }
        }

        #endregion

        #region Steuerung

        public void start()
        {
            if (_uploaderWorker == null || (_uploaderWorker != null && !_uploaderWorker.Working) || _worker == null || (_worker != null && !_worker.IsBusy))
            {
                if (log.IsInfoEnabled) log.Info("Upload-Process initializing...");
                //Recreate the PickupWorker
                if (_uploaderWorker == null) _uploaderWorker = new SUploaderWorker();
                //Reset the PickupWorker
                _uploaderWorker.WorkedFiles = 0;
                _uploaderWorker.Work = true;
                //Recreate the BackgroundWorker
                _worker = new BackgroundWorker();
                _worker.WorkerSupportsCancellation = true;
                _worker.WorkerReportsProgress = false;
                _worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnRunWorkerCompleted);
                _worker.DoWork += new DoWorkEventHandler(_uploaderWorker.DoWork);
                if (log.IsInfoEnabled) log.Info("...Upload-Process initialized");
                if (log.IsInfoEnabled) log.Info("Upload-Process starting...");
                _worker.RunWorkerAsync();
                if (log.IsInfoEnabled) log.Info("...Upload-Process started");
            }
        }

        /// <summary>
        /// Asynchrone Stop-Methode
        /// Achtung: mnauell prüfen, wann gestoppt wurde!
        /// </summary>
        public void stop()
        {
            if (_uploaderWorker != null)
            {
                _uploaderWorker.Work = false;
                if (log.IsInfoEnabled) log.Info("Sent stop signal to upload process.");
            }
            else
            {
                if (log.IsInfoEnabled) log.Info("Upload process is not working, couldn't send stop signal.");
            }
        }

        #endregion

        #region Processing

        void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Sicherheit: Work sollte bereits false sein, wenn Prozess abegschloßen!
            if (_uploaderWorker != null) _uploaderWorker.Work = false;
        }

        #endregion

    }

    /// <summary>
    /// Work -> Abbruch-Variable von außen (Prozess läuft nur solange Work = true)
    /// Working -> Rückmelde-Variable des Prozesses ("ich arbeite noch")
    /// Worked -> Anzahl verarbeiteter Files
    /// </summary>
    public class SUploaderWorker
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //Worker-Steuerungsvariablen
        private int _worked = 0;
        private bool _work = false;
        private bool _working = false;
        //Worker-Arbeitsvariablen
        private String _remoteFolder;
        private String _remoteHost;
        private int _remotePort;
        private String _remoteUser;
        private String _remotePassword;
        private bool _useSSL;
        private int _sleepTime = 2000;
        private bool _watermark = true;
        private int _watermarkMode = 0;
        private String _chmod = "775";
        private String _importURL = String.Empty;

        public SUploaderWorker()
        {
            _working = false;
            _worked = 0;

        }

        #region Steuerung

        public void reloadConfig()
        {
            _sleepTime = Int32.Parse(MAppConfig.getInstance().getConfigValue("uploadProcess", "sleepTime", @"2000"));
            _remoteFolder = MAppConfig.getInstance().getConfigValue("uploadProcess", "remoteFolder", @"");
            _remoteHost = MAppConfig.getInstance().getConfigValue("uploadProcess", "remoteHost", @"localhost");
            _remotePort = Int32.Parse(MAppConfig.getInstance().getConfigValue("uploadProcess", "remotePort", @"21"));
            _remoteUser = MAppConfig.getInstance().getConfigValue("uploadProcess", "remoteUser", @"root");
            _remotePassword = MAppConfig.getInstance().getConfigValue("uploadProcess", "remotePassword", @"");
            _useSSL = Boolean.Parse(MAppConfig.getInstance().getConfigValue("uploadProcess", "useSSL", @"false"));
            _watermark = Boolean.Parse(MAppConfig.getInstance().getConfigValue("uploadProcess", "watermark", @"true"));
            _watermarkMode = Int32.Parse(MAppConfig.getInstance().getConfigValue("uploadProcess", "watermarkMode", @"0"));
            _chmod = MAppConfig.getInstance().getConfigValue("uploadProcess", "chmod", @"775");
            _importURL = MAppConfig.getInstance().getConfigValue("uploadProcess", "importURL", String.Empty);
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
                    doUpload();

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

        private bool sendFTPCommand(String ftpCommand)
        {
            bool success = false;
            if (log.IsDebugEnabled) log.Debug("Trying to send FTP command '"+ftpCommand+"'.");
            try
            {
                //FTP-Verbindung erstellen
                FTPFactory ftp = new FTPFactory();
                //Connection Settings
                if (log.IsDebugEnabled) log.Debug("   -> Setup connection to '"+_remoteHost+":"+_remotePort+"'.");
                ftp.setRemoteHost(_remoteHost);
                ftp.setRemotePort(_remotePort);
                //Perform SSL-Session
                if (_useSSL)
                {
                    if (log.IsDebugEnabled) log.Debug("   -> Using SSL via anonymous login and 'AUTH SSL' command.");
                    ftp.loginWithoutUser();
                    ftp.sendCommand("AUTH SSL");
                    ftp.getSslStream();
                    ftp.setUseStream(true);
                }
                //Login
                if (log.IsDebugEnabled) log.Debug("   -> Login with user '"+_remoteUser+"' and given password.");
                ftp.setRemoteUser(_remoteUser);
                ftp.setRemotePass(_remotePassword);
                ftp.login();

                //FTP-Vorabbefehle senden
                if (log.IsDebugEnabled) log.Debug("   -> Setting up binary mode.");
                ftp.setBinaryMode(true);

                //FTP-Befehl senden
                if (log.IsDebugEnabled) log.Debug("   -> Send the command.");
                ftp.sendCommand(ftpCommand);
                if (log.IsInfoEnabled) log.Info("Sent FTP command '" + ftpCommand + "' with result: '" + ftp.getReply());
                success = true;

                //FTP-Abschließen
                ftp.close();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Couldn't send FTP command '" + ftpCommand + "': " + e.Message);
            }
            return success;
        }

        /// <summary>
        /// Uploads a file via FTP
        /// Changes file attributes like configured in XML
        /// </summary>
        /// <param name="ftpFolder">Folder on remote system in which to store</param>
        /// <param name="fileName">Full file name with folder path of the local file to upload</param>
        /// <returns>Successfull</returns>
        private bool sendFTPFile(String ftpFolder, String fileName)
        {
            return sendFTPFile(ftpFolder, fileName, String.Empty);
        }
        private bool sendFTPFile(String ftpFolder, String fileName, String remoteFileName)
        {
            bool success = false;
            if (log.IsDebugEnabled) log.Debug("Trying to send FTP file '" + fileName + "'.");
            try
            {
                //FTP-Verbindung erstellen
                FTPFactory ftp = new FTPFactory();
                //Connection Settings
                if (log.IsDebugEnabled) log.Debug("   -> Setup connection to '" + _remoteHost + ":" + _remotePort + "'.");
                ftp.setRemoteHost(_remoteHost);
                ftp.setRemotePort(_remotePort);
                //Perform SSL-Session
                if (_useSSL)
                {
                    if (log.IsDebugEnabled) log.Debug("   -> Using SSL via anonymous login and 'AUTH SSL' command.");
                    ftp.loginWithoutUser();
                    ftp.sendCommand("AUTH SSL");
                    ftp.getSslStream();
                    ftp.setUseStream(true);
                }
                //Login
                if (log.IsDebugEnabled) log.Debug("   -> Login with user '" + _remoteUser + "' and given password.");
                ftp.setRemoteUser(_remoteUser);
                ftp.setRemotePass(_remotePassword);
                ftp.login();

                //FTP-Vorabbefehle senden
                if (log.IsDebugEnabled) log.Debug("   -> Setting up binary mode and change directory to '"+ftpFolder+"'.");
                ftp.setBinaryMode(true);
                ftp.chdir(ftpFolder);

                //FTP-Befehl senden
                if (log.IsDebugEnabled) log.Debug("   -> Send the file.");
                if (_useSSL)
                    ftp.uploadSecure(fileName, false);
                else
                    ftp.upload(fileName, false);
                if (log.IsInfoEnabled) log.Info("Sent FTP file '" + fileName + "' with result: '" + ftp.getReply());
                //Rename?
                if (remoteFileName != String.Empty)
                {
                    String oldFileName = fileName.Substring(fileName.LastIndexOf(@"\") + 1);
                    ftp.renameRemoteFile(oldFileName,remoteFileName);
                }

                //Needed for Upload Verification
                //long fileSize = ftp.getFileSize(fileName);
                //if (log.IsInfoEnabled) log.Info("Uploaded file has size of "+fileSize+" byte ");
                success = true;

                //FTP-Abschließen
                ftp.close();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Couldn't send FTP file '" + fileName + "': " + e.Message.Replace("\r","").Replace("\n",""));
            }
            return success;
        }

        private String formatStreamToString(Stream input)
        {
            // used to build entire input
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            string tempString = null;
            int count = 0;

            do
            {
                // fill the buffer with data
                count = input.Read(buf, 0, buf.Length);

                // make sure we read some data
                if (count != 0)
                {
                    // translate from bytes to ASCII text
                    tempString = Encoding.ASCII.GetString(buf, 0, count);

                    // continue building the string
                    sb.Append(tempString);
                }
            }
            while (count > 0); // any more data to read?
            return sb.ToString();
        }

        private void doUpload()
        {
            _working = true;

            if (log.IsDebugEnabled) log.Debug("Connecting to databse class...");
            SDB db = new SDB();
            if (log.IsDebugEnabled) log.Debug("...connected.");
            if (log.IsDebugEnabled) log.Debug("Looping through non-uploaded files in databse...");
            foreach (CImage image in db.getNonUploadedImages())
            {
                if (log.IsDebugEnabled) log.Debug("-> Checking if file exists on filesystem.");
                if (File.Exists(image.FilePath))
                {
                    try {
                        if (log.IsDebugEnabled) log.Debug("-> It exists!");
                        if (log.IsDebugEnabled) log.Debug("-> Creating temporary file name...");
                        FileInfo fi = new FileInfo(image.FilePath);
                        String tempFilePath = System.IO.Path.GetTempPath();
                        if (tempFilePath.Substring(tempFilePath.Length - 1, 1) != @"\") tempFilePath += @"\";
                        tempFilePath += fi.Name + ".tmp";
                        if (log.IsDebugEnabled) log.Debug("   -> Temp file is '"+tempFilePath+"'.");
                        if (log.IsDebugEnabled) log.Debug("-> ...temporary file name created.");
                        //Watermarken
                        if (log.IsDebugEnabled) log.Debug("-> Begin watermarking...");
                        if (_watermark && File.Exists(@"img\watermark.png"))
                        {
                            if (log.IsDebugEnabled) log.Debug("-> Load memory stream from source '"+image.FilePath+"'.");
                            MemoryStream originStream = CImage.LoadFileToMemoryStream(image.FilePath);
                            Image origin = Image.FromStream(originStream);
                            Image waterMark = Image.FromFile(@"img\watermark.png");

                             if (log.IsDebugEnabled) log.Debug("-> Detecting watermark position: '"+_watermarkMode+"'.");
                            Point watermarkPos = new Point(0,0);
                            switch (_watermarkMode)
                            {
                                case 0: watermarkPos = new Point(700, 0); break;
                                case 1: watermarkPos = new Point(origin.Width - waterMark.Width, 0); break;
                                case 2: watermarkPos = new Point(origin.Width - waterMark.Width, origin.Height - waterMark.Height); break;
                                case 3: watermarkPos = new Point(0, origin.Height - waterMark.Height); break;
                            }

                            if (log.IsDebugEnabled) log.Debug("-> Mixing watermark.");
                            Graphics gr = Graphics.FromImage(origin);
                            gr.SmoothingMode = SmoothingMode.AntiAlias & SmoothingMode.HighQuality;
                            gr.DrawImage(waterMark, watermarkPos.X, watermarkPos.Y, waterMark.Width, waterMark.Height);
                            gr.Dispose();

                            if (log.IsDebugEnabled) log.Debug("-> Saving watermarked image.");
                            EncoderParameters origEncodingParameters = new EncoderParameters(1);

                            System.Drawing.Imaging.Encoder origEncoderQuality = System.Drawing.Imaging.Encoder.Quality;
                            origEncodingParameters.Param[0] = new EncoderParameter(origEncoderQuality, 100L);
//                            System.Drawing.Imaging.Encoder origEncoderDepth = System.Drawing.Imaging.Encoder.ColorDepth;
  //                          origEncodingParameters.Param[0] = new EncoderParameter(origEncoderDepth, origin.);

                            origin.Save(tempFilePath, CImage.GetEncoder(ImageFormat.Jpeg), origEncodingParameters);

                            if (log.IsDebugEnabled) log.Debug("-> Free up some stuff.");
                            origin.Dispose();
                            origin = null;
                            originStream.Close();
                            originStream.Dispose();
                            originStream = null;
                            if (log.IsDebugEnabled) log.Debug("-> Watermarked.");
                        }
                        else
                        {
                            if (log.IsDebugEnabled) log.Debug("-> Watermarking disabled or not possible.");
                            File.Copy(image.FilePath, tempFilePath, true);
                        }
                        if (log.IsDebugEnabled) log.Debug("-> ...end watermarking.");
                        //Dateiinformationen
                        if (log.IsDebugEnabled) log.Debug("-> Read detailed file information.");
                        fi = new FileInfo(tempFilePath);
                        //Datei einlesen
                        if (log.IsDebugEnabled) log.Debug("-> Read file into stream.");
                        FileStream fs = File.OpenRead(tempFilePath);
                        byte[] buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                        fs.Close();

                        //Datei hochladen
                        if (log.IsDebugEnabled) log.Debug("-> Upload the file.");
                        String remoteFileName = fi.FullName.Substring(fi.FullName.LastIndexOf(@"\") + 1, fi.FullName.LastIndexOf(".") - (fi.FullName.LastIndexOf(@"\") + 1));
                        bool uploaded = sendFTPFile(_remoteFolder, fi.FullName, remoteFileName);
                        if (uploaded)
                        {

                            //Berechtigungen setzen!
                            if (log.IsDebugEnabled) log.Debug("-> Setting rights on remote file.");
                            //sendFTPCommand("SITE CHGRP " + _chgrp + " " + _remoteFolder + remoteFileName);
                            //sendFTPCommand("SITE CHOWN " + _chown + " " + _remoteFolder + remoteFileName);
                            sendFTPCommand("SITE CHMOD " + _chmod + " " + _remoteFolder + remoteFileName);

                            //Image als Imported deklarieren!
                            try
                            {
                                if (log.IsDebugEnabled) log.Debug("-> Setting up HTTP import request to external URL: '" + _importURL + "'.");
                                if (_importURL != String.Empty)
                                {
                                    if (log.IsDebugEnabled) log.Debug("-> Creating ASCII-Encoding of file bytes and convert them to Base64.");
                                    byte[] fileNameByte = System.Text.ASCIIEncoding.ASCII.GetBytes(fi.Name);
                                    string fileNameStr = System.Convert.ToBase64String(fileNameByte);
                                    if (log.IsDebugEnabled) log.Debug("-> Full HTTP URL is: '" + _importURL + "'.");
                                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_importURL + fileNameStr);
                                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                                    String resp = formatStreamToString(response.GetResponseStream());
                                    if (resp.IndexOf("Error") >= 0)
                                    {
                                        if (log.IsErrorEnabled) log.Error("Couldn't upload correctly. HTTP import request failed: " + resp);
                                    }
                                }
                                else
                                {
                                    if (log.IsWarnEnabled) log.Warn("HTTP import request URL is empty. Couldn't tell remote system about import.");
                                }
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled) log.Error("Couldn't upload correctly. HTTP import request failed: '" + e.Message + "'.");
                            }

                            //Image updaten
                            if (log.IsDebugEnabled) log.Debug("-> Update image in local database...");
                            image.UploadedDate = DateTime.Now;
                            image.Uploaded = true;
                            db.updateImage(image);
                            if (log.IsDebugEnabled) log.Debug("-> ...updated.");
                            //Logging
                            if (log.IsInfoEnabled) log.Info("Uploaded '" + image.FilePath + "' successfull.");
                            //Statistik
                            this._worked = this._worked + 1;
                        }
                        else
                        {
                            //Logging
                            if (log.IsErrorEnabled) log.Error("Upload of '" + image.FilePath + "' not successfull.");
                        }

                        //Temp-File löschen
                        if (log.IsDebugEnabled) log.Debug("-> Deleting temp file.");
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled) log.Error("Couldn't upload image: '" + e.Message+"'");
                    }
                }
                else
                {
                    if (log.IsErrorEnabled) log.Error("Try to upload non-existing image: '" + image.FilePath + "'.");
                }
            }
            db.Dispose();
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
