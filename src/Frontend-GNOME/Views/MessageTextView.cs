/*
 * $Id$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2009 Mirco Bauer <meebey@meebey.net>
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;
using SysDiag = System.Diagnostics;
#if UI_GNOME
using GNOME = Gnome;
#endif
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class MessageTextView : Gtk.TextView
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif        
        private static readonly Gdk.Cursor _NormalCursor = new Gdk.Cursor(Gdk.CursorType.Xterm);
        private static readonly Gdk.Cursor _LinkCursor = new Gdk.Cursor(Gdk.CursorType.Hand2);
        private Gtk.TextTagTable _MessageTextTagTable;
        private MessageModel _LastMessage;
        private bool         _ShowTimestamps;
        private bool         _ShowHighlight;
        private bool         _HasHighlight;
        private bool         _AtUrlTag;
        private UserConfig   _Config;
        private Gdk.Color?   _BackgroundColor;
        private Gdk.Color?   _ForegroundColor;
        private Pango.FontDescription _FontDescription;

        public event MessageTextViewMessageAddedEventHandler       MessageAdded;
        public event MessageTextViewMessageHighlightedEventHandler MessageHighlighted;
        
        public bool ShowTimestamps {
            get {
                return _ShowTimestamps;
            }
            set {
                _ShowTimestamps = value;
            }
        }

        public bool ShowHighlight {
            get {
                return _ShowHighlight;
            }
            set {
                _ShowHighlight = value;
            }
        }

        public bool HasHighlight
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

        public Gtk.TextTagTable MessageTextTagTable {
            get {
                return _MessageTextTagTable;
            }
        }

        public MessageTextView()
        {
            Trace.Call();

            _MessageTextTagTable = BuildTagTable();
            
            Buffer = new Gtk.TextBuffer(_MessageTextTagTable);
            MotionNotifyEvent += OnMotionNotifyEvent;
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
            
            AddMessage(msg, true);
        }
        
        public void AddMessage(MessageModel msg, bool addLinebreak)
        {
            Trace.Call(msg, addLinebreak);
            
            Gtk.TextIter iter = Buffer.EndIter;
            
            if (_ShowTimestamps) {
                if (_LastMessage != null &&
                    _LastMessage.TimeStamp.Date != msg.TimeStamp.Date) {
                    string dayLine = String.Format(
                        "-!- " + _("Day changed to {0}"),
                        msg.TimeStamp.ToLocalTime().Date.ToLongDateString()
                    );
                    Buffer.Insert(ref iter, dayLine + "\n");
                }
                
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
                    Buffer.Insert(ref iter, timestamp + " ");
                }
            }

            bool hasHighlight = false;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
#if LOG4NET
                _Logger.Debug("AddMessage(): msgPart.GetType(): " + msgPart.GetType());
#endif
                // supposed to be used only in a ChatView
                if (msgPart.IsHighlight) {
                    hasHighlight = true;
                }
                
                Gdk.Color bgColor = DefaultAttributes.Appearance.BgColor;
                if (_BackgroundColor != null) {
                    bgColor = _BackgroundColor.Value;
                }
                TextColor bgTextColor = ColorTools.GetTextColor(bgColor);
                // TODO: implement all types
                if (msgPart is UrlMessagePartModel) {
                    UrlMessagePartModel fmsgui = (UrlMessagePartModel) msgPart;
                    // HACK: the engine should set a color for us!
                    Gtk.TextTag urlTag = _MessageTextTagTable.Lookup("url");
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
                        string tagname = GetTextTagName(color, null);
                        //string tagname = _GetTextTagName(fmsgti.ForegroundColor, null);
                        tags.Add(tagname);
                    }
                    if (fmsgti.BackgroundColor != TextColor.None) {
                        // TODO: get this from ChatView
                        string tagname = GetTextTagName(null, fmsgti.BackgroundColor);
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
            if (addLinebreak) {
                Buffer.Insert(ref iter, "\n");
            }
            
            if (MessageAdded != null) {
                MessageAdded(this, new MessageTextViewMessageAddedEventArgs(msg));
            }
            
            if (hasHighlight) {
                if (MessageHighlighted != null) {
                    MessageHighlighted(this, new MessageTextViewMessageHighlightedEventArgs(msg));
                }
            }
            
            _LastMessage = msg;
        }

        /*
         * Helper methods
         */
        private Gtk.TextTagTable BuildTagTable()
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
            tt.TextEvent += OnTextTagUrlTextEvent;
            fd = new Pango.FontDescription();
            tt.FontDesc = fd;
            ttt.Add(tt);
            
            return ttt;
        }

        protected virtual void OnMotionNotifyEvent(object sender, Gtk.MotionNotifyEventArgs e)
        {
            // GDK is ugly!
            Gdk.ModifierType modifierType;
            int windowX, windowY;
            int bufferX, bufferY;
            
            // get the window position of the mouse
            GdkWindow.GetPointer(out windowX, out windowY, out modifierType);
            // get buffer position with the window position
            WindowToBufferCoords(Gtk.TextWindowType.Widget,
                                 windowX, windowY,
                                 out bufferX, out bufferY);
            // get TextIter with buffer position
            Gtk.TextIter iter = GetIterAtLocation(bufferX, bufferY);
            bool atUrlTag = false;
            foreach (Gtk.TextTag tag in iter.Tags) {
                if (tag.Name == "url") {
                    atUrlTag = true;
                    break;
                }
            }
            
            Gdk.Window window = GetWindow(Gtk.TextWindowType.Text); 
            if (atUrlTag != _AtUrlTag) {
                _AtUrlTag = atUrlTag;
                
                if (atUrlTag) {
#if LOG4NET
                    _Logger.Debug("OnMotionNotifyEvent(): at url tag");
#endif
                    window.Cursor = _LinkCursor;
                } else {
#if LOG4NET
                    _Logger.Debug("OnMotionNotifyEvent(): not at url tag");
#endif
                    window.Cursor = _NormalCursor;
                }
            }
        }
        
        protected virtual void OnTextTagUrlTextEvent(object sender, Gtk.TextEventArgs e)
        {
            Trace.Call(sender, e);
            
            if (e.Event.Type != Gdk.EventType.ButtonRelease) {
                return;
            }
            
            Gtk.TextIter start = Gtk.TextIter.Zero;
            Gtk.TextIter end = Gtk.TextIter.Zero;

            // if something in the textview is selected, bail out
            if (HasTextViewSelection) {
#if LOG4NET
                _Logger.Debug("OnTextTagUrlTextEvent(): active selection present, bailing out...");
#endif
                return;
            }
            
            // get URL via TextTag from TextIter
            Gtk.TextTag tag = (Gtk.TextTag) sender;
            
            start = e.Iter;
            start.BackwardToTagToggle(tag);
            end = e.Iter;
            end.ForwardToTagToggle(tag);
            string url = Buffer.GetText(start, end, false);
            if (String.IsNullOrEmpty(url)) {
#if LOG4NET
                _Logger.Warn("OnTextTagUrlTextEvent(): url is empty, ignoring...");
#endif
                return;
            }
            
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
        
        private string GetTextTagName(TextColor fgColor, TextColor bgColor)
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
             
             if (_MessageTextTagTable.Lookup(tagname) == null) {
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
                 _Logger.Debug("GetTextTagName(): adding: " + tagname + " to _OutputTextTagTable");
#endif
                 _MessageTextTagTable.Add(tt);
             }
             return tagname;
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
    
    public delegate void MessageTextViewMessageAddedEventHandler(object sender, MessageTextViewMessageAddedEventArgs e);
    
    public class MessageTextViewMessageAddedEventArgs : EventArgs
    {
        private MessageModel f_Message;
        
        public MessageModel Message {
            get {
                return f_Message;
            }
        }
         
        public MessageTextViewMessageAddedEventArgs(MessageModel message)
        {
            f_Message = message;
        }
    }
    
    public delegate void MessageTextViewMessageHighlightedEventHandler(object sender, MessageTextViewMessageHighlightedEventArgs e);
    
    public class MessageTextViewMessageHighlightedEventArgs : EventArgs
    {
        private MessageModel f_Message;
        
        public MessageModel Message {
            get {
                return f_Message;
            }
        }
         
        public MessageTextViewMessageHighlightedEventArgs(MessageModel message)
        {
            f_Message = message;
        }
    }
}
