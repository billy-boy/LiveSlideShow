using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using log4net;
using log4net.Config;

using matIT.Generic;
using matIT.Generic.Config.AppConfig;
using matIT.SlideShow.ClassTypes;
using matIT.SlideShow.DB;
using matIT.SlideShow.Forms;

namespace matIT.SlideShow
{
    public partial class frmMain : Form
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Singleton

        private static frmMain _instance;
        public static frmMain getInstance()
        {
            if (_instance == null)
                _instance = new frmMain();
            return _instance;
        }

        #endregion

        //D3D Device
        private Device _d3dDdevice = null;
        //Textures
        private CTexture _frontTexture;
        private CTexture _backTexture;
        private Texture _background;
        //Showing?
        private bool _stop = false;
        private bool _show = true;
        private bool _showing = false;
        private int _showed = 0;
        //Controling
        BackgroundWorker _controlThread;
        //BackgroundWorker _monitorThread;
        //frmControl _controlFrm;
        //frmMonitor _monitorFrm;
        //Helper for KioskMode
        Int32 _lastViewedLastPickedUpImageId = -1;

        public frmMain()
        {
            InitializeComponent();
            InitForm();
            InitDirect3D();

            // Open control window
            InitControl();
            InitMonitor();

            // Remove the cursor
            //this.Cursor.Dispose();
        }
        ~frmMain()
        {
            killControl();
            killMonitor();
        }

        #region Image Handling

        private Point getPositionPoint(Size imageSize, Size availSize)
        {
            Point ret = new Point(0, 0);
            if (imageSize.Width < availSize.Width)
                ret.X = (Int32)(((Double)availSize.Width - imageSize.Width) / 2.0);
            if (imageSize.Height < availSize.Height)
                ret.Y = (Int32)(((Double)availSize.Height - imageSize.Height) / 2.0);
            return ret;
        }

        private Size getSizeByRatio(Size originalSize, Size availSize)
        {
            Size ret = availSize;
            if (originalSize.Width > availSize.Width || originalSize.Height > availSize.Height)
            {
            }

            //Querformat
            if (originalSize.Width > originalSize.Height)
                ret.Height = (Int32)(((Double)originalSize.Height / originalSize.Width) * availSize.Width);
            //Hochformat
            else
                ret.Width = (Int32)(((Double)originalSize.Width / originalSize.Height) * availSize.Height);

            //Unschöne Ratios
            if (ret.Height > availSize.Height)
            {
                ret.Height = availSize.Height;
                ret.Width = (Int32)(((Double)originalSize.Width / originalSize.Height) * availSize.Height);
            }
            if (ret.Width > availSize.Width)
            {
                ret.Width = availSize.Width;
                ret.Height = (Int32)(((Double)originalSize.Height / originalSize.Width) * availSize.Width);
            }

            return ret;
        }

        private Bitmap reSize(Image img, Size newSize)
        {
            //Größe errechnen
            newSize = getSizeByRatio(img.Size, newSize);
            //Neue Bitmap erstellen (Breite, Höhe, Bittiefe, Auflösung)
            Bitmap ret = new Bitmap(newSize.Width, newSize.Height, PixelFormat.Format32bppRgb);
            ret.SetResolution(img.HorizontalResolution, img.VerticalResolution);

            //Resize via Graphics into Bitmap
            Graphics grPhoto = Graphics.FromImage(ret);
            grPhoto.Clear(Color.Black);
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

            //Resize
            grPhoto.DrawImage(img,
                new Rectangle(0, 0, newSize.Width, newSize.Height),
                new Rectangle(0, 0, img.Width, img.Height),
                GraphicsUnit.Pixel);

            //Aufräumen & Return
            grPhoto.Dispose();
            return ret;
        }

        #endregion

        private void DrawImages()
        {
            if (log.IsDebugEnabled) log.Debug("Start drawing front and back texture...");

            //Check input
            if (_frontTexture != null && _frontTexture.Opacity < 0)
                _frontTexture.Opacity = 0;
            if (_backTexture != null && _backTexture.Opacity > 255)
                _backTexture.Opacity = 255;
            //Clear Direct3D Device
            //_d3dDdevice.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
            _d3dDdevice.BeginScene();

            using (Sprite spriteobject = new Sprite(_d3dDdevice))
            {
                //Prepare Sprite
                spriteobject.Begin(SpriteFlags.AlphaBlend | SpriteFlags.SortDepthBackToFront);
                Color color;
                Point position;

                //
                // Draw FRONT-Texture
                //

                if (_frontTexture != null && !_frontTexture.Disposed)
                {
                    //Alpha-Value
                    color = Color.FromArgb((int)_frontTexture.Opacity, (int)_frontTexture.Opacity, (int)_frontTexture.Opacity, (int)_frontTexture.Opacity);
                    //Detect postioning
                    position = getPositionPoint(_frontTexture.Picture.Size, this._panel.Size);
                    //Draw
                    spriteobject.Draw2D(_frontTexture.Texture, new Point(0, 0), 0f, position, color);
                }
                else
                {
                    if (log.IsWarnEnabled) log.Warn("Front Texture is null or disposed and should be rendered (this is normal during first fade after startup)...");
                }

                //
                // Draw BACK-Texture
                //

                if (_backTexture != null && !_backTexture.Disposed)
                {
                    //Alpha-Value
                    color = Color.FromArgb((int)_backTexture.Opacity, (int)_backTexture.Opacity, (int)_backTexture.Opacity, (int)_backTexture.Opacity);
                    //Detect postioning
                    position = getPositionPoint(_backTexture.Picture.Size, this._panel.Size);
                    //Draw
                    spriteobject.Draw2D(_backTexture.Texture, new Point(0, 0), 0f, position, color);
                }
                else
                {
                    if (log.IsWarnEnabled) log.Warn("Back Texture is null or disposed and should be rendered...");
                }
                    
                //End Sprite
                spriteobject.End();
            }
            //Finish GFX
            _d3dDdevice.EndScene();
            _d3dDdevice.Present();

            if (log.IsDebugEnabled) log.Debug("...finished drawing front and back texture.");
        }

        private void closeSubForms()
        {
            if (log.IsDebugEnabled) log.Debug("Start subform cancelation (1x 10sec).");
            DateTime startCancelation;
            //Kill Monitor Thread
            //startCancelation = DateTime.Now;
            //while (_monitorThread != null && _monitorThread.IsBusy && DateTime.Now.Subtract(startCancelation).TotalSeconds <= 10)
            //{
            //    if (log.IsDebugEnabled) log.Debug("Send cancel request to monitoring form...");
            //    killMonitor();
            //    Thread.Sleep(1000);
            //}
            //Kill Control Thread
            startCancelation = DateTime.Now;
            while (_controlThread != null && _controlThread.IsBusy && DateTime.Now.Subtract(startCancelation).TotalSeconds <= 10)
            {
                if (log.IsDebugEnabled) log.Debug("Send cancel request to control form...");
                killControl();
                Thread.Sleep(1000);
            }
        }

        #region Control-Form

        private void InitControl()
        {
            if (log.IsInfoEnabled) log.Info("Initializing Control-Form in separate thread...");
            _controlThread = new BackgroundWorker();
            _controlThread.DoWork += new DoWorkEventHandler(showControl);
            _controlThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(closedControl);
            _controlThread.WorkerReportsProgress = false;
            _controlThread.WorkerSupportsCancellation = true;
            _controlThread.RunWorkerAsync();
            if (log.IsInfoEnabled) log.Info("...initializing finished.");
        }

        private void closedControl(object sender, RunWorkerCompletedEventArgs e)
        {
            if (log.IsInfoEnabled) log.Info("Control-Form thread terminated.");
            closeSubForms();
            this.Close();
        }

        //Used from second Thread!
        private void showControl(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker parent = sender as BackgroundWorker;
            if (parent != null)
            {
                if (log.IsInfoEnabled) log.Info("Control-Form thread started.");
                frmControl _controlFrm = new frmControl(parent);
                _controlFrm.ShowDialog();
                if (log.IsInfoEnabled) log.Info("Control-Form thread finished.");  
            }
        }

        private void killControl()
        {
            if (_controlThread != null && _controlThread.IsBusy)
                _controlThread.CancelAsync();
        }

        #endregion

        #region Monitor-Form

        private void InitMonitor()
        {
            if (log.IsInfoEnabled) log.Info("Initializing Monitor-Form in separate thread...");
            //_monitorThread = new BackgroundWorker();
            //_monitorThread.DoWork += new DoWorkEventHandler(showMonitor);
            //_monitorThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(closedMonitor);
            //_monitorThread.WorkerReportsProgress = false;
            //_monitorThread.WorkerSupportsCancellation = true;
            //_monitorThread.RunWorkerAsync();
            if (log.IsInfoEnabled) log.Info("...initializing finished.");
        }

        private void closedMonitor(object sender, RunWorkerCompletedEventArgs e)
        {
            if (log.IsInfoEnabled) log.Info("Monitor-Form thread terminated.");
        }

        //Used from second Thread!
        private void showMonitor(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker parent = sender as BackgroundWorker;
            if (parent != null)
            {
                if (log.IsInfoEnabled) log.Info("Monitor-Form thread started.");
                frmMonitor _monitorFrm = new frmMonitor(parent);
                _monitorFrm.ShowDialog();
                if (log.IsInfoEnabled) log.Info("Monitor-Form thread finished.");
            }
      }

        private void killMonitor()
        {
            //if (_monitorThread != null && _monitorThread.IsBusy)
            //    _monitorThread.CancelAsync();
        }

        #endregion

        private void InitDirect3D()
        {
            if (log.IsInfoEnabled) log.Info("Initializing Direct 3D...");
            //Read DepthFormat
            if (log.IsInfoEnabled) log.Info("-> Reading DepthFormat...");
            String sdFormat = MAppConfig.getInstance().getConfigValue("direct3d", "DepthFormat");
            if (log.IsInfoEnabled) log.Info("-> ...DepthFormat is '" + sdFormat + "'.");
            DepthFormat dFormat = DepthFormat.D24S8;
            switch (sdFormat)
            {
                case "D32": dFormat = DepthFormat.D32; break;
                case "D15S1": dFormat = DepthFormat.D15S1; break;
                case "D24S8": dFormat = DepthFormat.D24S8; break;
                case "D16": dFormat = DepthFormat.D16; break;
                case "D24X8": dFormat = DepthFormat.D24X8; break;
                case "D24X4S4": dFormat = DepthFormat.D24X4S4; break;
                default: dFormat = DepthFormat.Unknown; break;
            }
            //Read DeviceType
            if (log.IsInfoEnabled) log.Info("-> Reading DeviceType...");
            String sdType = MAppConfig.getInstance().getConfigValue("direct3d", "DeviceType");
            if (log.IsInfoEnabled) log.Info("-> ...DeviceType is '" + sdFormat + "'.");
            DeviceType dType = DeviceType.Hardware;
            switch (sdType)
            {
                case "Hardware": dType = DeviceType.Hardware; break;
                case "Software": dType = DeviceType.Software; break;
                case "Reference": dType = DeviceType.Reference; break;
            }

            // Does the hardware support a 16-bit z-buffer?
            if (log.IsInfoEnabled) log.Info("-> Check settings in Direct 3D Manager...");
            //Manager.Adapters.Default.CurrentDisplayMode.Format
            //if (!Manager.CheckDeviceFormat(Manager.Adapters.Default.Adapter,
            //                               dType,Format.A4R4G4B4
            //                               ,
            //                               Usage.DepthStencil,
            //                               ResourceType.Surface,
            //                               dFormat))
            //{
                // POTENTIAL PROBLEM: We need at least a 16-bit z-buffer!
            //    if (log.IsErrorEnabled) log.Error("Can't init Direct X specified settings.");
            //    return;
            // }
            if (log.IsInfoEnabled) log.Info("-> ...checked.");

            //
            // Do we support hardware vertex processing? if so, use it. 
            // If not, downgrade to software.
            //
            if (log.IsInfoEnabled) log.Info("-> Checkup VertexProcessing mode...");
            Caps caps = Manager.GetDeviceCaps(Manager.Adapters.Default.Adapter, dType);
            CreateFlags flags;

            if (caps.DeviceCaps.SupportsHardwareTransformAndLight)
            {
                if (log.IsInfoEnabled) log.Info("-> VertexProcessing: Hardware");
                flags = CreateFlags.HardwareVertexProcessing;
            }
            else
            {
                if (log.IsInfoEnabled) log.Info("-> VertexProcessing: Software");
                flags = CreateFlags.SoftwareVertexProcessing;
            }
            if (log.IsInfoEnabled) log.Info("-> ...checked up VertexProcessing mode.");

            //
            // Everything checks out - create a simple, windowed device.
            //
            if (log.IsInfoEnabled) log.Info("-> Creating Direct 3D window device...");
            PresentParameters d3dpp = new PresentParameters();

            //d3dpp.BackBufferFormat = Format.Unknown;
            d3dpp.SwapEffect = SwapEffect.Discard;
            d3dpp.Windowed = true;
            //d3dpp.AutoDepthStencilFormat = dFormat;
            //d3dpp.EnableAutoDepthStencil = true;
            //d3dpp.AutoDepthStencilFormat = DepthFormat.D16;
            //d3dpp.PresentationInterval = PresentInterval.Immediate;

            _d3dDdevice = new Device(0, dType, this._panel, flags, d3dpp);

            // Register an event-handler for DeviceReset and call it to continue
            // our setup.
            _d3dDdevice.DeviceReset += new System.EventHandler(this.OnResetDevice);
            OnResetDevice(_d3dDdevice, null);
            if (log.IsInfoEnabled) log.Debug("-> ...created.");
            if (log.IsInfoEnabled) log.Info("... initialized Direct 3D.");
        }

        private void ShowForm()
        {
            this.WindowState = FormWindowState.Normal;
            this.WindowState = FormWindowState.Maximized;
        }

        private void InitForm()
        {
            if (log.IsInfoEnabled) log.Info("Initializing slideshow window...");
            try
            {
                //Maximze & Position Form
                String slideShowMonitor = MAppConfig.getInstance().getConfigValue("slideShow", "monitor");
                if (log.IsDebugEnabled) log.Debug("-> Screen: " + slideShowMonitor);
                foreach (Screen screen in Screen.AllScreens)
                {
                    //Wenn null or default dann letztbesten (wegen nicht vorhandenem break;) nehmen
                    if (slideShowMonitor == null || slideShowMonitor == String.Empty)
                    {
                        this.Left = screen.Bounds.Left + 12;
                        this.Top = screen.Bounds.Top + 12;
                    }
                    //Wenn nicht null, dann auswählen und nehmen
                    else if (slideShowMonitor == screen.DeviceName)
                    {
                        this.Left = screen.Bounds.Left + 12;
                        this.Top = screen.Bounds.Top + 12;
                        break;
                    }
                }
                if (log.IsDebugEnabled) log.Debug("-> Coordinates (based on screen setup): " + this.Left + "px left, "+this.Top+"px top. Each one -12 is screens bounds.");
                this.StartPosition = FormStartPosition.Manual;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("No initialization of slideshow window possible: " + e.Message);
            }
            finally
            {
                if (log.IsInfoEnabled) log.Info("... initializing finished.");
            }
        }

        private CImage detectImage()
        {
            if (log.IsInfoEnabled) log.Info("Detecting new CImage...");
            SDB db = new SDB();
            CImage img;
            if (log.IsDebugEnabled) log.Debug("Checking lookup mode...");
            if (Boolean.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewAllPictures", "false")))
            {
                if (log.IsInfoEnabled) log.Info("-> Lookup mode is randomly from all images.");
                img = db.getRandomImage();
            }
            else if (Boolean.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewLastPickedUpFirst", "false")))
            {
                if (log.IsInfoEnabled) log.Info("-> Lookup mode is last image picked up.");
                img = db.getLastPickedUpImage();
                if (_frontTexture != null && _frontTexture.Image != null && (img.FileId == _frontTexture.Image.FileId || img.FileId == _lastViewedLastPickedUpImageId))
                {
                    if (log.IsInfoEnabled) log.Info("-> Same picture looked up again, selecting random one?");
                    if (Boolean.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewAllPicturesIfNoNewOne", "true")))
                    {
                        if (log.IsInfoEnabled) log.Info("-> Emergency mode is randomly from all images.");
                        img = db.getRandomImage();
                    }
                    else
                    {
                        if (log.IsInfoEnabled) log.Info("-> Emergency mode is to view default background (in this case last image is viewed again).");
                        return img;
                    }
                }
                else
                {
                    //AllRight, we can view the LastPickedUp one and we safe the ID for better knowing later on
                    _lastViewedLastPickedUpImageId = img.FileId;
                }
            }
            else
            {
                if (log.IsInfoEnabled) log.Info("-> Lookup mode is only not viewed images.");
                img = db.getNextViewImage();
            }
            db.Dispose();
            //No image? -> Try emergency image
            if (img == null || !File.Exists(img.FilePath))
            {
                if (log.IsInfoEnabled) log.Info("No image detected, detecting emergency image...");
                if (log.IsDebugEnabled) log.Debug("Checking emergency mode...");
                if (Boolean.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewAllPicturesIfNoNewOne", "true")))
                {
                    if (log.IsInfoEnabled) log.Info("-> Emergency mode is randomly from all images.");
                    img = db.getRandomImage();
                }
                else
                {
                    if (log.IsInfoEnabled) log.Info("-> Emergency mode is to view default background.");
                }
            }
            //No emergency image or stopped?
            if (img == null || !File.Exists(img.FilePath) || _stop)
            {
                img = new CImage();
                img.FilePath = @"img\background.jpg";

                if (_stop)
                    _stop = false;
            }
            if (log.IsInfoEnabled) log.Info("...CImage with path '"+img.FilePath+"' detected");
            return img;
        }

        #region Steuerung & Status

        public void Start()
        {
            _show = true;
            _stop = false;
            _showed = 0;
            FadeWait_Start();
            if (log.IsInfoEnabled) log.Info("Slideshow started.");
        }

        public void Pause()
        {
            _show = false;
            if (log.IsInfoEnabled) log.Info("Slideshow paused.");
        }

        public void Stop()
        {
            _show = false;
            _stop = true;
            if (log.IsInfoEnabled) log.Info("Slideshow stoped.");
        }

        /// <summary>
        /// Abbruch-Variable
        /// </summary>
        public bool Show
        {
            get { return _show; }
        }

        /// <summary>
        /// Rückmelde-Variable
        /// </summary>
        public bool Showing
        {
            get { return _showing; }
        }

        /// <summary>
        /// Status-Variable
        /// </summary>
        public int Showed
        {
            get { return _showed; }
        }

        #endregion

        #region Fading Processing

        //FadeIn CImage (detect before)
        private void Fading_Start()
        {
            if (log.IsInfoEnabled) log.Info("Start fading initializing...");
            try
            {
                //Emergency init
                if (_d3dDdevice == null)
                    InitDirect3D();
                //Get Image
                CImage img = detectImage();
                //Load a Texture
                if (_backTexture != null)
                    _backTexture.Dispose();
                _backTexture = null;
                _backTexture = new CTexture();
                _backTexture.Image = img;

                //Load and size an image
                if (log.IsDebugEnabled) log.Debug("-> Load and resize image.");
                Image originalImage = Image.FromFile(img.FilePath);
                Bitmap reImage = reSize(originalImage, new Size(this._panel.Width, this._panel.Height));

                //Wirite the ID inside
                if (Boolean.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewImageID", "false")))
                {
                    Graphics g = Graphics.FromImage(reImage);
                    String imageID = img.FileId.ToString();
                    if (MAppConfig.getInstance().getConfigValue("slideShow", "viewImageID_Mode", "1") == "1")
                        imageID = img.FilePath.Substring(img.FilePath.LastIndexOf("\\")+1).ToLower().Replace("IMG","").Replace(".jpg","").Replace("_","");
                    Brush backGround = new SolidBrush(Color.FromArgb(Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewImageID_BackgroundTransparency", "190")), 0, 0, 0));
                    g.FillRectangle(backGround, new RectangleF(0, reImage.Height - Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewImageID_Height", "190")), reImage.Width, Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewImageID_Height", "190"))));
                    StringFormat strFormat = new StringFormat();
                    strFormat.Alignment = StringAlignment.Center;
                    strFormat.LineAlignment = StringAlignment.Center;
                    g.DrawString(imageID, new System.Drawing.Font("Arial", Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewImageID_FontSize", "24"))), Brushes.White, new RectangleF(0, reImage.Height - Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewImageID_Height", "190")), reImage.Width, Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewImageID_Height", "190"))), strFormat);
                    g.Dispose();
                }

                //Save the image to the texture
                if (log.IsDebugEnabled) log.Debug("-> Save the image to the back texture.");
                _backTexture.Picture = reImage;
                _backTexture.Texture = new Texture(_d3dDdevice, (Bitmap)reImage, Usage.Dynamic, Pool.Default);
                _backTexture.Opacity = 0;

                //Clear the screen & Clean the ressources
                if (log.IsDebugEnabled) log.Debug("-> Clear the screen and clean the ressources.");
                _d3dDdevice.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
                originalImage.Dispose();
                originalImage = null;

                //Start the fading
                if (log.IsDebugEnabled) log.Debug("-> Setup fading parameters...");
                if (_backTexture != null && _frontTexture != null && _backTexture.Image.FilePath == _frontTexture.Image.FilePath)
                {
                    if (log.IsWarnEnabled) log.Warn("Warning: Fading target is same as fading source! Disable fading!");
                    FadeWait_Start();
                }
                else
                {
                    this._timerFading.Interval = Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "fadeTime", "125"));
                    if (log.IsDebugEnabled) log.Debug("-> Start fading timer with intervall of '" + this._timerFading.Interval + "'ms.");
                    this._timerFading.Enabled = true;
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Error while initialize fading: "+e.Message);
            }
            finally
            {
                if (log.IsInfoEnabled) log.Info("...finished fading initialization - Now start Direct X Fading via timers.");
            }
        }

        //FadeIn Processing
        private void Fading_Do()
        {
            if (log.IsDebugEnabled) log.Debug("Doing fading...");

            //Count the opacity
            if (log.IsDebugEnabled) log.Debug("-> Count the opacity.");
            if (_backTexture != null)
            {
                _backTexture.Opacity += Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "fadeValue", "1"));
                if (log.IsDebugEnabled) log.Debug("   -> Back texture opacity is '"+_backTexture.Opacity+"'.");
            }
            if (_frontTexture != null)
            {
                _frontTexture.Opacity -= Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "fadeValue", "1"));
                if (log.IsDebugEnabled) log.Debug("   -> Front texture opacity is '" + _frontTexture.Opacity + "'.");
            }

            //Checks values
            if (log.IsDebugEnabled) log.Debug("-> Valide the opacity values.");
            if (_frontTexture != null && _frontTexture.Opacity <= 0)
            {
                _frontTexture.Opacity = 0;
                if (log.IsDebugEnabled) log.Debug("   -> Front texture opacity is '" + _frontTexture.Opacity + "'.");
            }
            if (_backTexture != null && _backTexture.Opacity >= 255)
            {
                _backTexture.Opacity = 255;
                if (log.IsDebugEnabled) log.Debug("   -> Back texture opacity is '" + _backTexture.Opacity + "'.");
            }

            //Stops the timer
            if (log.IsDebugEnabled) log.Debug("-> Check if all is right.");
            if (((_frontTexture != null && _frontTexture.Opacity == 0) || _frontTexture == null) && ((_backTexture != null && _backTexture.Opacity == 255) || _backTexture == null))
            {
                if (log.IsDebugEnabled) log.Debug("   -> Stoping fading timer.");
                Fading_Stop();
                return;
            }

            //Render
            if (log.IsDebugEnabled) log.Debug("-> Raise rendering.");
            this.Render();

            if (log.IsDebugEnabled) log.Debug("...finished fading.");
        }

        //FadeIn Stop
        private void Fading_Stop()
        {
            //Stop fading
            if (log.IsInfoEnabled) log.Info("Direct X Fading has finished - Now doing cleanup stuff...");
            if (log.IsDebugEnabled) log.Debug("-> Stop fading timer.");
            this._timerFading.Enabled = false;

            //Toggle Foreground and Background
            if (log.IsDebugEnabled) log.Debug("-> Toogle front and back texture.");
            CTexture tmp = _frontTexture;
            _frontTexture = _backTexture;
            _backTexture = tmp;
            if (tmp != null)
                tmp.Dispose();
            tmp = null;
            try
            {
                //Save the image as viewed in DB
                if (log.IsDebugEnabled) log.Debug("-> Saving front texture to database.");
                _frontTexture.Image.ViewedDate = DateTime.Now;
                _frontTexture.Image.Viewed = true;
                _frontTexture.Image.ViewCount = _frontTexture.Image.ViewCount + 1;
                //Schreibe das Bild in die DB!
                if (!Boolean.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "viewOnlyMode", "false")))
                {
                    SDB db = new SDB();
                    db.updateImage(_frontTexture.Image);
                    db.Dispose();
                }
                else
                {
                    if (log.IsInfoEnabled) log.Info("View only mode is set to active (Kiosk-Mode). Pictures are not updated in Database!");
                }
                //Count the internal viewing counter
                _showed++;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Image kann nicht zurückgespeichert werden. Viewed wurde nicht gesetzt! Error: "+e.Message);
            }

            //Start showing
            if (log.IsDebugEnabled) log.Debug("-> Start fade waiting timer.");
            this.FadeWait_Start();
            //Force a GarbageCollection
            if (log.IsDebugEnabled) log.Debug("-> Cleanup with forced garbe collection.");
            System.GC.Collect();
            if (log.IsInfoEnabled) log.Info("...Finished cleanup stuff");
        }

        //FadeWait Start
        private void FadeWait_Start()
        {
            this._timerFadeWait.Interval = Int32.Parse(MAppConfig.getInstance().getConfigValue("slideShow", "showTime", "3000"));
            if (log.IsInfoEnabled) log.Info("Start fade waiting timer with intervall of '" + this._timerFadeWait.Interval + "'ms.");
            if (_show)
            {
                this._timerFadeWait.Enabled = true;
                _showing = true;
            }
            else
            {
                if (log.IsInfoEnabled) log.Info("-> Do not start because of raised stop event.");
                _showing = false;
            }

            //Nocheinmal durchlaufen wegen Background setzen!
            if (_stop)
                Fading_Start();
        }

        //FadeWait Processing
        private void FadeWait_Do()
        {
            if (log.IsDebugEnabled) log.Debug("Waited enough.");
            //Stop directly
            FadeWait_Stop();
        }

        //FadeWait Stop
        private void FadeWait_Stop()
        {
            if (log.IsInfoEnabled) log.Info("Stop fade waiting timer and start fading.");
            this._timerFadeWait.Enabled = false;
            Fading_Start();
        }

        #endregion

        #region Direct3D Handling

        /// <summary>
        /// This event-handler is a good place to create and initialize any 
        /// Direct3D related objects, which may become invalid during a 
        /// device reset.
        /// </summary>
        public void OnResetDevice(object sender, EventArgs e)
        {
            // This sample doens't create anything that requires recreation 
            // after the DeviceReset event.
        }

        private void Render()
        {
            DrawImages();
        }

        #endregion

        #region I/O Handling

        protected override void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case System.Windows.Forms.Keys.Escape:
                    this.Dispose();
                    break;
            }
        }

        #endregion

        #region Windows-Form Events

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            //if (MessageBox.Show(this, "Soll die Slideshow beendet werden?", CApp.getInstance().appName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            //{
            //    e.Cancel = true;
            //    return;
            //}
            closeSubForms();
        }

        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void _timerFadeIn_Tick(object sender, EventArgs e)
        {
            Fading_Do();
        }

        private void _timerFadeWait_Tick(object sender, EventArgs e)
        {
            FadeWait_Do();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {

        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            ShowForm();
            // Start Fading
            Fading_Start();
        }

        #endregion
    }
}
