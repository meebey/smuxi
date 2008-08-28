
using System;
using System.Reflection;
using System.Windows.Forms;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Swf
{
    public partial class MainWindow : Form
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private bool             _CaretMode;
        private ChatViewManager  _ChatViewManager;
        private IFrontendUI      _UI;
        private Notebook         _Notebook;
        private Entry            _Entry;
        
        public bool CaretMode {
            get {
                return _CaretMode;
            }
        }
        
        public Notebook Notebook {
            get {
                return _Notebook;
            }
        }
        
        public IFrontendUI UI {
            get {
                return _UI;
            }
        }
        
        public ToolStripStatusLabel NetworkStatusbar {
            get {
                return _NetworkStatusbar;
            }
        } 

        public ToolStripStatusLabel Statusbar {
            get {
                return _Statusbar;
            }
        } 

        public ToolStripProgressBar ProgressBar {
            get {
                return _ProgressBar;
            }
        }
        
        public Entry Entry {
            get {
                return _Entry;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            
            _Entry.Notebook = _Notebook;
            
            _Notebook.Show();
            
            _ChatViewManager = new ChatViewManager(_Notebook);
            Assembly asm = Assembly.GetExecutingAssembly();
            _ChatViewManager.Load(asm);
            _ChatViewManager.LoadAll(System.IO.Path.GetDirectoryName(asm.Location),
                                     "smuxi-frontend-swf-*.dll");
            
            _UI = new SwfUI(_ChatViewManager, this);
            
            _NetworkStatusbar.Text = String.Empty;
            _Statusbar.Text = String.Empty;
        }
        
        public void ApplyConfig(UserConfig userConfig)
        {
            Trace.Call(userConfig);
            
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }
            
            _Entry.ApplyConfig(userConfig);
            _Notebook.ApplyConfig(userConfig);
            _ChatViewManager.ApplyConfig(userConfig);
        }
    }
}
