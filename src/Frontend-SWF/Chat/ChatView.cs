using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;

namespace Smuxi.Frontend.Swf
{
    public partial class ChatView : TabPage, IChatView
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private   ChatModel          _ChatModel;
        private   bool               _HasHighlight;

        //protected override void OnPaint(PaintEventArgs pe)
        //{
        //    // TODO: Add custom paint code here

        //    // Calling the base class OnPaint
        //    base.OnPaint(pe);
        //}


        public ChatModel ChatModel
        {
            get { return _ChatModel; }
        }

        public RichTextBox OutputTextView
        {
            get { return _OutputTextView; }
        }

        public bool HasHighlight {
            get {
                return _HasHighlight;
            }
            set {
                _HasHighlight = value;
            }
        }

        public ChatView(ChatModel chat) : this()
        {
            _ChatModel = chat;

            Name = chat.Name;
            Text = Name;
        }


        private ChatView()
        {
            InitializeComponent();
        }



        public void ScrollUp()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void ScrollDown()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void ScrollToStart()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void ScrollToEnd()
        {
            throw new Exception("The method or operation is not implemented.");
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

        public void AddMessage(MessageModel msg)
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
                    
                    _OutputTextView.AppendText(fmsgti.Text);                } 
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
            if (false /*TODO: Come up with a way to deturmine if top level chatview*/) {
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