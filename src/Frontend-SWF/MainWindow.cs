using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Swf
{
	public partial class MainWindow: Form
	{
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private bool             _CaretMode;
        /* TODO
	    private ChatViewManager  _ChatViewManager;
         */
	    private IFrontendUI      _UI;
        public bool CaretMode {
            get {
                return _CaretMode;
            }
        }
        
        public Notebook Notebook {
            get {
                return null;
                //return _Notebook;
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
                return null;
                //return _Entry;
            }
        }
		public MainWindow()
		{
			InitializeComponent();
		}

	}
}