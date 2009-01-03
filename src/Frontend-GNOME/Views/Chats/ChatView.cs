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
        private   MessageTextView    _OutputMessageTextView;
        
        public ChatModel ChatModel {
            get {
                return _ChatModel;
            }
        }
        
        public bool HasHighlight {
            get {
                return _OutputMessageTextView.HasHighlight;
            }
        }
        
//            set {
//                _HasHighlight = value;
//                
//                if (!value) {
//                    // clear highlight with "no activity"
//                    HasActivity = false;
//                    return;
//                }
//                
//                string color = (string) Frontend.UserConfig["Interface/Notebook/Tab/HighlightColor"];
//                _TabLabel.Markup = String.Format("<span foreground=\"{0}\">{1}</span>", color, _Name);
//            }
//        }
        
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
        
        public virtual bool HasSelection {
            get {
                return HasTextViewSelection;
            }
        }
        
        public virtual new bool HasFocus {
            get {
                return base.HasFocus || _OutputMessageTextView.HasFocus;
            }
            set {
                _OutputMessageTextView.HasFocus = value;
            }
        }
        
        public Gtk.Widget LabelWidget {
            get {
                return _TabEventBox;
            }
        }
        
        public MessageTextView OutputMessageTextView {
            get {
                return _OutputMessageTextView;
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

        public ChatView(ChatModel chat)
        {
            Trace.Call(chat);
            
            _ChatModel = chat;
            _Name = _ChatModel.Name;
            Name = _Name;
            
            MessageTextView tv = new MessageTextView(this);
            _EndMark = tv.Buffer.CreateMark("end", tv.Buffer.EndIter, false); 
            tv.Editable = false;
            //tv.CursorVisible = false;
            tv.CursorVisible = true;
            tv.WrapMode = Gtk.WrapMode.Char;
            tv.Buffer.Changed += new EventHandler(_OnTextBufferChanged);
            tv.MotionNotifyEvent += new Gtk.MotionNotifyEventHandler(_OnMotionNotifyEvent);
            _OutputMessageTextView = tv;
            
            Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
            //sw.HscrollbarPolicy = Gtk.PolicyType.Never;
            sw.HscrollbarPolicy = Gtk.PolicyType.Automatic;
            sw.VscrollbarPolicy = Gtk.PolicyType.Always;
            sw.ShadowType = Gtk.ShadowType.In;
            sw.Add(_OutputMessageTextView);
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
            _OutputMessageTextView.Buffer.Clear();
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
            
            _OutputMessageTextView.ApplyConfig(config);
        }
        
        public virtual void Close()
        {
            Trace.Call();
        }
        
        internal string _GetTextTagName(TextColor fgColor, TextColor bgColor)
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
        
        internal void _OnTextTagUrlTextEvent(object sender, Gtk.TextEventArgs e)
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
