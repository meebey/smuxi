/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2009-2015 Mirco Bauer <meebey@meebey.net>
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
using System.IO;
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
        static readonly Regex NickRegex = new Regex("^(<([^ ]+)> )");
        static bool IsGtk2_17 { get; set; }
        private Gtk.TextTagTable _MessageTextTagTable;
        private MessageModel _LastMessage;
        private bool         _ShowTimestamps;
        private bool         _ShowHighlight;
        private bool         _ShowMarkerline;
        private bool         _AtLinkTag;
        private Uri          _ActiveLink;
        private ThemeSettings _ThemeSettings;
        private Gdk.Color    _MarkerlineColor = new Gdk.Color(255, 0, 0);
        private int          _MarkerlineBufferPosition;
        private int          _BufferLines = -1;

        IconCache EmojiCache { get; set; }
        Gtk.TextTag BoldTag { get; set; }
        Gtk.TextTag ItalicTag { get; set; }
        Gtk.TextTag UnderlineTag { get; set; }
        Gtk.TextTag LinkTag { get; set; }
        Gtk.TextTag EventTag { get; set; }

        Gtk.TextTag PersonTag { get; set; }
        bool AtPersonTag { get; set; }

        public event MessageTextViewMessageAddedEventHandler       MessageAdded;
        public event MessageTextViewMessageHighlightedEventHandler MessageHighlighted;
        public event EventHandler<MessageTextViewPersonClickedEventArgs> PersonClicked;

        public int MarkerlineBufferPosition {
            get {
                return _MarkerlineBufferPosition;
            }
            set {
                _MarkerlineBufferPosition = value;
            }
        }

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

        public bool ShowMarkerline {
            get {
                return _ShowMarkerline;
            }
            set {
                _ShowMarkerline = value;
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

        public bool IsEmpty {
            get {
                return Buffer.CharCount == 0;
            }
        }

        public bool IsAtUrlTag {
            get {
                return _AtLinkTag;
            }
        }

        public MessageModel LastMessage {
            get {
                return _LastMessage;
            }
        }

        Gdk.Color BackgroundColor {
            get {
                if (_ThemeSettings.BackgroundColor == null) {
                    return DefaultAttributes.Appearance.BgColor;
                }
                return _ThemeSettings.BackgroundColor.Value;
            }
        }

        static MessageTextView()
        {
            IsGtk2_17 = String.IsNullOrEmpty(Gtk.Global.CheckVersion(2, 17, 0)) &&
                        !String.IsNullOrEmpty(Gtk.Global.CheckVersion(2, 18, 0));
        }

        public MessageTextView()
        {
            Trace.Call();

            _MessageTextTagTable = BuildTagTable();
            _ThemeSettings = new ThemeSettings();

            EmojiCache = new IconCache("emoji");
            Buffer = new Gtk.TextBuffer(_MessageTextTagTable);
            MotionNotifyEvent += OnMotionNotifyEvent;
            PopulatePopup += OnPopulatePopup;
            ExposeEvent += OnExposeEvent;
            Realized += delegate {
                CheckStyle();
            };
            StyleSet += delegate(object o, Gtk.StyleSetArgs args) {
                if (!IsRealized) {
                    // HACK: avoid GTK+ crash in gtk_text_attributes_copy_values()
                    return;
                }
                CheckStyle();
            };
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
                Gtk.WrapMode wrapMode = (Gtk.WrapMode) Enum.Parse(
                    typeof(Gtk.WrapMode),
                    wrapModeStr
                );
                if (wrapMode == Gtk.WrapMode.Word) {
                    wrapMode = Gtk.WrapMode.WordChar;
                }
                WrapMode = wrapMode;
            }

            _BufferLines = (int) config["Interface/Notebook/BufferLines"];

#if LOG4NET
            DateTime start = DateTime.UtcNow;
#endif

            ResizeEmoji();

#if LOG4NET
            DateTime stop = DateTime.UtcNow;
            double duration = stop.Subtract(start).TotalMilliseconds;
            _Logger.Debug("ApplyConfig(): ResizeEmoji()" +
                " done, took: " + Math.Round(duration) + " ms");
#endif
        }

        void ResizeEmoji()
        {
            var buffer = Buffer;

            int width, height;
            int descent;
            using (var layout = CreatePangoLayout(null)) {
                layout.GetPixelSize(out width, out height);
                descent = layout.Context.GetMetrics(layout.FontDescription, null).Descent;
            }

            _MessageTextTagTable.Foreach((tag) => {
                if (!(tag is EmojiTag)) {
                    return;
                }

                var emojiTag = tag as EmojiTag;
                tag.Rise = -descent;
                var pix = new Gdk.Pixbuf(emojiTag.Path, -1, height);

                var beforeIter = buffer.GetIterAtMark(emojiTag.Mark);
                var afterIter = beforeIter;
                afterIter.ForwardToTagToggle(tag);
                buffer.RemoveTag(tag, beforeIter, afterIter);
                buffer.Delete(ref beforeIter, ref afterIter);
                buffer.InsertPixbuf(ref beforeIter, pix);
                // after all that, we need to re-apply the tag to the buffer
                afterIter = beforeIter;
                beforeIter = Buffer.GetIterAtMark(emojiTag.Mark);
                buffer.ApplyTag(tag, beforeIter, afterIter);
            });
        }

        void CheckStyle()
        {
            Trace.Call();

            var bgTextColor = ColorConverter.GetTextColor(BackgroundColor);
            // get best contrast for the event font color
            Gdk.Color eventColor = Gdk.Color.Zero;
            Gdk.Color.Parse("darkgray", ref eventColor);
            var eventTextColor = TextColorTools.GetBestTextColor(
                ColorConverter.GetTextColor(eventColor),
                bgTextColor,
                TextColorContrast.High
            );
            EventTag.ForegroundGdk = ColorConverter.GetGdkColor(
                eventTextColor
            );

            // get best contrast for the link font color
            Gdk.Color linkColor = Gdk.Color.Zero;
            Gdk.Color.Parse("darkblue", ref linkColor);
            var linkTextColor = TextColorTools.GetBestTextColor(
                ColorConverter.GetTextColor(linkColor),
                bgTextColor
            );
            LinkTag.ForegroundGdk = ColorConverter.GetGdkColor(
                linkTextColor
            );
        }

        public void Clear()
        {
            Trace.Call();
            
            Buffer.Clear();
        }

        static void AddAlternativeText(Gtk.TextBuffer buffer, ref Gtk.TextIter iter, ImageMessagePartModel imgPart)
        {
            if (!String.IsNullOrEmpty(imgPart.AlternativeText)) {
                buffer.Insert(ref iter, imgPart.AlternativeText);
            }
        }

        void AddEmoji(Gtk.TextBuffer buffer, ref Gtk.TextIter iter, ImageMessagePartModel imgPart, string shortName)
        {
            var unicode = Emojione.ShortnameToUnicode(shortName);
            if (unicode == null) {
                AddAlternativeText(buffer, ref iter, imgPart);
                return;
            }

            // A mark here serves two pusposes. One is to allow us to apply the
            // tag across the pixbuf. It also lets us know later where to put
            // the pixbuf if we need to load it from the network
            var mark = new Gtk.TextMark(null, true);
            buffer.AddMark(mark, iter);

            var emojiName = unicode + ".png";
            string emojiPath;
            if (EmojiCache.TryGetIcon("emojione", emojiName, out emojiPath)) {
                var emojiFile = new FileInfo(emojiPath);
                if (emojiFile.Exists && emojiFile.Length > 0) {
                    AddEmoji(buffer, ref iter, imgPart, shortName, mark, emojiPath);
                } else {
                    AddAlternativeText(buffer, ref iter, imgPart);
                }

                return;
            }

            var emojiUrl = Emojione.UnicodeToUrl(unicode);
            EmojiCache.BeginDownloadFile("emojione", emojiName, emojiUrl,
                (path) => {
                    GLib.Idle.Add(delegate {
                        var markIter = buffer.GetIterAtMark(mark);
                        AddEmoji(buffer, ref markIter, imgPart, shortName, mark, path);
                        return false;
                    });
                },
                (ex) => {
                    GLib.Idle.Add(delegate {
                        var markIter = buffer.GetIterAtMark(mark);
                        buffer.DeleteMark(mark);
                        AddAlternativeText(buffer, ref markIter, imgPart);
                        return false;
                    });
                }
            );
        }

        void AddEmoji(Gtk.TextBuffer buffer, ref Gtk.TextIter iter,
                      ImageMessagePartModel imgPart, string shortName,
                      Gtk.TextMark emojiMark , string imagePath)
        {
            int width, height;
            int widthPango, heightPango;
            int descent;
            using (var layout = CreatePangoLayout(null)) {
                layout.GetPixelSize(out width, out height);
                layout.GetSize(out widthPango, out heightPango);
                descent = layout.Context.GetMetrics(layout.FontDescription, null).Descent;
            }

            Gdk.Pixbuf emojiPixBuf;
            try {
                emojiPixBuf = new Gdk.Pixbuf(imagePath, -1, height);
            } catch (Exception ex) {
#if LOG4NET
                _Logger.ErrorFormat(
                    "AddEmoji(): error loading " +
                    "image file: '{0}' " +
                    "emoji: '{1}' into Gdk.Pixbuf(), " +
                    "Exception: {2}",
                    imagePath, shortName, ex
                );
#endif

                // delete the broken image file, maybe after the
                // next download this will be a valid image
                File.Delete(imagePath);

                // show alternative text as fallback instead
                buffer.DeleteMark(emojiMark);
                AddAlternativeText(buffer, ref iter, imgPart);
                return;
            }

            buffer.InsertPixbuf(ref iter, emojiPixBuf);
            var beforeIter = buffer.GetIterAtMark(emojiMark);
            var emojiTag = new EmojiTag(emojiMark, imagePath) {
                Rise =  - descent
            };
            _MessageTextTagTable.Add(emojiTag);
            buffer.ApplyTag(emojiTag, beforeIter, iter);
        }

        public void AddMessage(MessageModel msg)
        {
            AddMessage(msg, true);
        }
        
        public void AddMessage(MessageModel msg, bool addLinebreak)
        {
            AddMessage(msg, addLinebreak, _ShowTimestamps);
        }

        public void AddMessage(MessageModel msg, bool addLinebreak, bool showTimestamps)
        {
#if MSG_DEBUG
            Trace.Call(msg, addLinebreak);
#endif

            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            var buffer = Buffer;
            var iter = buffer.EndIter;
            var startMark = new Gtk.TextMark(null, true);
            buffer.AddMark(startMark, iter);


            var senderPrefixWidth = GetSenderPrefixWidth(msg);
            Gtk.TextTag indentTag = null;
            if (senderPrefixWidth != 0) {
                // TODO: re-use text tags that have the same indent width
                indentTag = new Gtk.TextTag(null) {
                    Indent = -senderPrefixWidth
                };
                _MessageTextTagTable.Add(indentTag);
            }

            if (showTimestamps) {
                var msgTimeStamp = msg.TimeStamp.ToLocalTime();
                if (_LastMessage != null) {
                    var lastMsgTimeStamp = _LastMessage.TimeStamp.ToLocalTime();
                    var span = msgTimeStamp.Date - lastMsgTimeStamp.Date;
                    if (span.Days > 0) {
                        var dayLine = new MessageBuilder().
                            AppendEventPrefix();
                        if (span.Days > 1) {
                            dayLine.AppendText(_("Day changed from {0} to {1}"),
                                               lastMsgTimeStamp.ToShortDateString(),
                                               msgTimeStamp.ToShortDateString());
                        } else {
                            dayLine.AppendText(_("Day changed to {0}"),
                                               msgTimeStamp.ToLongDateString());
                        }
                        dayLine.AppendText("\n");
                        var dayLineMsg = dayLine.ToMessage().ToString();
                        Buffer.InsertWithTags(ref iter, dayLineMsg, EventTag);
                    }
                }
                
                string timestamp = null;
                try {
                    string format = (string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"];
                    if (!String.IsNullOrEmpty(format)) {
                        timestamp = msgTimeStamp.ToString(format);
                    }
                } catch (FormatException e) {
                    timestamp = "Timestamp Format ERROR: " + e.Message;
                }

                if (timestamp != null) {
                    timestamp = String.Format("{0} ", timestamp);
                    buffer.Insert(ref iter, timestamp);

                    // apply timestamp width to indent tag
                    if (indentTag != null) {
                        indentTag.Indent -= GetPangoWidth(timestamp);
                    }
                }
            }

            var msgStartMark = new Gtk.TextMark(null, true);
            buffer.AddMark(msgStartMark, iter);

            bool hasHighlight = false;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
                // supposed to be used only in a ChatView
                if (msgPart.IsHighlight) {
                    hasHighlight = true;
                }

                // TODO: implement all types
                if (msgPart is UrlMessagePartModel) {
                    var urlPart = (UrlMessagePartModel) msgPart;
                    var linkText = urlPart.Text ?? urlPart.Url;

                    Uri uri;
                    try {
                        uri = new Uri(urlPart.Url);
                    } catch (UriFormatException ex) {
#if LOG4NET
                        _Logger.Error("AddMessage(): Invalid URL: " + urlPart.Url, ex);
#endif
                        buffer.Insert(ref iter, linkText);
                        continue;
                    }

                    var tags = new List<Gtk.TextTag>();
                    // link URI tag
                    var linkTag = new LinkTag(uri);
                    linkTag.TextEvent += OnLinkTagTextEvent;
                    _MessageTextTagTable.Add(linkTag);
                    tags.Add(linkTag);

                    // link style tag
                    tags.Add(LinkTag);

                    buffer.InsertWithTags(ref iter, linkText, tags.ToArray());
                } else if (msgPart is TextMessagePartModel) {
                    var tags = new List<Gtk.TextTag>();
                    TextMessagePartModel fmsgti = (TextMessagePartModel) msgPart;
                    if (fmsgti.Text == null) {
                        // Gtk.TextBuffer.Insert*() asserts on text == NULL
                        continue;
                    }
                    if (fmsgti.ForegroundColor != TextColor.None) {
                        var bg = ColorConverter.GetTextColor(BackgroundColor);
                        if (fmsgti.BackgroundColor != TextColor.None) {
                            bg = fmsgti.BackgroundColor;
                        }
                        TextColor color = TextColorTools.GetBestTextColor(
                            fmsgti.ForegroundColor, bg
                        );
                        string tagname = GetTextTagName(color, null);
                        var tag = _MessageTextTagTable.Lookup(tagname);
                        tags.Add(tag);
                    }
                    if (fmsgti.BackgroundColor != TextColor.None) {
                        // TODO: get this from ChatView
                        string tagname = GetTextTagName(null, fmsgti.BackgroundColor);
                        var tag = _MessageTextTagTable.Lookup(tagname);
                        tags.Add(tag);
                    }
                    if (fmsgti.Underline) {
#if LOG4NET && MSG_DEBUG
                        _Logger.Debug("AddMessage(): fmsgti.Underline is true");
#endif
                        tags.Add(UnderlineTag);
                    }
                    if (fmsgti.Bold) {
#if LOG4NET && MSG_DEBUG
                        _Logger.Debug("AddMessage(): fmsgti.Bold is true");
#endif
                        tags.Add(BoldTag);
                    }
                    if (fmsgti.Italic) {
#if LOG4NET && MSG_DEBUG
                        _Logger.Debug("AddMessage(): fmsgti.Italic is true");
#endif
                        tags.Add(ItalicTag);
                    }

                    if (tags.Count > 0) {
                        buffer.InsertWithTags(ref iter, fmsgti.Text, tags.ToArray());
                    } else {
                        buffer.Insert(ref iter, fmsgti.Text);
                    }
                } else if (msgPart is ImageMessagePartModel) {
                    var imgpart = (ImageMessagePartModel) msgPart;
                    Uri uri = null;
                    string scheme = null;
                    try {
                        uri = new Uri(imgpart.ImageFileName);
                        scheme = uri.Scheme;
                    } catch (UriFormatException) {
                        AddAlternativeText(buffer, ref iter, imgpart);
                    }
                    switch (scheme) {
                        case "smuxi-emoji":
                            var shortName = uri.Host;
                            AddEmoji(buffer, ref iter, imgpart, shortName);
                            break;
                        default:
                            AddAlternativeText(buffer, ref iter, imgpart);
                            break;
                    }
                }
            }
            var startIter = buffer.GetIterAtMark(startMark);
            if (msg.MessageType == MessageType.Event) {
                buffer.ApplyTag(EventTag, startIter, iter);
            }
            if (indentTag != null) {
                buffer.ApplyTag(indentTag, startIter, iter);
            }
            var nick = msg.GetNick();
            if (nick != null) {
                // TODO: re-use the same person tag for the same nick
                var personTag = new PersonTag(nick, nick);
                personTag.TextEvent += OnPersonTagTextEvent;
                _MessageTextTagTable.Add(personTag);

                var msgStartIter = buffer.GetIterAtMark(msgStartMark);
                var nickEndIter = msgStartIter;
                nickEndIter.ForwardChars(nick.Length + 2);
                buffer.ApplyTag(PersonTag, msgStartIter, nickEndIter);
                buffer.ApplyTag(personTag, msgStartIter, nickEndIter);
            }
            buffer.DeleteMark(startMark);
            buffer.DeleteMark(msgStartMark);
            if (addLinebreak) {
                buffer.Insert(ref iter, "\n");
            }

            CheckBufferSize();

            if (IsGtk2_17) {
                // HACK: force a redraw of the widget, as for some reason
                // GTK+ 2.17.6 is not redrawing some lines we add here, especially
                // for local messages. See:
                // http://projects.qnetp.net/issues/show/185
                QueueDraw();
            }
            if (Frontend.IsWindows && _LastMessage == null) {
                // HACK: workaround rendering issue on Windows where the
                // first inserted text is not showing up until the next insert
                QueueDraw();
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

        public void UpdateMarkerline()
        {
            Trace.Call();

            if (IsEmpty) {
                return;
            }

            _MarkerlineBufferPosition = Buffer.EndIter.Offset - 1;
            QueueDraw();
        }

        public override void Dispose()
        {
            // HACK: this shouldn't be needed but GTK# keeps GC handles
            // these callbacks for some reason and thus leaks :(
            _MessageTextTagTable.Foreach(tag => {
                if (tag is LinkTag) {
                    tag.TextEvent -= OnLinkTagTextEvent;
                } else if (tag is PersonTag) {
                    tag.TextEvent -= OnPersonTagTextEvent;
                }
            });
            _MessageTextTagTable.Dispose();
            base.Dispose();
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

            // WARNING: the insertion order of tags MATTERS!
            // The attributes of the text tags are applied in the order of
            // insertion to the text table, and not in which order the tags
            // applied in the buffer. This is sick IMHO.
            tt = new Gtk.TextTag("bold");
            fd = new Pango.FontDescription();
            fd.Weight = Pango.Weight.Bold;
            tt.FontDesc = fd;
            BoldTag = tt;
            ttt.Add(tt);

            tt = new Gtk.TextTag("italic");
            fd = new Pango.FontDescription();
            fd.Style = Pango.Style.Italic;
            tt.FontDesc = fd;
            ItalicTag = tt;
            ttt.Add(tt);
            
            tt = new Gtk.TextTag("underline");
            tt.Underline = Pango.Underline.Single;
            UnderlineTag = tt;
            ttt.Add(tt);
            
            tt = new Gtk.TextTag("event");
            tt.Foreground = "darkgray";
            EventTag = tt;
            ttt.Add(tt);

            tt = new Gtk.TextTag("link");
            tt.Underline = Pango.Underline.Single;
            tt.Foreground = "darkblue";
            LinkTag = tt;
            ttt.Add(tt);

            tt = new Gtk.TextTag("person");
            PersonTag = tt;
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
            bool atPersonTag = false;
            foreach (Gtk.TextTag tag in iter.Tags) {
                if (tag.Name == "link") {
                    atUrlTag = true;
                    break;
                }
                if (tag.Name == "person") {
                    atPersonTag = true;
                    break;
                }
            }
            
            Gdk.Window window = GetWindow(Gtk.TextWindowType.Text); 
            if (atUrlTag != _AtLinkTag) {
                _AtLinkTag = atUrlTag;
                
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
                    _ActiveLink = null;
                }
            }
            if (atPersonTag != AtPersonTag) {
                AtPersonTag = atPersonTag;
                
                if (atPersonTag) {
#if LOG4NET
                    _Logger.Debug("OnMotionNotifyEvent(): at person tag");
#endif
                    window.Cursor = _LinkCursor;
                } else {
#if LOG4NET
                    _Logger.Debug("OnMotionNotifyEvent(): not at person tag");
#endif
                    window.Cursor = _NormalCursor;
                }
            }
        }
        
        protected virtual void OnLinkTagTextEvent(object sender, Gtk.TextEventArgs e)
        {
            // logging noise
            //Trace.Call(sender, e);
            
            // if something in the textview is selected, bail out
            if (HasTextViewSelection) {
#if LOG4NET
                _Logger.Debug("OnLinkTagTextEvent(): active selection present, bailing out...");
#endif
                return;
            }
            
            var tag = (LinkTag) sender;
            _ActiveLink = tag.Link;

            if (e.Event.Type != Gdk.EventType.ButtonRelease) {
                return;
            }

            if (_ActiveLink == null) {
#if LOG4NET
                _Logger.Warn("OnLinkTagTextEvent(): _ActiveLink is null, ignoring...");
#endif
                return;
            }

            Frontend.OpenLink(_ActiveLink);
        }

        protected virtual void OnPersonTagTextEvent(object sender, Gtk.TextEventArgs e)
        {
            // logging noise
            //Trace.Call(sender, e);
            
            // if something in the textview is selected, bail out
            if (HasTextViewSelection) {
#if LOG4NET
                _Logger.Debug("OnPersonTagTextEvent(): active selection present, bailing out...");
#endif
                return;
            }
            
            var tag = (PersonTag) sender;
            if (tag == null) {
                return;
            }

            if (e.Event.Type != Gdk.EventType.ButtonPress) {
                return;
            }

            if (PersonClicked != null) {
                PersonClicked(
                    this,
                    new MessageTextViewPersonClickedEventArgs(tag.IdentityName)
                );
            }
        }

        protected virtual void OnPopulatePopup(object sender, Gtk.PopulatePopupArgs e)
        {
            Trace.Call(sender, e);

            if (!_AtLinkTag) {
                return;
            }

            Gtk.Menu popup = e.Menu;
            // remove all items
            foreach (Gtk.Widget children in popup.Children) {
                popup.Remove(children);
            }

            Gtk.ImageMenuItem open_item = new Gtk.ImageMenuItem(Gtk.Stock.Open, null);
            open_item.Activated += delegate {
                if (_ActiveLink != null) {
                    Frontend.OpenLink(_ActiveLink);
                }
            };
            popup.Append(open_item);

            Gtk.ImageMenuItem copy_item = new Gtk.ImageMenuItem(Gtk.Stock.Copy, null);
            copy_item.Activated += delegate {
                if (_ActiveLink == null) {
                    return;
                }
                Gdk.Atom clipboardAtom = Gdk.Atom.Intern("CLIPBOARD", false);
                Gtk.Clipboard clipboard = Gtk.Clipboard.Get(clipboardAtom);
                clipboard.Text = _ActiveLink.ToString();
            };
            popup.Append(copy_item);

            popup.ShowAll();
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
                 _MessageTextTagTable.Add(tt);
             }
             return tagname;
        }
        
        void OnExposeEvent(object sender, Gtk.ExposeEventArgs e)
        {
            if (!_ShowMarkerline || _MarkerlineBufferPosition == 0) {
                return;
            }

            var window = e.Event.Window;
            var gc = new Gdk.GC(window);
            gc.RgbFgColor = _MarkerlineColor;

            var iter = Buffer.GetIterAtOffset(_MarkerlineBufferPosition);
            var location = GetIterLocation(iter);
            int last_y = location.Y + location.Height;
            // padding
            last_y += PixelsAboveLines + PixelsBelowLines / 2;

            int x, y;
            BufferToWindowCoords(Gtk.TextWindowType.Text, 0,
                                 last_y, out x, out y);

            if (y < e.Event.Area.Y) {
                return;
            }

            window.DrawLine(gc, 0, y, VisibleRect.Width, y);
        }

        void CheckBufferSize()
        {
            if (_BufferLines == -1) {
                // no limit defined
                return;
            }

            var buffer = Buffer;
            if (buffer.LineCount > _BufferLines) {
                Gtk.TextIter start_iter = buffer.StartIter;
                // TODO: maybe we should delete chunks instead of each line
                Gtk.TextIter end_iter = buffer.GetIterAtLine(buffer.LineCount -
                                                             _BufferLines);
                int offset = end_iter.Offset;

                // release tags
                var toggled_tags = new List<Gtk.TextTag>(16);
                var start_tags = start_iter.GetToggledTags(true);
                toggled_tags.AddRange(start_tags);

                var tag_iter = start_iter;
                while (tag_iter.ForwardToTagToggle(null)) {
                    if (tag_iter.Compare(end_iter) >= 0) {
                        // tag is after line end
                        break;
                    }
                    var iter_tags = tag_iter.GetToggledTags(true);
                    toggled_tags.AddRange(iter_tags);
                }

                foreach (var tag in toggled_tags) {
                    // don't remove color tags as they are shared wither other lines
                    var tagName = tag.Name;
                    if (tagName != null &&
                        (tagName.StartsWith("fg_color:") ||
                         tagName.StartsWith("bg_color:"))) {
                        continue;
                    }
                    if (tag.IndentSet || tag is LinkTag || tag is PersonTag || tag is EmojiTag) {
                        buffer.RemoveTag(tag, start_iter, end_iter);
                        _MessageTextTagTable.Remove(tag);
                        tag.Dispose();
                    }
                }

                buffer.Delete(ref start_iter, ref end_iter);

                // update markerline offset if present
                if (_MarkerlineBufferPosition != 0) {
                    _MarkerlineBufferPosition -= offset;
                    // remove markerline if it went out of buffer
                    if (_MarkerlineBufferPosition < 0) {
                        _MarkerlineBufferPosition = 0;
                    }
                }
            }
        }

        int GetSenderPrefixWidth(MessageModel msg)
        {
            // HACK: try to obtain the nickname from the message
            // TODO: extend MessageModel with Origin property
            var msgText = msg.ToString();
            var nickMatch = NickRegex.Match(msgText);
            if (nickMatch.Success) {
                // HACK: the nick can be bold
                if (msg.MessageParts.Count >= 3) {
                    // possibly colored nick, see MessageBuilder.CreateNick()
                    var prefixPart = msg.MessageParts[0];
                    var nickPart = msg.MessageParts[1];
                    var suffixPart = msg.MessageParts[2];
                    if (prefixPart.ToString() == "<" &&
                        nickPart is TextMessagePartModel &&
                        suffixPart.ToString().StartsWith(">")) {
                        // colored nick
                        var nickTextPart = (TextMessagePartModel) nickPart;
                        if (nickTextPart.Bold) {
                            return GetPangoWidth(
                                String.Format(
                                    "{0}<b>{1}</b>{2} ",
                                    GLib.Markup.EscapeText("<"),
                                    GLib.Markup.EscapeText(
                                        nickMatch.Groups[2].Value
                                    ),
                                    GLib.Markup.EscapeText(">")
                                ),
                                true
                            );
                        }
                    }
                }
                return GetPangoWidth(nickMatch.Groups[1].Value, false);
            } else {
                if (msgText.StartsWith("-!- ")) {
                    return GetPangoWidth("-!- ", false);
                }
            }
            return 0;
        }

        int GetPangoWidth(string text)
        {
            return GetPangoWidth(text, false);
        }

        int GetPangoWidth(string text, bool isMarkup)
        {
            int width, heigth;
            using (var layout = CreatePangoLayout(null)) {
                if (isMarkup) {
                    layout.SetMarkup(text);
                } else {
                    layout.SetText(text);
                }
                layout.GetPixelSize(out width, out heigth);
            }
            return width;
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

    public class MessageTextViewPersonClickedEventArgs : EventArgs
    {
        public string IdentityName { get; private set; }
        
        public MessageTextViewPersonClickedEventArgs(string identityName)
        {
            IdentityName = identityName;
        }
    }
}
