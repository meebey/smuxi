using System;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;

namespace Smuxi.Frontend.Swf
{
    public abstract partial class ChatView : TabPage, IChatView
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private   ChatModel          _ChatModel;
        private   bool               _HasHighlight;
        private   RichTextBox        _OutputTextView;
        private   Color?             _BackgroundColor;
        private   Color?             _ForegroundColor;
        private   Font               _Font;
        
        //protected override void OnPaint(PaintEventArgs pe)
        //{
        //    // TODO: Add custom paint code here

        //    // Calling the base class OnPaint
        //    base.OnPaint(pe);
        //}

        public ChatModel ChatModel {
            get {
                return _ChatModel;
            }
        }

        public RichTextBox OutputTextView {
            get {
                return _OutputTextView;
            }
        }

        public bool HasHighlight {
            get {
                return _HasHighlight;
            }
            set {
                _HasHighlight = value;
            }
        }

        protected Font Font {
            get {
                return _Font;
            }
        }

        protected Color? BackgroundColor {
            get {
                return _BackgroundColor;
            }
        }

        protected Color? ForegroundColor {
            get {
                return _ForegroundColor;
            }
        }
        
        protected ChatView(ChatModel chat)
        {
            _ChatModel = chat;

            InitializeComponent();
            // BUG? the designer doesn't add the control to the TabPage
            Controls.Add(_OutputTextView);
            
            Name = chat.Name;
            Text = chat.Name;
        }

        public void ScrollUp()
        {
            Trace.Call();

            // TODO
        }

        public void ScrollDown()
        {
            Trace.Call();

            // TODO
        }

        public void ScrollToStart()
        {
            Trace.Call();

            // TODO
        }

        public void ScrollToEnd()
        {
            Trace.Call();
            
            _OutputTextView.SelectionStart = _OutputTextView.Text.Length;
            _OutputTextView.SelectionLength = 0;
            _OutputTextView.ScrollToCaret();
        }

        public void Enable()
        {
            Trace.Call();

            Enabled = true;
        }

        public void Disable()
        {
            Trace.Call();

            Enabled = false;
        }

        public virtual void Sync()
        {
            Trace.Call();
            
#if LOG4NET
            _Logger.Debug("Sync() syncing messages");
#endif
            // sync messages
            // cleanup, be sure the output is empty
            _OutputTextView.Clear();
            IList<MessageModel> messages = _ChatModel.Messages;
            if (messages.Count > 0) {
                foreach (MessageModel msg in messages) {
                    AddMessage(msg);
                }
            }
        }

        public virtual void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);
            
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            
            string bgStr = (string) config["Interface/Chat/BackgroundColor"];
            if (!String.IsNullOrEmpty(bgStr)) {
                try {
                    Color color = ColorTools.GetColor(bgStr);
                    _BackgroundColor = color;
                    _OutputTextView.BackColor = color;
                } catch (FormatException ex) {
#if LOG4NET
                    _Logger.Error("setting background color failed", ex); 
#endif
                }
            } else {
                _BackgroundColor = null;
                _OutputTextView.BackColor = Color.Empty;
            }
            
            string fgStr = (string) config["Interface/Chat/ForegroundColor"];
            if (!String.IsNullOrEmpty(fgStr)) {
                try {
                    Color color = ColorTools.GetColor(fgStr);
                    _ForegroundColor = color;
                    _OutputTextView.ForeColor = color;
                } catch (FormatException ex) {
#if LOG4NET
                    _Logger.Error("setting foreground color failed", ex); 
#endif
                }
            } else {
                _ForegroundColor = null;
                _OutputTextView.ForeColor = Color.Empty;
            }
            
            string fontFamily = (string) config["Interface/Chat/FontFamily"];
            string fontStyle = (string) config["Interface/Chat/FontStyle"];
            int fontSize = 0;
            if (config["Interface/Chat/FontSize"] != null) {
                fontSize = (int) config["Interface/Chat/FontSize"];
            }
            Font font = null;
            if (String.IsNullOrEmpty(fontFamily)) {
                // use Monospace by default
                float? defaultSize; 
                try {
                    defaultSize = Font.Size;
                } catch (NullReferenceException) {
#if LOG4NET
                    _Logger.Error("could not get default system font size, using internal default");
#endif
                    // Mono bug?
                    defaultSize = 12f; 
                }
                font = new Font(FontFamily.GenericMonospace, defaultSize.Value);
            } else {
                string fontWeigth = null;
                if (fontStyle.Contains(" ")) {
                    int pos = fontStyle.IndexOf(" ");
                    fontWeigth = fontStyle.Substring(0, pos);
                    fontStyle = fontStyle.Substring(pos + 1);
                }
                FontStyle style = (FontStyle) Enum.Parse(typeof(FontStyle), fontStyle);
                font = new Font(fontFamily, fontSize, style);
            }
            _Font = font;
            _OutputTextView.Font = font;
        }
        
        public virtual void AddMessage(MessageModel msg)
        {
            Trace.Call(msg);
            
            string timestamp;
            try {
                string format = (string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"];
                timestamp = msg.TimeStamp.ToLocalTime().ToString(format);
            } catch (FormatException e) {
                timestamp = "Timestamp Format ERROR: " + e.Message;
            }

            _OutputTextView.AppendText(timestamp + " ");
            
            bool hasHighlight = false;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
#if LOG4NET
                _Logger.Debug("AddMessage(): msgPart.GetType(): " + msgPart.GetType());
#endif
                if (msgPart.IsHighlight) {
                    hasHighlight = true;
                }
                
                // TODO: implement all types
                if (msgPart is UrlMessagePartModel) {
                    UrlMessagePartModel fmsgui = (UrlMessagePartModel) msgPart;
                    /*TODO: Create a link in the TextView (possibly requiring WinAPI hacks...)*/
                    _OutputTextView.AppendText(fmsgui.Url);
                } else if (msgPart is TextMessagePartModel) {
                    /*TODO: Add required formatting to the TextView (possibly requiring WinAPI hacks...)*/
                    TextMessagePartModel fmsgti = (TextMessagePartModel) msgPart;
#if LOG4NET
                    _Logger.Debug("AddMessage(): fmsgti.Text: '" + fmsgti.Text + "'");
#endif
                    
                    int oldTextLength = _OutputTextView.TextLength;
                    _OutputTextView.AppendText(fmsgti.Text);

                    // HACK: Mono's RichTextBox has problems with colors
                    //if (Type.GetType("Mono.Runtime") == null) {
                        if (fmsgti.ForegroundColor != TextColor.None) {
                            _OutputTextView.SelectionStart = oldTextLength;
                            _OutputTextView.SelectionLength = fmsgti.Text.Length;
#if LOG4NET
                            _Logger.Debug("AddMessage(): SelectionStart: " + _OutputTextView.SelectionStart);
                            _Logger.Debug("AddMessage(): SelectionLength: " + _OutputTextView.SelectionLength);
#endif
                            _OutputTextView.SelectionColor = ColorTools.GetColor(fmsgti.ForegroundColor);
                        }
                    //}
                    
                    if (fmsgti.Underline) {
#if LOG4NET
                        _Logger.Debug("AddMessage(): fmsgti.Underline is true");
#endif
                    }
                    if (fmsgti.Bold) {
#if LOG4NET
                        _Logger.Debug("AddMessage(): fmsgti.Bold is true");
#endif
                    }
                    if (fmsgti.Italic) {
#if LOG4NET
                        _Logger.Debug("AddMessage(): fmsgti.Italic is true");
#endif
                    }
                } 
            }
            _OutputTextView.AppendText("\n");
            
            // HACK: out of scope?
            if (hasHighlight /*&& !Frontend.MainWindow.HasToplevelFocus*/) {
                /*TODO: Flash the main window*/
                if (Frontend.UserConfig["Sound/BeepOnHighlight"] != null &&
                    (bool)Frontend.UserConfig["Sound/BeepOnHighlight"]) {
                    System.Media.SystemSounds.Beep.Play();
                }
            }
            
            // HACK: out of scope?
            if (((TabControl)this.Parent).SelectedTab != this) {
                string color = null;
                if (hasHighlight) {
                    _HasHighlight = hasHighlight;
                    color = (string) Frontend.UserConfig["Interface/Notebook/Tab/HighlightColor"];
                } else if (!_HasHighlight) {
                    color = (string) Frontend.UserConfig["Interface/Notebook/Tab/ActivityColor"];
                }
                
                if (color != null) {
                    /*TODO: Color the associated Tab*/
                }
            }
        }
    }
}
