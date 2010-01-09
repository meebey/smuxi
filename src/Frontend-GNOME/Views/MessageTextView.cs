/*
 * $Id$
 *
 * Smuxi - Smart MUltipleXed Irc
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
        private bool         _AtUrlTag;
        private string       _Url;
        private UserConfig   _Config;
        private ThemeSettings _ThemeSettings;

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

        public Gtk.TextTagTable MessageTextTagTable {
            get {
                return _MessageTextTagTable;
            }
        }

        public MessageTextView()
        {
            Trace.Call();

            _MessageTextTagTable = BuildTagTable();
            _ThemeSettings = new ThemeSettings();
            
            Buffer = new Gtk.TextBuffer(_MessageTextTagTable);
            MotionNotifyEvent += OnMotionNotifyEvent;
            PopulatePopup += OnPopulatePopup;
        }

        public void ApplyConfig(UserConfig config)
        {
            _ThemeSettings = new ThemeSettings(config);
            if (_ThemeSettings.BackgroundColor == null) {
                ModifyBase(Gtk.StateType.Normal);
            } else {
                ModifyBase(Gtk.StateType.Normal, _ThemeSettings.BackgroundColor.Value);
            }
            if (_ThemeSettings.ForegroundColor == null) {
                ModifyText(Gtk.StateType.Normal);
            } else {
                ModifyText(Gtk.StateType.Normal, _ThemeSettings.ForegroundColor.Value);
            }
            ModifyFont(_ThemeSettings.FontDescription);
            
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

            Gdk.Color bgColor = DefaultAttributes.Appearance.BgColor;
            if (_ThemeSettings.BackgroundColor != null) {
                bgColor = _ThemeSettings.BackgroundColor.Value;
            }
            TextColor bgTextColor = ColorTools.GetTextColor(bgColor);

            if (_ShowTimestamps) {
                DateTime localTimestamp = msg.TimeStamp.ToLocalTime();
                if (_LastMessage != null &&
                    _LastMessage.TimeStamp.ToLocalTime().Date != localTimestamp.Date) {
                    string dayLine = String.Format(
                        "-!- " + _("Day changed to {0}"),
                        localTimestamp.Date.ToLongDateString()
                    );
                    Buffer.Insert(ref iter, dayLine + "\n");
                }
                
                string timestamp = null;
                try {
                    string format = (string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"];
                    if (!String.IsNullOrEmpty(format)) {
                        timestamp = localTimestamp.ToString(format);
                    }
                } catch (FormatException e) {
                    timestamp = "Timestamp Format ERROR: " + e.Message;
                }

                if (timestamp != null) {
                    timestamp = String.Format("{0} ", timestamp);
                    if (msg.MessageType == MessageType.Event) {
                        // get best contrast for the event font color
                        Gtk.TextTag eventTag = _MessageTextTagTable.Lookup("event");
                        Gdk.Color eventColor = eventTag.ForegroundGdk;
                        TextColor eventTextColor = ColorTools.GetBestTextColor(
                            ColorTools.GetTextColor(eventColor),
                            bgTextColor,
                            ColorContrast.High
                        );
                        eventTag.ForegroundGdk = ColorTools.GetGdkColor(
                            eventTextColor
                        );
                        Buffer.InsertWithTagsByName(ref iter, timestamp, "event");
                    } else {
                        Buffer.Insert(ref iter, timestamp);
                    }
                }
            }

            bool hasHighlight = false;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
                // supposed to be used only in a ChatView
                if (msgPart.IsHighlight) {
                    hasHighlight = true;
                }

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
                    if (msg.MessageType == MessageType.Event &&
                        fmsgti.ForegroundColor == TextColor.None) {
                        // only mark parts that don't have a color set
                        tags.Add("event");
                    }

                    if (tags.Count > 0) {
                        Buffer.InsertWithTagsByName(ref iter, fmsgti.Text, tags.ToArray());
                    } else {
                        Buffer.Insert(ref iter, fmsgti.Text);
                    }
                }
            }
            if (addLinebreak) {
                Buffer.Insert(ref iter, "\n");
            }

            // HACK: force a redraw of the widget, as for some reason
            // GTK+ 2.17.6 is not redrawing some lines we add here, especially
            // for local messages. See:
            // http://projects.qnetp.net/issues/show/185
            QueueDraw();

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

            tt = new Gtk.TextTag("event");
            tt.Foreground = "darkgray";
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
            // logging noise
            //Trace.Call(sender, e);
            
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
            _Url = Buffer.GetText(start, end, false);

            if (e.Event.Type != Gdk.EventType.ButtonRelease) {
                return;
            }

            if (String.IsNullOrEmpty(_Url)) {
#if LOG4NET
                _Logger.Warn("OnTextTagUrlTextEvent(): url is empty, ignoring...");
#endif
                return;
            }

            OpenLink(_Url);
        }

        protected virtual void OnPopulatePopup(object sender, Gtk.PopulatePopupArgs e)
        {
            Trace.Call(sender, e);

            if (!_AtUrlTag) {
                return;
            }

            Gtk.Menu popup = e.Menu;
            // remove all items
            foreach (Gtk.Widget children in popup.Children) {
                popup.Remove(children);
            }

            Gtk.ImageMenuItem open_item = new Gtk.ImageMenuItem(Gtk.Stock.Open, null);
            open_item.Activated += delegate {
                if (!String.IsNullOrEmpty(_Url)) {
                    OpenLink(_Url);
                }
            };
            popup.Append(open_item);

            Gtk.ImageMenuItem copy_item = new Gtk.ImageMenuItem(Gtk.Stock.Copy, null);
            copy_item.Activated += delegate {
                Gdk.Atom clipboardAtom = Gdk.Atom.Intern("CLIPBOARD", false);
                Gtk.Clipboard clipboard = Gtk.Clipboard.Get(clipboardAtom);
                clipboard.Text = _Url;
            };
            popup.Append(copy_item);

            popup.ShowAll();
        }

        private void OpenLink(string link)
        {
            Trace.Call(link);

            if (!Regex.IsMatch(link, @"^[a-zA-Z0-9\-]+:\/\/")) {
                // URL doesn't start with a protocol
                link = "http://" + link;
            }
            
            // hopefully MS .NET / Mono finds some way to handle the URL
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    SysDiag.Process.Start(link);
                } catch (Exception ex) {
                    // exceptions in the thread pool would kill the process, see:
                    // http://msdn.microsoft.com/en-us/library/0ka9477y.aspx
                    // http://projects.qnetp.net/issues/show/194
#if LOG4NET
                    _Logger.Error("OpenLink(): opening URL: '" + link + "' failed", ex);
#endif
                }
            });
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
