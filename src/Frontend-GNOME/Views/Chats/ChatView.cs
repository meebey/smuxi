/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2014 Mirco Bauer <meebey@meebey.net>
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
using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;

namespace Smuxi.Frontend.Gnome
{
    // TODO: use Gtk.Bin
    public abstract class ChatView : Gtk.EventBox, IChatView, IDisposable, ITraceable
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        public    string             ID { get; internal set; }
        public    int                Position { get; internal set; }
        private   ChatModel          _ChatModel;
        private   bool               _HasHighlight;
        public    int                HighlightCount { get; private set; }
        private   bool               _HasActivity;
        public    int                ActivityCount { get; private set; }
        private   bool               _HasEvent;
        private   bool               _IsSynced;
        private   Gtk.TextMark       _EndMark;
        private   Gtk.Menu           _TabMenu;
        private   Gtk.Label          _TabLabel;
        private   Gtk.EventBox       _TabEventBox;
        private   Gtk.HBox           _TabHBox;
        private   Gtk.ScrolledWindow _OutputScrolledWindow;
        private   MessageTextView    _OutputMessageTextView;
        private   ThemeSettings      _ThemeSettings;
        private   TaskQueue          _LastSeenHighlightQueue;
        public    DateTime           SyncedLastSeenMessage { get; private set; }
        public    DateTime           SyncedLastSeenHighlight { get; private set; }
        IList<MessageModel>          SyncedMessages { get; set; }
        protected string             SyncedName { get; set; }
        public    IProtocolManager   ProtocolManager { get; set; }
        bool                         UseLowBandwidthMode { get; set; }
        public Gtk.Image TabImage { get; protected set; }
        bool                         IsAutoScrolling { get; set; }

        public event EventHandler<EventArgs> StatusChanged;

        public ChatModel ChatModel {
            get {
                return _ChatModel;
            }
        }

        public new string Name {
            get {
                return base.Name;
            }
            set {
                base.Name = value;
                _TabLabel.Text = value;

                OnStatusChanged(EventArgs.Empty);
            }
        }

        // this property is thread-safe
        public bool IsActive {
            get {
                // is it really safe to query a property value of glib owned
                // object?!?
                return Frontend.MainWindow.HasToplevelFocus &&
                        Object.ReferenceEquals(
                            Frontend.MainWindow.ChatViewManager.CurrentChatView,
                            this
                        );
            }
        }

        public bool HasHighlight {
            get {
                return _HasHighlight;
            }
            set {
                if (value) {
                    _HasHighlight = value;
                    HighlightCount++;
                    OnStatusChanged(EventArgs.Empty);
                } else {
                    if (_HasHighlight == value) {
                        // nothing to update
                        return;
                    }
                    _HasHighlight = value;
                    // clear highlight with "no activity"
                    HasActivity = false;
                    HighlightCount = 0;
                    OnStatusChanged(EventArgs.Empty);
                    return;
                }

                var color = TextColorTools.GetBestTextColor(
                    ColorConverter.GetTextColor(_ThemeSettings.HighlightColor),
                    ColorConverter.GetTextColor(
                        Gtk.Rc.GetStyle(_TabLabel).Base(Gtk.StateType.Normal)
                    ), TextColorContrast.High
                );

                if (HighlightCount > 1) {
                    _TabLabel.Markup = String.Format(
                        "<span foreground=\"{0}\">{1} ({2})</span>",
                        GLib.Markup.EscapeText(color.ToString()),
                        GLib.Markup.EscapeText(Name),
                        GLib.Markup.EscapeText(HighlightCount.ToString())

                    );
                } else {
                    _TabLabel.Markup = String.Format(
                        "<span foreground=\"{0}\">{1}</span>",
                        GLib.Markup.EscapeText(color.ToString()),
                        GLib.Markup.EscapeText(Name)
                    );
                }
            }
        }

        public bool HasActivity {
            get {
                return _HasActivity;
            }
            set {
                if (value) {
                    ActivityCount++;
                    OnStatusChanged(EventArgs.Empty);
                } else {
                    ActivityCount = 0;
                }

                if (_HasActivity == value) {
                    // nothing to update
                    return;
                }
                _HasActivity = value;
                OnStatusChanged(EventArgs.Empty);

                if (HasHighlight) {
                    // don't show activity if there is a highlight active
                    return;
                }

                Gdk.Color colorValue;
                if (value) {
                    colorValue = _ThemeSettings.ActivityColor;
                } else {
                    colorValue = _ThemeSettings.NoActivityColor;
                }
                var color = TextColorTools.GetBestTextColor(
                    ColorConverter.GetTextColor(colorValue),
                    ColorConverter.GetTextColor(
                        Gtk.Rc.GetStyle(_TabLabel).Base(Gtk.StateType.Normal)
                    ), TextColorContrast.High
                );
                _TabLabel.Markup = String.Format(
                    "<span foreground=\"{0}\">{1}</span>",
                    GLib.Markup.EscapeText(color.ToString()),
                    GLib.Markup.EscapeText(Name)
                );
            }
        }

        public bool HasEvent {
            get {
                return _HasEvent;
            }
            set {
                if (_HasEvent == value) {
                    // nothing to update
                    return;
                }
                _HasEvent = value;
                OnStatusChanged(EventArgs.Empty);

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

                var color = TextColorTools.GetBestTextColor(
                    ColorConverter.GetTextColor(_ThemeSettings.EventColor),
                    ColorConverter.GetTextColor(
                        Gtk.Rc.GetStyle(_TabLabel).Base(Gtk.StateType.Normal)
                    ), TextColorContrast.High
                );
                _TabLabel.Markup = String.Format(
                    "<span foreground=\"{0}\">{1}</span>",
                    GLib.Markup.EscapeText(color.ToString()),
                    GLib.Markup.EscapeText(Name)
                );
            }
        }
        
        public virtual bool HasSelection {
            get {
                return _OutputMessageTextView.HasTextViewSelection;
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

        // by default: no participants
        public virtual IList<PersonModel> Participants {
            get {
                return new List<PersonModel>();
            }
            protected set {
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

        public Gtk.Menu TabMenu {
            get {
                return _TabMenu;
            }
        }

        protected ThemeSettings ThemeSettings {
            get {
                return _ThemeSettings;
            }
        }

        protected abstract Gtk.Image DefaultTabImage {
            get;
        }

        public event EventHandler<ChatViewMessageHighlightedEventArgs> MessageHighlighted;

        public ChatView(ChatModel chat)
        {
            Trace.Call(chat);
            
            _ChatModel = chat;

            IsAutoScrolling = true;
            MessageTextView tv = new MessageTextView();
            _EndMark = tv.Buffer.CreateMark("end", tv.Buffer.EndIter, false); 
            tv.ShowTimestamps = true;
            tv.ShowMarkerline = true;
            tv.Editable = false;
            tv.CursorVisible = true;
            tv.WrapMode = Gtk.WrapMode.Char;
            tv.MessageAdded += OnMessageTextViewMessageAdded;
            tv.MessageHighlighted += OnMessageTextViewMessageHighlighted;
            tv.PopulatePopup += OnMessageTextViewPopulatePopup;
            tv.SizeRequested += delegate {
                AutoScroll();
            };
            tv.PersonClicked += OnMessageTextViewPersonClicked;
            _OutputMessageTextView = tv;

            Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow();
            _OutputScrolledWindow = sw;
            //sw.HscrollbarPolicy = Gtk.PolicyType.Never;
            sw.HscrollbarPolicy = Gtk.PolicyType.Automatic;
            sw.VscrollbarPolicy = Gtk.PolicyType.Always;
            sw.ShadowType = Gtk.ShadowType.In;
            sw.Vadjustment.ValueChanged += OnVadjustmentValueChanged;
            sw.Add(_OutputMessageTextView);

            // popup menu
            _TabMenu = new Gtk.Menu();
            _TabMenu.Shown += OnTabMenuShown;

            //FocusChild = _OutputTextView;
            //CanFocus = false;
            
            _TabLabel = new Gtk.Label();

            TabImage = DefaultTabImage;
            _TabHBox = new Gtk.HBox();
            _TabHBox.PackEnd(new Gtk.Fixed(), true, true, 0);
            _TabHBox.PackEnd(_TabLabel, false, false, 0);
            _TabHBox.PackStart(TabImage, false, false, 2);
            _TabHBox.ShowAll();
            
            _TabEventBox = new Gtk.EventBox();
            _TabEventBox.VisibleWindow = false;
            _TabEventBox.ButtonPressEvent += new Gtk.ButtonPressEventHandler(OnTabButtonPress);
            _TabEventBox.Add(_TabHBox);
            _TabEventBox.ShowAll();

            _ThemeSettings = new ThemeSettings();

            // OPT-TODO: this should use a TaskStack instead of TaskQueue
            _LastSeenHighlightQueue = new TaskQueue("LastSeenHighlightQueue("+ID+")");
            _LastSeenHighlightQueue.AbortedEvent += OnLastSeenHighlightQueueAbortedEvent;
            _LastSeenHighlightQueue.ExceptionEvent += OnLastSeenHighlightQueueExceptionEvent;
        }

        protected ChatView(IntPtr handle) : base(handle)
        {
        }
        
        ~ChatView()
        {
            Trace.Call();

            Dispose(false);
        }

        public override void Dispose()
        {
            Trace.Call();

            Dispose(true);
            base.Dispose();
        }

        protected void Dispose(bool disposing)
        {
            Trace.Call(disposing);

            if (disposing) {
                if (_LastSeenHighlightQueue != null) {
                    _LastSeenHighlightQueue.Dispose();
                }
                _LastSeenHighlightQueue = null;

                // HACK: this shouldn't be needed but GTK# keeps GC handles
                // these callbacks for some reason and thus leaks :(
                _OutputMessageTextView.Dispose();
                _TabMenu.Shown -= OnTabMenuShown;
                _OutputScrolledWindow.Vadjustment.ValueChanged -= OnVadjustmentValueChanged;
            }
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
#if SCROLL_DEBUG
            Trace.Call();
#endif

            // BUG? doesn't work always for some reason
            // seems like GTK+ doesn't update the adjustment till we give back control
            //Gtk.Adjustment adj = _OutputScrolledWindow.Vadjustment;
#if LOG4NET && SCROLL_DEBUG
            _Logger.Debug("ScrollToEnd(): Vadjustment.Value: " + adj.Value +
                          " Vadjustment.Upper: " + adj.Upper +
                          " Vadjustment.PageSize: " + adj.PageSize);
#endif
            //adj.Value = adj.Upper - adj.PageSize;
            
            //_OutputTextView.Buffer.MoveMark(_EndMark, _OutputTextView.Buffer.EndIter);
            //_OutputTextView.ScrollMarkOnscreen(_EndMark);
            //_OutputTextView.ScrollToMark(_EndMark, 0.49, true, 0.0, 0.0);
            
            //_OutputTextView.ScrollMarkOnscreen(_OutputTextView.Buffer.InsertMark);

            //_OutputTextView.ScrollMarkOnscreen(_OutputTextView.Buffer.GetMark("tail"));

#if SCROLL_DEBUG
            System.Reflection.MethodBase mb = Trace.GetMethodBase();
#endif
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
#if SCROLL_DEBUG
                Trace.Call(mb);
#endif
                _OutputMessageTextView.ScrollMarkOnscreen(_EndMark);
                return false;
            }));
        }
        
        void CheckAutoScroll()
        {
            var vAdjustment = _OutputScrolledWindow.Vadjustment;
            if (vAdjustment.Upper == (vAdjustment.Value + vAdjustment.PageSize)) {
                // the scrollbar is way at the end, lets autoscroll
                IsAutoScrolling = true;
            } else {
                IsAutoScrolling = false;
            }
        }

        void AutoScroll()
        {
            if (!IsAutoScrolling) {
                return;
            }

            ScrollToEnd();
        }

        public virtual void Enable()
        {
            Trace.Call();
        }
        
        public virtual void Disable()
        {
            Trace.Call();

            _IsSynced = false;
        }
        
        public void Sync()
        {
            Sync(0);
        }

        public virtual void Sync(int msgCount)
        {
            Trace.Call();

            GLib.Idle.Add(delegate {
                TabImage.SetFromStock(Gtk.Stock.Refresh, Gtk.IconSize.Menu);
                OnStatusChanged(EventArgs.Empty);
                return false;
            });

            // REMOTING CALL
            SyncedName = _ChatModel.Name;

            if (!Frontend.IsLocalEngine && Frontend.UseLowBandwidthMode) {
                // FIXME: set TabImage back to normal
                return;
            }

            // REMOTING CALL
            SyncedLastSeenHighlight = _ChatModel.LastSeenHighlight;
            
            if (Frontend.EngineVersion >= new Version(0, 12)) {
                // REMOTING CALL
                SyncedLastSeenMessage = _ChatModel.LastSeenMessage;
            }

            DateTime start, stop;
            start = DateTime.UtcNow;
            if (msgCount > 0 && Frontend.EngineVersion >= new Version(0, 8, 9)) {
                // REMOTING CALL
                var msgBuffer = _ChatModel.MessageBuffer;
                // REMOTING CALL
                var offset = msgBuffer.Count - msgCount;
                if (offset < 0) {
                    offset = 0;
                }
                // REMOTING CALL
                SyncedMessages = _ChatModel.MessageBuffer.GetRange(offset, msgCount);
            } else {
                // REMOTING CALL
                SyncedMessages = _ChatModel.Messages;
            }
            stop = DateTime.UtcNow;
#if LOG4NET
            _Logger.Debug(
                String.Format(
                    "Sync(): retrieving ChatModel.Messages took: {0:0.00} ms",
                    (stop - start).TotalMilliseconds
                )
            );
#endif
        }

        public virtual void Populate()
        {
            Trace.Call();

            Name = SyncedName;

            // sync messages
            // cleanup, be sure the output is empty
            _OutputMessageTextView.Clear();

            if (!Frontend.IsLocalEngine && Frontend.UseLowBandwidthMode) {
                var msg = new MessageBuilder();
                msg.AppendEventPrefix();
                msg.AppendMessage(_("Low Bandwidth Mode is active: no messages synced."));
                AddMessage(msg.ToMessage());
            } else {
                if (SyncedMessages != null) {
                    // TODO: push messages in batches and give back control to
                    // GTK+ in between for blocking the GUI thread less
                    foreach (MessageModel msg in SyncedMessages) {
                        AddMessage(msg);
                        if (msg.TimeStamp <= SyncedLastSeenMessage) {
                            // let the user know at which position new messages start
                            _OutputMessageTextView.UpdateMarkerline();
                        }
                    }
                }
            }

            // as we don't track which messages were already seen it would
            // show all chats with message activity after the frontend connect
            if (!HasHighlight) {
                HasActivity = false;
                HasEvent = false;
            }

            // reset tab icon to normal
            TabImage.Pixbuf = DefaultTabImage.Pixbuf;
            OnStatusChanged(EventArgs.Empty);

            SyncedMessages = null;
            _IsSynced = true;
        }
        
        public virtual void UpdateLastSeenMessage()
        {
            _OutputMessageTextView.UpdateMarkerline();
            
            if (Frontend.EngineVersion < new Version(0, 12)) {
                return;
            }
            
            var lastSeenMessage = _OutputMessageTextView.LastMessage;
            if (lastSeenMessage == null) {
                return;
            }
            
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    // REMOTING CALL
                    _ChatModel.LastSeenMessage = lastSeenMessage.TimeStamp;
                } catch (Exception ex) {
                    Frontend.ShowException(ex);
                }
            });
        }
        
        public virtual void AddMessage(MessageModel msg)
        {
            switch (msg.MessageType) {
                case MessageType.ChatNameChanged:
                    ThreadPool.QueueUserWorkItem(delegate {
                        try {
                            // REMOTING CALL
                            var newname = ChatModel.Name;
                            Gtk.Application.Invoke(delegate {
                                Name = newname;
                            });
                        } catch (Exception ex) {
                            Frontend.ShowException(ex);
                        }
                    });
                    return;
            }
            _OutputMessageTextView.AddMessage(msg);
        }
        
        public virtual void Clear()
        {
            Trace.Call();
            
            _OutputMessageTextView.Clear();
        }
        
        public virtual void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);
            
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            
            _ThemeSettings = new ThemeSettings(config);

            _OutputMessageTextView.ApplyConfig(config);
        }
        
        public virtual void Close()
        {
            Trace.Call();

            var protocolManager = ProtocolManager;
            if (protocolManager == null) {
#if LOG4NET
                _Logger.WarnFormat(
                    "{0}.Close(): ProtocolManager is null, bailing out!", this
                );
#endif
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    protocolManager.CloseChat(
                        Frontend.FrontendManager,
                        ChatModel
                    );
                } catch (Exception ex) {
                    Frontend.ShowException(ex);
                }
            });
        }

        public override string ToString()
        {
            return String.Format("<{0}>", ToTraceString());
        }

        public string ToTraceString()
        {
            return ID;
        }

        protected virtual void OnTabButtonPress(object sender, Gtk.ButtonPressEventArgs e)
        {
            Trace.Call(sender, e);

            try {
                if (e.Event.Button == 3) {
                    _TabMenu.Popup(null, null, null, e.Event.Button, e.Event.Time);
                }
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        protected virtual void OnTabMenuShown(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            foreach (var child in _TabMenu.Children) {
                _TabMenu.Remove(child);
            }
            var closeItem = new Gtk.ImageMenuItem(Gtk.Stock.Close, null);
            closeItem.Activated += OnTabMenuCloseActivated;
            _TabMenu.Append(closeItem);
            _TabMenu.ShowAll();
        }

        protected virtual void OnTabMenuCloseActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                Close();
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void OnMessageTextViewMessageAdded(object sender, MessageTextViewMessageAddedEventArgs e)
        {
            if (!IsActive) {
                switch (e.Message.MessageType) {
                    case MessageType.Normal:
                        HasActivity = true;
                        break;
                    case MessageType.Event:
                        HasEvent = true;
                        break;
                }
            }

            var buffer = _OutputMessageTextView.Buffer;
            buffer.MoveMark(_EndMark, buffer.EndIter);

            AutoScroll();
        }
        
        protected virtual void OnMessageTextViewMessageHighlighted(object sender, MessageTextViewMessageHighlightedEventArgs e)
        {
            if (_IsSynced) {
                bool isActiveChat = IsActive;

                if (Frontend.UseLowBandwidthMode && !isActiveChat) {
                    HasHighlight = true;
                    return;
                }

                var method = Trace.GetMethodBase();
                // update last seen highlight
                // OPT-TODO: we should use a TaskStack here OR at least a
                // timeout approach that will only sync once per 30 seconds!
                _LastSeenHighlightQueue.Queue(delegate {
                    Trace.Call(method, null, null);

                    // unhandled exception here would kill the syncer thread
                    try {
                        if (isActiveChat) {
                            // REMOTING CALL 1
                            _ChatModel.LastSeenHighlight = e.Message.TimeStamp;
                        } else {
                            // REMOTING CALL 1
                            if (_ChatModel.LastSeenHighlight < e.Message.TimeStamp) {
                                Gtk.Application.Invoke(delegate {
                                    // we have to make sure we only highlight
                                    // the chat if it still isn't the active
                                    // one as isActiveChat state is probably
                                    // obsolete by now
                                    if (IsActive) {
                                        return;
                                    }

                                    HasHighlight = true;
                                });
                            }
                        }
                    } catch (Exception ex) {
#if LOG4NET
                        _Logger.Error("OnMessageTextViewMessageHighlighted(): Exception: ", ex);
#endif
                    }
                });
            } else {
                if (e.Message.TimeStamp > SyncedLastSeenHighlight) {
                    HasHighlight = true;
                }
            }

            if (e.Message.TimeStamp > SyncedLastSeenHighlight) {
                // unseen highlight

                // HACK: out of scope?
                // only beep if the main windows has no focus (the user is
                // elsewhere) and the chat is was already synced, as during sync we
                // would get insane from all beeping caused by the old highlights
                if (!Frontend.MainWindow.HasToplevelFocus &&
                    _IsSynced &&
                    Frontend.UserConfig["Sound/BeepOnHighlight"] != null &&
                    (bool) Frontend.UserConfig["Sound/BeepOnHighlight"]) {
#if LOG4NET
                    _Logger.Debug("OnMessageTextViewMessageHighlighted(): BEEP!");
#endif
                    try {
                        if (Display != null) {
                            Display.Beep();
                        }
                    } catch (Exception ex) {
#if LOG4NET
                        _Logger.Error("OnMessageTextViewMessageHighlighted(): Exception", ex);
#endif
                    }
                }

                if (MessageHighlighted != null) {
                    MessageHighlighted(this, new ChatViewMessageHighlightedEventArgs(e.Message));
                }
            }
        }

        protected virtual void OnMessageTextViewPopulatePopup(object sender, Gtk.PopulatePopupArgs e)
        {
            Trace.Call(sender, e);

            if (OutputMessageTextView.IsAtUrlTag) {
                return;
            }

            Gtk.Menu popup = e.Menu;

            // hide menu bar item as it uses the app menu on OS X
            if (!Frontend.IsMacOSX) {
                popup.Prepend(new Gtk.SeparatorMenuItem());

                var item = new Gtk.CheckMenuItem(_("Show _Menubar"));
                item.Active = Frontend.MainWindow.ShowMenuBar;
                item.Activated += delegate {
                    try {
                        Frontend.MainWindow.ShowMenuBar = !Frontend.MainWindow.ShowMenuBar;
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                };
                popup.Prepend(item);
            }

            popup.ShowAll();
        }

        protected virtual void OnMessageTextViewPersonClicked(object sender, MessageTextViewPersonClickedEventArgs e)
        {
            Trace.Call(sender, e);

            var entry = Frontend.MainWindow.Entry;
            var text = entry.Text;
            var match = Regex.Match(text, "^[^ ]+: ");
            if (match.Success) {
                // removing existing nick
                text = text.Substring(match.Length);
            }
            text = String.Format("{0}: {1}", e.IdentityName, text);
            entry.Text = text;
            entry.HasFocus = true;
        }

        protected virtual void OnLastSeenHighlightQueueExceptionEvent(object sender, TaskQueueExceptionEventArgs e)
        {
            Trace.Call(sender, e);

#if LOG4NET
            _Logger.Error("Exception in TaskQueue: ", e.Exception);
            _Logger.Error("Inner-Exception: ", e.Exception.InnerException);
#endif
            Frontend.ShowException(e.Exception);
        }

        protected virtual void OnLastSeenHighlightQueueAbortedEvent(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

#if LOG4NET
            _Logger.Debug("OnLastSeenHighlightQueueAbortedEvent(): task queue aborted!");
#endif
        }

        protected virtual void OnStatusChanged(EventArgs e)
        {
            if (StatusChanged != null) {
                StatusChanged(this, e);
            }
        }

        void OnVadjustmentValueChanged(object sender, EventArgs e)
        {
            CheckAutoScroll();
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }

    public class ChatViewMessageHighlightedEventArgs : EventArgs
    {
        public MessageModel Message { get; set; }

        public ChatViewMessageHighlightedEventArgs(MessageModel msg)
        {
            Message = msg;
        }
    }
}
