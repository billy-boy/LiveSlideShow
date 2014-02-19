using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;

using log4net;
using System.Threading;
using log4net.Core;
using matIT.Generic.Config.AppConfig;


namespace matIT.SlideShow.Forms
{
    public partial class frmMonitor : Form
    {
        #region Static Members

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static Queue _logEvents = new Queue(500, 10f);
        private static List<String> _logFacilities = new List<String>();

        #endregion  
  
        #region Members Variables

        private BackgroundWorker _parent;
        private bool logWatching = true;  
        private log4net.Appender.MemoryAppender _logger;  
        private Thread _logWatcher;
        private bool _autoScroll = true;
        private int _logLevel = 20000;
        private System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();

        #endregion  

        public frmMonitor(BackgroundWorker parent)
        {
            _parent = parent;
            InitializeComponent();
            InitCancelCheckTimer();
            InitForm();

            this.Closing += new CancelEventHandler(Form_Closing);  
            _logger = new log4net.Appender.MemoryAppender();  
  
            // Could use a fancier Configurator if you don't want to catch every message  
            log4net.Config.BasicConfigurator.Configure(_logger);  
  
            // Since there are no events to catch on logging, we dedicate  
            // a thread to watching for logging events  
            _logWatcher = new Thread(new ThreadStart(LogWatcher));  
            _logWatcher.Start();

            //Log-Timer
            _timer.Interval = 250;
            _timer.Tick += new EventHandler(LogHandler);
            _timer.Enabled = true;
        }

        private void InitForm()
        {
            this.Text = CApp.getInstance().appName + " - Monitoring";
            try
            {
                //Screen Settings + Form Position
                String configFormMonitor = MAppConfig.getInstance().getConfigValue("cfgForm", "monitor");
                foreach (Screen screen in Screen.AllScreens)
                {
                    //Wenn null dann letztbesten nehmen
                    if (configFormMonitor == null)
                    {
                        this.Left = screen.Bounds.Left + 12 + 600;
                        this.Top = screen.Bounds.Top + 12;
                    }
                    //Wenn nicht null, dann auswählen und nehmen
                    else if (configFormMonitor == screen.DeviceName)
                    {
                        this.Left = screen.Bounds.Left + 12 + 600;
                        this.Top = screen.Bounds.Top + 12;
                    }
                }
                this.StartPosition = FormStartPosition.Manual;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled) log.Error("Keine Initialisierung des Control-Forms möglich: " + e.Message);
            }
        }

        private void Form_Closing(object sender, CancelEventArgs e)  
        {  
            // Gotta stop our logging thread  
            logWatching = false;  
            _logWatcher.Join();  
        }

        #region CancelAsync-Check

        private void InitCancelCheckTimer()
        {
            System.Windows.Forms.Timer _cancelCheckTimer = new System.Windows.Forms.Timer();
            _cancelCheckTimer.Interval = 5000;
            _cancelCheckTimer.Tick += new EventHandler(checkAsyncCancel);
            _cancelCheckTimer.Start();
        }

        private void checkAsyncCancel(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer _cancelCheckTimer = sender as System.Windows.Forms.Timer;
            if (_parent != null && _parent.CancellationPending)
            {
                if (_cancelCheckTimer != null) _cancelCheckTimer.Stop();
                this.Close();
            }
        }

        #endregion

        public delegate void addLineToListBoxDelegate(String line, ListBox box);
        public void addLineToListBox(String line, ListBox box)
        {
            if (box.InvokeRequired) box.Invoke(new addLineToListBoxDelegate(addLineToListBox),new object[] { line, box });
            else 
            { 
                //Remove an old one
                if (box.Items.Count > 100)
                {
                    box.Items.RemoveAt(0);
                }
                //Add
                box.Items.Add(line);
                //Auto-Scroll
                if (_autoScroll)
                {
                    box.SelectedIndex = box.Items.Count - 1;
                    box.SelectedIndex = -1;
                }
            }
        }

        public delegate void reloadClassComboDelegate();
        public void reloadClassCombo()
        {
            if (_cmbFacility != null)
            {
                if (_cmbFacility.InvokeRequired) _cmbFacility.Invoke(new reloadClassComboDelegate(reloadClassCombo));
                else
                {
                    String selectedFacility = null;
                    if (_cmbFacility.SelectedItem != null) selectedFacility = _cmbFacility.SelectedItem.ToString();
                    _cmbFacility.Items.Clear();
                    foreach (String facility in frmMonitor._logFacilities)
                    {
                        int index = _cmbFacility.Items.Add(facility);
                        if (selectedFacility != null && facility == selectedFacility)
                            _cmbFacility.SelectedIndex = index;
                    }
                }
            }
        }

        public delegate String selectedClassComboItemDelegate();
        public String selectedClassComboItem()
        {
            if (_cmbFacility.InvokeRequired) return _cmbFacility.Invoke(new selectedClassComboItemDelegate(selectedClassComboItem)) as String;
            else
            {
                if (_cmbFacility.SelectedItem == null)
                    return null;
                return _cmbFacility.SelectedItem.ToString();
            }
        }

        private void reloadListBoxLines()
        {
            if (frmMonitor._logEvents.Count > 0)
            {
                String line = frmMonitor._logEvents.Dequeue() as String;
                if (line != null)
                    addLineToListBox(line, _ltbMonitor);
            }
        }

        private void LogWatcher()  
        {  
            // we loop until the Form is closed  
            while(logWatching)  
            {  
                LoggingEvent[] events = _logger.GetEvents();  
                if( events != null && events.Length > 0 )  
                {  
                    // if there are events, we clear them from the logger,  
                    // since we're done with them  
                    _logger.Clear();  
                    foreach( LoggingEvent ev in events )  
                    {
                        if (!frmMonitor._logFacilities.Contains(ev.LoggerName))
                        {
                            frmMonitor._logFacilities.Add(ev.LoggerName);
                        }
                        String selectedLoggerName = selectedClassComboItem();
                        if (ev.Level.Value >= _logLevel && (selectedLoggerName == null || selectedLoggerName == String.Empty || selectedLoggerName == ev.LoggerName))
                        {
                            // the line we want to log
                            // %date  [%thread]  %-5level  [%logger] - %message
                            string line = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + " [" + ev.ThreadName + "] " + ev.Level.DisplayName + " [" + ev.LoggerName + "]: " + ev.RenderedMessage + "\r\n";
                            frmMonitor._logEvents.Enqueue(line);
                            //addLineToListBox(line, _ltbMonitor);
                        }
                    } 
                } 
                // nap for a while, don't need the events on the millisecond.  
                Thread.Sleep(1000);  
            }  
        }

        private void LogHandler(object sender, EventArgs e)
        {
            _timer.Enabled = false;
            reloadClassCombo();
            reloadListBoxLines();
            _txtBacklog.Text = frmMonitor._logEvents.Count.ToString();
            _timer.Enabled = true;
        }

        private void toggleAutoScroll()
        {
            _autoScroll = !_autoScroll;

            if (_autoScroll) _btnToggleAutoScroll.Text = "Disable Auto-Scroll";
            else _btnToggleAutoScroll.Text = "Enable Auto-Scroll";
        }

        private void _btnToggleAutoScroll_Click(object sender, EventArgs e)
        {
            toggleAutoScroll();
        }

        private void frmMonitor_ResizeEnd(object sender, EventArgs e)
        {
            int correctur = 24;

            this._ltbMonitor.Left = 0;
            this._ltbMonitor.Top = 0;
            this._ltbMonitor.Width = this.ClientSize.Width;
            this._ltbMonitor.Height = this.ClientSize.Height - 3 - this._btnToggleAutoScroll.Height - 3 - correctur;

            this._btnToggleAutoScroll.Top = this.ClientSize.Height - 3 - correctur;
            this._lblLevel.Top = this.ClientSize.Height - 3 - correctur;
            this._cmbLevel.Top = this.ClientSize.Height - 3 - correctur;
            this._lblFacility.Top = this.ClientSize.Height - 3 - correctur;
            this._cmbFacility.Top = this.ClientSize.Height - 3 - correctur;
        }

        private void _cmbLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            //OFF
            //FATAL
            //ERROR
            //WARN
            //INFO
            //DEBUG
            //ALL
            switch (_cmbLevel.SelectedItem.ToString())
            {
                case "ALL": _logLevel = 0; break;
                case "DEBUG": _logLevel = 20000; break;
                case "INFO": _logLevel = 40000; break;
                case "WARN": _logLevel = 60000; break;
                case "ERROR": _logLevel = 80000; break;
                case "FATAL": _logLevel = 100000; break;
                case "OFF": _logLevel = 120000; break;
            }
        }

    }
}
