/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2008 Mirco Bauer <meebey@meebey.net>
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

using System;
using System.Drawing;
using SysDiag = System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;
#if UI_GNOME
using GNOME = Gnome;
#endif

namespace Smuxi.Frontend.Gnome
{
    // TODO: use Gtk.Bin
    public abstract class ChatView : Gtk.EventBox, IChatView
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly Gdk.Cursor _NormalCursor = new Gdk.Cursor(Gdk.CursorType.Xterm);
        private static readonly Gdk.Cursor _LinkCursor = new Gdk.Cursor(Gdk.CursorType.Hand2);
        private   string             _Name;
        private   bool               _AtUrlTag;
        private   ChatModel          _ChatModel;
        private   bool               _HasHighlight;
        private   bool               _HasActivity;
        private   bool               _HasEvent;
        private   Gtk.TextMark       _EndMark;
        private   Gtk.Menu           _TabMenu;
        private   Gtk.Label          _TabLabel;
        private   Gtk.EventBox       _TabEventBox;
        private   Gtk.HBox           _TabHBox;
        private   Gtk.ScrolledWindow _OutputScrolledWindow;
        private   Gtk.TextView       _OutputTextView;
        private   Gtk.TextTagTable   _OutputTextTagTable;
        private   Pango.FontDescription _FontDescription;
        private   Gdk.Color?         _BackgroundColor;
        private   Gdk.Color?         _ForegroundColor;
        
        public ChatModel ChatModel {
            get {
                return _ChatModel;
            }
        }
        
        public bool HasHighlight {
            get {
                return _HasHighlight;
            }
            set {
                _HasHighlight = value;
                
                if (!value) {
                    // clear highlight with "no activity"
                    HasActivity = false;
                    return;
                }
                
                string color = (string) Frontend.UserConfig["Interface/Notebook/Tab/HighlightColor"];
                _TabLabel.Markup = String.Format("<span foreground=\"{0}\">{1}</span>", color, _Name);
            }
        }
        
        public bool HasActivity {
            get {
                return _HasActivity;
            }
            set {
                _HasActivity = value;
                
                if (HasHighlight) {
                    // don't show activity if there is a highlight active
                    return;
                }
                
                string color = null;
                if (value) {
                    color = (string) Frontend.UserConfig["Interface/Notebook/Tab/ActivityColor"];
                } else {
                    color = (string) Frontend.UserConfig["Interface/Notebook/Tab/NoActivityColor"];
                }
                _TabLabel.Markup = String.Format("<span foreground=\"{0}\">{1}</span>", color, _Name);
            }
        }
        
        public bool HasEvent {
            get {
                return _HasEvent;
            }
            set {
                if (HasHighlight) {
                    return;
                }
                if (HasActivity) {
                    return;
                }
                
                if (!value) {
                    // clear event with "no activity"
                    HasActivity = false;
                    return;
                }
                
                string color = (string) Frontend.UserConfig["Interface/Notebook/Tab/EventColor"];
                _TabLabel.Markup = String.Format("<span foreground=\"{0}\">{1}</span>", color, _Name);
            }
        }
        
        public bool HasTextViewSelection {
            get {
#if GTK_SHARP_2_10
                return _OutputTextView.Buffer.HasSelection;
#else
                Gtk.TextIter start, end;
                _OutputTextView.Buffer.GetSelectionBounds(out start, out end);
                return start.Offset != end.Offset;
#endif
            }
        }
        
        public virtual bool HasSelection {
            get {
                return HasTextViewSelection;
            }
        }
        
        public virtual new bool HasFocus {
            get {
                return base.HasFocus || _OutputTextView.HasFocus;
            }
            set {
                _OutputTextView.HasFocus = value;
            }
        }
        
        public Gtk.Widget LabelWidget {
            get {
                return _TabEventBox;
            }
        }
        
        public Gtk.TextView OutputTextView {
            get {
                return _OutputTextView;
            }
        }
        
        protected Gtk.TextTagTable OutputTextTagTable {
            get {
                return _OutputTextTagTable;
            }
        }

        protected Gtk.ScrolledWindow OutputScrolledWindow {
            get {
                return _OutputScrolledWindow;
            }
        }

        protected Gtk.HBox TabHBox {
            get {
                return _TabHBox;
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
        
        public ChatView(ChatModel chat)
        {
            Trace.Call(chat);
            
            _ChatModel = chat;
            _Name = _ChatModel.Name;
            Name = _Name;
            
            // TextTags
            Gtk.TextTagTable ttt = new Gtk.TextTagTable();
            _OutputTextTagTable = ttt;
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
            
            Gtk.TextView tv = new Gtk.TextView();
            tv.Buffer = new Gtk.TextBuffer(ttt);
            _EndMark = tv.Buffer.CreateMark("end", tv.Buffer.EndIter, false); 
            tv.Editable = false;
            //tv.CursorVisible = false;
            tv.CursorVisible = true;
            tv.WrapMode = Gtk.WrapMode.Char;
            tv.Buffer.Changed += new EventHandler(_OnTextBufferChanged);
            tv.MotionNotifyEvent += new Gtk.MotionNotifyEventHandler(_OnMotionNotifyEvent);
            _OutputTextView = tv;
            
            Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
            //sw.HscrollbarPolicy = Gtk.PolicyType.Never;
            sw.HscrollbarPolicy = Gtk.PolicyType.Automatic;
            sw.VscrollbarPolicy = Gtk.PolicyType.Always;
            sw.ShadowType = Gtk.ShadowType.In;
            sw.Add(_OutputTextView);
            _OutputScrolledWindow = sw;
            
            // popup menu
            _TabMenu = new Gtk.Menu();
            
            Gtk.ImageMenuItem close_item = new Gtk.ImageMenuItem(Gtk.Stock.Close, null);
            close_item.Activated += new EventHandler(OnTabMenuCloseActivated);  
            _TabMenu.Append(close_item);
            
            //FocusChild = _OutputTextView;
            //CanFocus = false;
            
            _TabLabel = new Gtk.Label();
            _TabLabel.Text = _Name;
            
            _TabHBox = new Gtk.HBox();
            _TabHBox.PackEnd(new Gtk.Fixed(), true, true, 0);
            _TabHBox.PackEnd(_TabLabel, false, false, 0);
            _TabHBox.ShowAll();
            
            _TabEventBox = new Gtk.EventBox();
            _TabEventBox.VisibleWindow = false;
            _TabEventBox.ButtonPressEvent += new Gtk.ButtonPressEventHandler(OnTabButtonPress);
            _TabEventBox.Add(_TabHBox);
            _TabEventBox.ShowAll();
        }
        
        public virtual void ScrollUp()
        {
            Trace.Call();

            Gtk.Adjustment adj = _OutputScrolledWindow.Vadjustment;
            adj.Value -= adj.PageSize - adj.StepIncrement;
        }
        
        public virtual void ScrollDown()
        {
            Trace.Call();

            // note: Upper - PageSize is the farest scrollable position! 
            Gtk.Adjustment adj = _OutputScrolledWindow.Vadjustment;
            if ((adj.Value + adj.PageSize) <= (adj.Upper - adj.PageSize)) {
                adj.Value += adj.PageSize - adj.StepIncrement;
            } else {
                // there is no page left to scroll, so let's just scroll to the
                // farest position instead
                adj.Value = adj.Upper - adj.PageSize;
            }
        }
        
        public virtual void ScrollToStart()
        {
            Trace.Call();
            
            Gtk.Adjustment adj = _OutputScrolledWindow.Vadjustment;
            adj.Value = adj.Lower;
        }
        
        public virtual void ScrollToEnd()
        {
            Trace.Call();
            
            Gtk.Adjustment adj = _OutputScrolledWindow.Vadjustment;
#if LOG4NET
            _Logger.Debug("ScrollToEnd(): Vadjustment.Value: " + adj.Value +
                          " Vadjustment.Upper: " + adj.Upper +
                          " Vadjustment.PageSize: " + adj.PageSize);
#endif
            
            // BUG? doesn't work always for some reason
            // seems like GTK+ doesn't update the adjustment till we give back control
            //adj.Value = adj.Upper - adj.PageSize;
            
            //_OutputTextView.Buffer.MoveMark(_EndMark, _OutputTextView.Buffer.EndIter);
            //_OutputTextView.ScrollMarkOnscreen(_EndMark);
            //_OutputTextView.ScrollToMark(_EndMark, 0.49, true, 0.0, 0.0);
            
            //_OutputTextView.ScrollMarkOnscreen(_OutputTextView.Buffer.InsertMark);

            //_OutputTextView.ScrollMarkOnscreen(_OutputTextView.Buffer.GetMark("tail"));
            
            System.Reflection.MethodBase mb = Trace.GetMethodBase();
            // WORKAROUND1: scroll after one second delay
            /*
            GLib.Timeout.Add(1000, new GLib.TimeoutHandler(delegate {
                Trace.Call(mb);
                
                _OutputTextView.ScrollMarkOnscreen(_EndMark);
                return false;
            }));
            */
            // WORKAROUND2: scroll when GTK+ mainloop is idle
            GLib.Idle.Add(new GLib.IdleHandler(delegate {
                Trace.Call(mb);
                
                _OutputTextView.ScrollMarkOnscreen(_EndMark);
                return false;
            }));
        }
        
        public virtual void Enable()
        {
            Trace.Call();
        }
        
        public virtual void Disable()
        {
            Trace.Call();
        }
        
        public virtual void Sync()
        {
            Trace.Call();
            
#if LOG4NET
            _Logger.Debug("Sync() syncing messages");
#endif
            // sync messages
            // cleanup, be sure the output is empty
            _OutputTextView.Buffer.Clear();
            IList<MessageModel> messages = _ChatModel.Messages;
            if (messages.Count > 0) {
                foreach (MessageModel msg in messages) {
                    AddMessage(msg);
                }
            }
        }
        
        public virtual void AddMessage(MessageModel msg)
        {
            Trace.Call(msg);
            
            string timestamp = null;
            try {
                string format = (string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"];
                if (!String.IsNullOrEmpty(format)) {
                    timestamp = msg.TimeStamp.ToLocalTime().ToString(format);
                }
            } catch (FormatException e) {
                timestamp = "Timestamp Format ERROR: " + e.Message;
            }
            
            Gtk.TextIter iter = _OutputTextView.Buffer.EndIter;
            if (timestamp != null) {
                _OutputTextView.Buffer.Insert(ref iter, timestamp + " ");
            }
            
            bool hasHighlight = false;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
#if LOG4NET
                _Logger.Debug("AddMessage(): msgPart.GetType(): " + msgPart.GetType());
#endif
                if (msgPart.IsHighlight) {
                    hasHighlight = true;
                }
                
                Gdk.Color bgColor = _OutputTextView.DefaultAttributes.Appearance.BgColor;
                if (_BackgroundColor != null) {
                    bgColor = _BackgroundColor.Value;
                }
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
                    _OutputTextView.Buffer.InsertWithTagsByName(ref iter, fmsgui.Url, "url");
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
                    
                    _OutputTextView.Buffer.InsertWithTagsByName(ref iter,
                                                                fmsgti.Text,
                                                                tags.ToArray());
                } 
            }
            _OutputTextView.Buffer.Insert(ref iter, "\n");
            
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
        
        public virtual void Clear()
        {
            Trace.Call();
            
            _OutputTextView.Buffer.Clear();
        }
        
        public virtual void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);
            
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            
            string bgStr = (string) config["Interface/Chat/BackgroundColor"];
            if (!String.IsNullOrEmpty(bgStr)) {
                Gdk.Color bgColor = Gdk.Color.Zero;
                if (Gdk.Color.Parse(bgStr, ref bgColor)) {
                    _OutputTextView.ModifyBase(Gtk.StateType.Normal, bgColor);
                    _BackgroundColor = bgColor;
                }
            } else {
                _OutputTextView.ModifyBase(Gtk.StateType.Normal);
                _BackgroundColor = null;
            }
            
            string fgStr = (string) config["Interface/Chat/ForegroundColor"];
            if (!String.IsNullOrEmpty(fgStr)) {
                Gdk.Color fgColor = Gdk.Color.Zero;
                if (Gdk.Color.Parse(fgStr, ref fgColor)) {
                    _OutputTextView.ModifyText(Gtk.StateType.Normal, fgColor);
                    _ForegroundColor = fgColor;
                }
            } else {
                _OutputTextView.ModifyText(Gtk.StateType.Normal);
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
            
            _OutputTextView.ModifyFont(_FontDescription);
            
            string wrapModeStr = (string) config["Interface/Chat/WrapMode"];
            if (!String.IsNullOrEmpty(wrapModeStr)) {
                Gtk.WrapMode wrapMode = (Gtk.WrapMode) Enum.Parse(typeof(Gtk.WrapMode), wrapModeStr);
                _OutputTextView.WrapMode = wrapMode;
            }
        }
        
        public virtual void Close()
        {
            Trace.Call();
        }
        
        private string _GetTextTagName(TextColor fgColor, TextColor bgColor)
        {
             string hexcode;
             string tagname;
             if (fgColor != null) {
                hexcode = fgColor.HexCode;
                tagname = "fg_color:" + hexcode;
             } else if (bgColor != null) {
                hexcode = bgColor.HexCode;
                tagname = "bg_color:" + hexcode;
             } else {
                return null;
             }
             
             if (_OutputTextTagTable.Lookup(tagname) == null) {
                 int red   = Int16.Parse(hexcode.Substring(0, 2), NumberStyles.HexNumber);
                 int green = Int16.Parse(hexcode.Substring(2, 2), NumberStyles.HexNumber);
                 int blue  = Int16.Parse(hexcode.Substring(4, 2), NumberStyles.HexNumber);
                 Gdk.Color c = new Gdk.Color((byte)red, (byte)green, (byte)blue);
                 Gtk.TextTag tt = new Gtk.TextTag(tagname);
                 if (fgColor != null) {
                    tt.ForegroundGdk = c;
                 } else if (bgColor != null) {
                    tt.BackgroundGdk = c;
                 }
#if LOG4NET
                 _Logger.Debug("_GetTextTagName(): adding: " + tagname + " to _OutputTextTagTable");
#endif
                 _OutputTextTagTable.Add(tt);
             }
             return tagname;
        }
                        
        private void _OnTextBufferChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
        
            Gtk.ScrolledWindow sw = _OutputScrolledWindow;
            Gtk.TextView tv = _OutputTextView;
            
            if (sw.Vadjustment.Upper == (sw.Vadjustment.Value + sw.Vadjustment.PageSize)) {
                // the scrollbar is way at the end, lets autoscroll
                Gtk.TextIter endit = tv.Buffer.EndIter;
                tv.Buffer.PlaceCursor(endit);
                tv.Buffer.MoveMark(tv.Buffer.InsertMark, endit);
                tv.ScrollMarkOnscreen(tv.Buffer.InsertMark);
            }
            
            int buffer_lines = (int)Frontend.UserConfig["Interface/Notebook/BufferLines"];
            if (tv.Buffer.LineCount > buffer_lines) {
                Gtk.TextIter start_iter = tv.Buffer.StartIter; 
                // TODO: maybe we should delete chunks instead of each line
                Gtk.TextIter end_iter = tv.Buffer.GetIterAtLine(tv.Buffer.LineCount - buffer_lines);
                tv.Buffer.Delete(ref start_iter, ref end_iter);
            }

            // update the end mark
            tv.Buffer.MoveMark(_EndMark, tv.Buffer.EndIter);
        }
        
        private void _OnTextTagUrlTextEvent(object sender, Gtk.TextEventArgs e)
        {
            if (e.Event.Type != Gdk.EventType.ButtonRelease) {
                return;
            }
            
            Gtk.TextIter start = Gtk.TextIter.Zero;
            Gtk.TextIter end = Gtk.TextIter.Zero;

            // if something in the textview is selected, bail out
            if (HasTextViewSelection) {
                return;
            }
            
            // get URL via TextTag from TextIter
            Gtk.TextTag tag = (Gtk.TextTag) sender;
            
            start = e.Iter;
            start.BackwardToTagToggle(tag);
            end = e.Iter;
            end.ForwardToTagToggle(tag);
            string url = _OutputTextView.Buffer.GetText(start, end, false);
            
            if (!Regex.IsMatch(url, @"^[a-zA-Z0-9\-]+:\/\/")) {
                // URL doesn't start with a protocol
                url = "http://" + url;
            }
            
            if (Type.GetType("Mono.Runtime") == null) {
                // this is not Mono, probably MS .NET, so ShellExecute is the better approach
                ThreadPool.QueueUserWorkItem(delegate {
                    SysDiag.Process.Start(url);
                });
                return;
            }
            
#if UI_GNOME
            try {
                GNOME.Url.Show(url);
            } catch (Exception ex) {
                string msg = String.Format(_("Opening URL ({0}) failed."), url);
                Frontend.ShowException(new ApplicationException(msg, ex));
            }
#else
            // hopefully Mono finds some way to handle the URL
            ThreadPool.QueueUserWorkItem(delegate {
                SysDiag.Process.Start(url);
            });
#endif
        }
        
        private void _OnMotionNotifyEvent(object sender, Gtk.MotionNotifyEventArgs e)
        {
            // GDK is ugly!
            Gdk.ModifierType modifierType;
            int windowX, windowY;
            int bufferX, bufferY;
            
            // get the window position of the mouse
            _OutputTextView.GdkWindow.GetPointer(out windowX, out windowY, out modifierType);
            // get buffer position with the window position
            _OutputTextView.WindowToBufferCoords(Gtk.TextWindowType.Widget,
                                                 windowX, windowY,
                                                 out bufferX, out bufferY);
            // get TextIter with buffer position
            Gtk.TextIter iter = _OutputTextView.GetIterAtLocation(bufferX, bufferY);
            bool atUrlTag = false;
            foreach (Gtk.TextTag tag in iter.Tags) {
                if (tag.Name == "url") {
                    atUrlTag = true;
                    break;
                }
            }
            
            Gdk.Window window = _OutputTextView.GetWindow(Gtk.TextWindowType.Text); 
            if (atUrlTag != _AtUrlTag) {
                _AtUrlTag = atUrlTag;
                
                if (atUrlTag) {
#if LOG4NET
                    _Logger.Debug("_OnMotionNotifyEvent(): at url tag");
#endif
                    window.Cursor = _LinkCursor;
                } else {
#if LOG4NET
                    _Logger.Debug("_OnMotionNotifyEvent(): not at url tag");
#endif
                    window.Cursor = _NormalCursor;
                }
            }
        }
        
        protected virtual void OnTabButtonPress(object sender, Gtk.ButtonPressEventArgs e)
        {
            Trace.Call(sender, e);
            
            if (e.Event.Button == 3) {
                _TabMenu.Popup(null, null, null, e.Event.Button, e.Event.Time);
                _TabMenu.ShowAll();
            } else if (e.Event.Button == 2) {
                Close();
            }
        }
        
        protected virtual void OnTabMenuCloseActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            Close();
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
