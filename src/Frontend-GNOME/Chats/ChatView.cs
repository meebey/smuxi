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

using System;
using System.Drawing;
using SysDiag = System.Diagnostics;
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
    public abstract class ChatView : Gtk.EventBox, IChatView
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private static readonly Gdk.Cursor _NormalCursor = new Gdk.Cursor(Gdk.CursorType.Xterm);
        private static readonly Gdk.Cursor _LinkCursor = new Gdk.Cursor(Gdk.CursorType.Hand2);
        private   bool               _AtUrlTag;
        private   ChatModel          _ChatModel;
        private   bool               _HasHighlight;
        private   Gtk.TextMark       _EndMark;
        protected Gtk.Label          _Label;
        protected Gtk.EventBox       _LabelEventBox;
        protected Gtk.ScrolledWindow _OutputScrolledWindow;
        protected Gtk.TextView       _OutputTextView;
        protected Gtk.TextTagTable   _OutputTextTagTable;
        
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
            }
        }

        public Gtk.Label Label {
            get {
                return _Label;
            }
            set {
                _Label = value;
            }
        }
    
        public Gtk.EventBox LabelEventBox {
            get {
                return _LabelEventBox;
            }
        }
        
        public Gtk.TextView OutputTextView {
            get {
                return _OutputTextView;
            }
        }
        
        public Gtk.TextTagTable OutputTextTagTable {
            get {
                return _OutputTextTagTable;
            }
        }
        
        public ChatView(ChatModel chat)
        {
            _ChatModel = chat;
            _LabelEventBox = new Gtk.EventBox();
            _LabelEventBox.VisibleWindow = false;
            
            Name = chat.Name;
            
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
            //tv.WrapMode = Gtk.WrapMode.WordChar;
            tv.WrapMode = Gtk.WrapMode.Char;
            tv.Buffer.Changed += new EventHandler(_OnTextBufferChanged);
            _OutputTextView = tv;
            
            Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
            //sw.HscrollbarPolicy = Gtk.PolicyType.Never;
            sw.HscrollbarPolicy = Gtk.PolicyType.Automatic;
            sw.VscrollbarPolicy = Gtk.PolicyType.Always;
            sw.ShadowType = Gtk.ShadowType.In;
            sw.Add(_OutputTextView);
            _OutputScrolledWindow = sw;
            
            _OutputTextView.MotionNotifyEvent += new Gtk.MotionNotifyEventHandler(_OnMotionNotifyEvent);
        }
    
        public void ScrollUp()
        {
            Trace.Call();

            Gtk.Adjustment adj = _OutputScrolledWindow.Vadjustment;
            adj.Value -= adj.PageSize - adj.StepIncrement;
        }
        
        public void ScrollDown()
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
        
        public void ScrollToStart()
        {
            Trace.Call();
            
            Gtk.Adjustment adj = _OutputScrolledWindow.Vadjustment;
            adj.Value = adj.Lower;
        }
        
        public void ScrollToEnd()
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
            // WORKAROUND: scroll after one second delay
            /*
            GLib.Timeout.Add(1000, new GLib.TimeoutHandler(delegate {
                Trace.Call(mb);
                
                _OutputTextView.ScrollMarkOnscreen(_EndMark);
                return false;
            }));
            */
            // WORKAROUND: scroll when GTK+ mainloop is idle
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
            
            Gtk.TextIter iter = _OutputTextView.Buffer.EndIter;
            _OutputTextView.Buffer.Insert(ref iter, timestamp + " ");
            
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
                    _OutputTextView.Buffer.InsertWithTagsByName(ref iter, fmsgui.Url, "url");
                } else if (msgPart is TextMessagePartModel) {
                    TextMessagePartModel fmsgti = (TextMessagePartModel) msgPart;
#if LOG4NET
                    _Logger.Debug("AddMessage(): fmsgti.Text: '" + fmsgti.Text + "'");
#endif
                    List<string> tags = new List<string>();
                    
                    if (fmsgti.ForegroundColor.HexCode != -1) {
                        // TODO: if the color is too near our background color,
                        // we should invert it
                        // use HSV
                        Gdk.Color bgcolor = _OutputTextView.DefaultAttributes.Appearance.BgColor;
                        /*
                        if (
                        bgcolor.Red
                        bgcolor.Green
                        bgcolor.Blue
                        
                        TextColor color;
                        if () {
                           color = -fmsgti.ForegroundColor;
                        } else {
                           color = fmsgti.ForegroundColor;
                        }
                        
                        System.Drawing.Color color = Color.FromArgb(0, (int)bgcolor.Red, (int)bgcolor.Green, (int)bgcolor.Blue);
                        */
                        string tagname = _GetTextTagName(fmsgti.ForegroundColor, null);
                        tags.Add(tagname);
                    }
                    if (fmsgti.BackgroundColor.HexCode != -1) {
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
                if (Frontend.UserConfig["Sound/BeepOnHighlight"] != null &&
                    (bool)Frontend.UserConfig["Sound/BeepOnHighlight"]) {
                    Frontend.MainWindow.Display.Beep();
                }
            }
            
            // HACK: out of scope?
            if (Frontend.MainWindow.Notebook.CurrentChatView != this) {
                string color = null;
                if (hasHighlight) {
                    _HasHighlight = hasHighlight;
                    color = (string) Frontend.UserConfig["Interface/Notebook/Tab/HighlightColor"];
                } else if (!_HasHighlight) {
                    color = (string) Frontend.UserConfig["Interface/Notebook/Tab/ActivityColor"];
                }
                
                if (color != null) {
                    _Label.Markup = String.Format("<span foreground=\"{0}\">{1}</span>", color, _ChatModel.Name);
                }
            }
        }
        
        private string _GetTextTagName(TextColor fg_color, TextColor bg_color)
        {
             string hexcode;
             string tagname;
             if (fg_color != null) {
                hexcode = fg_color.HexCode.ToString("X6");
                tagname = "fg_color:" + hexcode;
             } else if (bg_color != null) {
                hexcode = bg_color.HexCode.ToString("X6");
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
                 if (fg_color != null) {
                    tt.ForegroundGdk = c;
                 } else if (bg_color != null) {
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
            
#if GTK_SHARP_2_10
            // if something is selected, bail out
            if (_OutputTextView.Buffer.HasSelection) {
                return;
            }
#endif
            
            // get URL via TextTag from TextIter
            Gtk.TextTag tag = (Gtk.TextTag) sender;
            
            Gtk.TextIter start = e.Iter;
            Gtk.TextIter end = e.Iter;
            
            start.BackwardToTagToggle(tag);
            end.ForwardToTagToggle(tag);
            string url = _OutputTextView.Buffer.GetText(start, end, false);
            
            if (!Regex.IsMatch(url, @"^[a-zA-Z0-9\-]+:\/\/")) {
                // URL doesn't start with a protocol
                url = "http://" + url;
            }
            
            if (Type.GetType("Mono.Runtime") == null) {
                // this is not Mono, probably MS .NET, so ShellExecute is the better approach
                SysDiag.Process.Start(url);
                return;
            }
            
#if UI_GNOME
            try {
                GNOME.Url.Show(url);
            } catch (Exception ex) {
                string msg = String.Format(_("Opening URL ({0}) failed."), url);
                Frontend.ShowException(new ApplicationException(msg, ex));
            }
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
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
