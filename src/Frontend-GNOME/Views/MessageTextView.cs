/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using Gtk;
using Smuxi.Common;
using Smuxi.Engine;
using System;

namespace Smuxi.Frontend.Gnome
{
    public class MessageTextView : TextView
    {
        private TextTagTable _MessageTextTagTable;
        private MessageModel _MessageModel;
        private bool         _ShowTimestamps;
        private bool         _ShowHighlight;
        private bool?        _HasHighlight;
        private ChatView     _ChatView;
        private UserConfig   _Config;
        private Gdk.Color?   _BackgroundColor;
        private Gdk.Color?   _ForegroundColor;
        private Pango.FontDescription _FontDescription;

        /*
         * Properties
         */
        public MessageModel MessageModel
        {
            get {
                return _MessageModel;
            }
            set {
                _MessageModel = value;
            }
        }

        public bool ShowTimestamps
        {
            get {
                return _ShowTimestamps;
            }
            set {
                _ShowTimestamps = value;
            }
        }

        public bool ShowHighlight
        {
            get {
                return _ShowHighlight;
            }
            set {
                _ShowHighlight = value;
            }
        }

        public bool? HasHighlight
        {
            get {
                return _HasHighlight;
            }
        }

        public bool HasTextViewSelection {
            get {
#if GTK_SHARP_2_10
                return Buffer.HasSelection;
#else
                Gtk.TextIter start, end;
                Buffer.GetSelectionBounds(out start, out end);
                return start.Offset != end.Offset;
#endif
            }
        }

        protected Pango.FontDescription FontDescription {
            get {
                return _FontDescription;
            }
        }

        protected Gdk.Color? BackgroundColor {
            get {
                return _BackgroundColor;
            }
        }

        protected Gdk.Color? ForegroundColor {
            get {
                return _ForegroundColor;
            }
        }

        /*
         * Constructors
         */
        public MessageTextView()
        {
            MessageTextView(null);
        }

        public MessageTextView(ChatView chatview)
        {
            Trace.Call(cview);

            _MessageTextTagTable = BuildTagTable();
            _ChatView = chatview;
            
            this.Buffer = new TextBuffer(_MessageTextTagTable);
        }

        /*
         * Public methods
         */
        public void ApplyConfig(UserConfig config)
        {
            _Config = config;
            string bgStr = (string) config["Interface/Chat/BackgroundColor"];
            if (!String.IsNullOrEmpty(bgStr)) {
                Gdk.Color bgColor = Gdk.Color.Zero;
                if (Gdk.Color.Parse(bgStr, ref bgColor)) {
                    ModifyBase(Gtk.StateType.Normal, bgColor);
                    _BackgroundColor = bgColor;
                }
            } else {
                ModifyBase(Gtk.StateType.Normal);
                _BackgroundColor = null;
            }

            string fgStr = (string) config["Interface/Chat/ForegroundColor"];
            if (!String.IsNullOrEmpty(fgStr)) {
                Gdk.Color fgColor = Gdk.Color.Zero;
                if (Gdk.Color.Parse(fgStr, ref fgColor)) {
                    ModifyText(Gtk.StateType.Normal, fgColor);
                    _ForegroundColor = fgColor;
                }
            } else {
                ModifyText(Gtk.StateType.Normal);
                _ForegroundColor = null;
            }
            
            string fontFamily = (string) config["Interface/Chat/FontFamily"];
            string fontStyle = (string) config["Interface/Chat/FontStyle"];
            int fontSize = 0;
            if (config["Interface/Chat/FontSize"] != null) {
                fontSize = (int) config["Interface/Chat/FontSize"];
            }
            Pango.FontDescription fontDescription = new Pango.FontDescription();
            if (String.IsNullOrEmpty(fontFamily)) {
                // use Monospace and Bold by default
                fontDescription.Family = "monospace";
                // black bold font on white background looks odd 
                //fontDescription.Weight = Pango.Weight.Bold;
            } else {
                fontDescription.Family = fontFamily;
                string frontWeigth = null;
                if (fontStyle.Contains(" ")) {
                    int pos = fontStyle.IndexOf(" ");
                    frontWeigth = fontStyle.Substring(0, pos);
                    fontStyle = fontStyle.Substring(pos + 1);
                }
                fontDescription.Style = (Pango.Style) Enum.Parse(typeof(Pango.Style), fontStyle);
                if (frontWeigth != null) {
                    fontDescription.Weight = (Pango.Weight) Enum.Parse(typeof(Pango.Weight), frontWeigth);
                }
                fontDescription.Size = fontSize * 1024;
            }
            _FontDescription = fontDescription;
            
            ModifyFont(_FontDescription);
            
            string wrapModeStr = (string) config["Interface/Chat/WrapMode"];
            if (!String.IsNullOrEmpty(wrapModeStr)) {
                Gtk.WrapMode wrapMode = (Gtk.WrapMode) Enum.Parse(typeof(Gtk.WrapMode), wrapModeStr);
                WrapMode = wrapMode;
            }
        }

        public void Clear()
        {
            Trace.Call();
            Buffer.Clear();
        }

        public void AddMessage(MessageModel msg)
        {
            Trace.Call(msg);
            
            TextIter iter = Buffer.EndIter;
            
            if (_ShowTimestamps) {
                string timestamp = null;
                try {
                    string format = (string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"];
                    if (!String.IsNullOrEmpty(format)) {
                        timestamp = msg.TimeStamp.ToLocalTime().ToString(format);
                    }
                } catch (FormatException e) {
                    timestamp = "Timestamp Format ERROR: " + e.Message;
                }

                if (timestamp != null) {
                    _OutputTextView.Buffer.Insert(ref iter, timestamp + " ");
                }
            }

            _HasHighlight = false;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
#if LOG4NET
                _Logger.Debug("AddMessage(): msgPart.GetType(): " + msgPart.GetType());
#endif
                // supposed to be used only in a ChatView
                if (_ShowHighlight && msgPart.IsHighlight) {
                    _HasHighlight = true;
                }
                    
//                            set {
//                _HasHighlight = value;
//                
//                if (!value) {
//                    // clear highlight with "no activity"
//                    HasActivity = false;
//                    return;
//                }
//                
//                string color = Frontend.UserConfig["Interface/Notebook/Tab/HighlightColor"];
//                _TabLabel.Markup = String.Format("<span foreground=\"{0}\">{1}</span>", color, _Name);
//            }
                
                Gdk.Color bgColor = DefaultAttributes.Appearance.BgColor;
                // TODO: where do we get _BackgroundColor from? Check ChatView
//                if (_BackgroundColor != null) {
//                    bgColor = _BackgroundColor.Value;
//                }
                TextColor bgTextColor = ColorTools.GetTextColor(bgColor);
                // TODO: implement all types
                if (msgPart is UrlMessagePartModel) {
                    UrlMessagePartModel fmsgui = (UrlMessagePartModel) msgPart;
                    // HACK: the engine should set a color for us!
                    Gtk.TextTag urlTag = _OutputTextTagTable.Lookup("url");
                    Gdk.Color urlColor = urlTag.ForegroundGdk;
                    //Console.WriteLine("urlColor: " + urlColor);
                    TextColor urlTextColor = ColorTools.GetTextColor(urlColor);
                    urlTextColor = ColorTools.GetBestTextColor(urlTextColor, bgTextColor);
                    //Console.WriteLine("GetBestTextColor({0}, {1}): {2}",  urlColor, bgTextColor, urlTextColor);
                    urlTag.ForegroundGdk = ColorTools.GetGdkColor(urlTextColor);
                    Buffer.InsertWithTagsByName(ref iter, fmsgui.Url, "url");
                } else if (msgPart is TextMessagePartModel) {
                    TextMessagePartModel fmsgti = (TextMessagePartModel) msgPart;
#if LOG4NET
                    _Logger.Debug("AddMessage(): fmsgti.Text: '" + fmsgti.Text + "'");
#endif
                    List<string> tags = new List<string>();
                    if (fmsgti.ForegroundColor != TextColor.None) {
                        TextColor color = ColorTools.GetBestTextColor(fmsgti.ForegroundColor, bgTextColor);
                        //Console.WriteLine("GetBestTextColor({0}, {1}): {2}",  fmsgti.ForegroundColor, bgTextColor, color);
                        string tagname = _GetTextTagName(color, null);
                        //string tagname = _GetTextTagName(fmsgti.ForegroundColor, null);
                        tags.Add(tagname);
                    }
                    if (fmsgti.BackgroundColor != TextColor.None) {
                        // TODO: get this from ChatView
                        string tagname = _GetTextTagName(null, fmsgti.BackgroundColor);
                        tags.Add(tagname);
                    }
                    if (fmsgti.Underline) {
#if LOG4NET
                        _Logger.Debug("AddMessage(): fmsgti.Underline is true");
#endif
                        tags.Add("underline");
                    }
                    if (fmsgti.Bold) {
#if LOG4NET
                        _Logger.Debug("AddMessage(): fmsgti.Bold is true");
#endif
                        tags.Add("bold");
                    }
                    if (fmsgti.Italic) {
#if LOG4NET
                        _Logger.Debug("AddMessage(): fmsgti.Italic is true");
#endif
                        tags.Add("italic");
                    }
                    
                    Buffer.InsertWithTagsByName(ref iter, fmsgti.Text, tags.ToArray());
                } 
            }
            // FIXME: this shouldn't happen in a topic, spurious \n added
            Buffer.Insert(ref iter, "\n");
            
            // event MessageAdded
            // event MessageHighlighted
            // HACK: out of scope?
            if (hasHighlight && !Frontend.MainWindow.HasToplevelFocus) {
                Frontend.MainWindow.UrgencyHint = true;
#if GTK_SHARP_2_10
                Frontend.StatusIcon.Blinking = true;
#endif
                if (Frontend.UserConfig["Sound/BeepOnHighlight"] != null &&
                    (bool)Frontend.UserConfig["Sound/BeepOnHighlight"]) {
                    Frontend.MainWindow.Display.Beep();
                }
            }
            
            // HACK: out of scope?
            if (Frontend.MainWindow.Notebook.CurrentChatView != this) {
                if (hasHighlight &&
                    _ChatModel.LastSeenHighlight < msg.TimeStamp) {
                    HasHighlight = true;
                }
                
                switch (msg.MessageType) {
                    case MessageType.Normal:
                        HasActivity = true;
                        break;
                    case MessageType.Event:
                        HasEvent = true;
                        break;
                }
            }
        }

        /*
         * Helper methods
         */
        private TextTagTable BuildTagTable()
        {
            // TextTags
            Gtk.TextTagTable ttt = new Gtk.TextTagTable();
            Gtk.TextTag tt;
            Pango.FontDescription fd;
            
            tt = new Gtk.TextTag("bold");
            fd = new Pango.FontDescription();
            fd.Weight = Pango.Weight.Bold;
            tt.FontDesc = fd;
            ttt.Add(tt);

            tt = new Gtk.TextTag("italic");
            fd = new Pango.FontDescription();
            fd.Style = Pango.Style.Italic;
            tt.FontDesc = fd;
            ttt.Add(tt);
            
            tt = new Gtk.TextTag("underline");
            tt.Underline = Pango.Underline.Single;
            ttt.Add(tt);
            
            tt = new Gtk.TextTag("url");
            tt.Underline = Pango.Underline.Single;
            tt.Foreground = "darkblue";
            tt.TextEvent += new Gtk.TextEventHandler(_OnTextTagUrlTextEvent);
            fd = new Pango.FontDescription();
            tt.FontDesc = fd;
            ttt.Add(tt);
        }

        /*
         * Event handlers
         */
        public void _OnTextTagUrlTextEvent(object sender, EventArgs e)
        {
        }
    }
}
