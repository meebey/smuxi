/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
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
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Globalization;
using Process = System.Diagnostics.Process;
using Smuxi;
using Smuxi.Common;
using Smuxi.Engine;
using System.Text.RegularExpressions;

namespace Smuxi.Frontend.Gnome
{
    public class PreferencesDialog
    {
        public enum Page : int {
            Connection = 0,
            Interface,
            Servers,
            Filters,
            Logging,
        }
        
        public enum InterfacePage : int {
            General = 0,
            Tabs,
            Input,
            Output,
            Notification
        }
        
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private Gtk.Dialog _Dialog;
        private Glade.XML  _Glade;
        
#region Widgets
        [Glade.Widget("Notebook")]
        private Gtk.Notebook _Notebook;
        [Glade.Widget("InterfaceNotebook")]
        private Gtk.Notebook _InterfaceNotebook;
        [Glade.Widget("MenuTreeView")]
        private Gtk.TreeView _MenuTreeView;
        [Glade.Widget("FilterListEventBox")]
        private Gtk.EventBox _FilterListEventBox;
#endregion
        
        private FilterListWidget _FilterListWidget;
        private ServerListView _ServerListView;
        
        public Page CurrentPage {
            get {
                return (Page) _Notebook.CurrentPage;
            }
            set {
                Gtk.TreeIter iter;
                _MenuTreeView.Model.GetIterFirst(out iter);
                do {
                    Page page = (Page) _MenuTreeView.Model.GetValue(iter, 0);
                    if (value == page) {
                        _MenuTreeView.Selection.SelectIter(iter);
                        break;
                    }
                } while (_MenuTreeView.Model.IterNext(ref iter));
            }
        }
        
        public InterfacePage CurrentInterfacePage {
            get {
                return (InterfacePage) _InterfaceNotebook.CurrentPage;
            }
            set {
                _InterfaceNotebook.CurrentPage = (int) value;
            }
        }
        
        public PreferencesDialog(Gtk.Window parent)
        {
            Trace.Call(parent);

            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            
            _Glade = new Glade.XML(null, Frontend.GladeFilename, "PreferencesDialog", null);
            //_Glade.BindFields(this);
            // changed signal is used in all settings, so use glade for now
            _Glade.Autoconnect(this);
            _Dialog = (Gtk.Dialog)_Glade["PreferencesDialog"];
            _Dialog.TransientFor = parent;
            
            ((Gtk.Button)_Glade["OKButton"]).Clicked += new EventHandler(_OnOKButtonClicked);
            ((Gtk.Button)_Glade["ApplyButton"]).Clicked += new EventHandler(_OnApplyButtonClicked);
            ((Gtk.Button)_Glade["CancelButton"]).Clicked += new EventHandler(_OnCancelButtonClicked);
            
            ((Gtk.TextView)_Glade["OnConnectCommandsTextView"]).Buffer.Changed += new EventHandler(_OnChanged);
            ((Gtk.TextView)_Glade["OnStartupCommandsTextView"]).Buffer.Changed += new EventHandler(_OnChanged);
            
            ((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Toggled += OnNotificationAreaIconCheckButtonToggled;
            ((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Toggled += _OnChanged;
            ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonAlways"]).Toggled += _OnChanged;
            ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonMinimized"]).Toggled += _OnChanged;
            // we can't support minimize for now, see: http://projects.qnetp.net/issues/show/158
            ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonMinimized"]).Visible = false;
            ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonClosed"]).Toggled += _OnChanged;
            
            ((Gtk.CheckButton)_Glade["OverrideForegroundColorCheckButton"]).Toggled += OnOverrideForegroundColorCheckButtonToggled;
            ((Gtk.CheckButton)_Glade["OverrideBackgroundColorCheckButton"]).Toggled += OnOverrideBackgroundColorCheckButtonToggled;
            ((Gtk.CheckButton)_Glade["OverrideFontCheckButton"]).Toggled += OnOverrideFontCheckButtonToggled;
            ((Gtk.FontButton)_Glade["FontButton"]).FontSet += _OnChanged;
            ((Gtk.CheckButton)_Glade["ShowAdvancedSettingsCheckButton"]).Toggled += delegate {
                CheckShowAdvancedSettingsCheckButton();
            };

            ((Gtk.CheckButton)_Glade["ProxyShowPasswordCheckButton"]).Toggled += delegate {
                CheckProxyShowPasswordCheckButton();
            };

            ((Gtk.TextView)_Glade["HighlightWordsTextView"]).Buffer.Changed += _OnChanged;
            if (Frontend.EngineVersion < new Version("0.7.2")) {
                // feature introduced in >= 0.7.2
                ((Gtk.TextView)_Glade["HighlightWordsTextView"]).Sensitive = false;
            }

            ((Gtk.Button)_Glade["LoggingOpenButton"]).Clicked += delegate {
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        var logPath = Platform.LogPath;
                        if (!Directory.Exists(logPath)) {
                            Directory.CreateDirectory(logPath);
                        }
                        Process.Start(logPath);
                    } catch (Exception ex) {
                        Frontend.ShowError(parent, ex);
                    }
                });
            };

            Gtk.ComboBox wrapModeComboBox = (Gtk.ComboBox)_Glade["WrapModeComboBox"];
            // initialize wrap modes
            // glade might initialize it already!
            wrapModeComboBox.Clear();
            wrapModeComboBox.Changed += _OnChanged;
            Gtk.CellRendererText cell = new Gtk.CellRendererText();
            wrapModeComboBox.PackStart(cell, false);
            wrapModeComboBox.AddAttribute(cell, "text", 1);
            Gtk.ListStore store = new Gtk.ListStore(typeof(Gtk.WrapMode), typeof(string));
            // fill ListStore
            store.AppendValues(Gtk.WrapMode.Char,     _("Character"));
            store.AppendValues(Gtk.WrapMode.WordChar, _("Word"));
            wrapModeComboBox.Model = store;
            wrapModeComboBox.Active = 0;
            
            Gtk.ComboBox persistencyTypeComboBox =
                (Gtk.ComboBox) _Glade["PersistencyTypeComboBox"];
            // glade might initialize it already!
            persistencyTypeComboBox.Clear();
            persistencyTypeComboBox.Changed += _OnChanged;
            cell = new Gtk.CellRendererText();
            persistencyTypeComboBox.PackStart(cell, false);
            persistencyTypeComboBox.AddAttribute(cell, "text", 1);
            store = new Gtk.ListStore(
                typeof(MessageBufferPersistencyType), typeof(string)
            );
            // fill ListStore
            store.AppendValues(MessageBufferPersistencyType.Volatile,
                               _("Volatile"));
            store.AppendValues(MessageBufferPersistencyType.Persistent,
                               _("Persistent"));
            persistencyTypeComboBox.Model = store;
            persistencyTypeComboBox.Active = 0;
            if (Frontend.EngineVersion < new Version("0.8.1")) {
                persistencyTypeComboBox.Sensitive = false;
                ((Gtk.SpinButton) _Glade["VolatileMaxCapacitySpinButton"]).Sensitive = false;
                ((Gtk.SpinButton) _Glade["PersistentMaxCapacitySpinButton"]).Sensitive = false;
            }

            Gtk.ComboBox proxyTypeComboBox = (Gtk.ComboBox)_Glade["ProxyTypeComboBox"];
            // initialize wrap modes
            // glade might initialize it already!
            proxyTypeComboBox.Clear();
            proxyTypeComboBox.Changed += _OnChanged;
            proxyTypeComboBox.Changed += delegate {
                CheckProxyTypeComBoBox();
            };
            cell = new Gtk.CellRendererText();
            proxyTypeComboBox.PackStart(cell, false);
            proxyTypeComboBox.AddAttribute(cell, "text", 1);
            store = new Gtk.ListStore(typeof(ProxyType), typeof(string));
            // fill ListStore
            store.AppendValues(ProxyType.None,    String.Format("<{0}>",
                                                                _("No Proxy")));
            store.AppendValues(ProxyType.System,  String.Format("<{0}>",
                                                                _("System Default")));
            store.AppendValues(ProxyType.Http,    "HTTP");
            store.AppendValues(ProxyType.Socks4,  "SOCK 4");
            store.AppendValues(ProxyType.Socks4a, "SOCK 4a");
            store.AppendValues(ProxyType.Socks5,  "SOCK 5");
            proxyTypeComboBox.Model = store;
            proxyTypeComboBox.Active = 0;

            _Notebook.ShowTabs = false;
            
            Gtk.ListStore ls = new Gtk.ListStore(typeof(Page), typeof(Gdk.Pixbuf), typeof(string));
            ls.AppendValues(Page.Connection, _Dialog.RenderIcon(
                                                Gtk.Stock.Connect,
                                                Gtk.IconSize.SmallToolbar, null),
                            _("Connection"));
            ls.AppendValues(Page.Interface, _Dialog.RenderIcon(
                                                Gtk.Stock.SelectFont,
                                                Gtk.IconSize.SmallToolbar, null),
                            _("Interface"));
            ls.AppendValues(Page.Servers, _Dialog.RenderIcon(
                                                Gtk.Stock.Network,
                                                Gtk.IconSize.SmallToolbar, null),
                            _("Servers"));

            if (Frontend.EngineVersion >= new Version("0.7.2")) {
                // features introduced in >= 0.7.2
                ls.AppendValues(Page.Filters, _Dialog.RenderIcon(
                                                    Gtk.Stock.Delete,
                                                    Gtk.IconSize.SmallToolbar, null),
                                _("Filters"));
                ls.AppendValues(Page.Logging, _Dialog.RenderIcon(
                                                    Gtk.Stock.JustifyLeft,
                                                    Gtk.IconSize.SmallToolbar, null),
                                _("Logging"));
            }

            int i = 1;
            _MenuTreeView.AppendColumn(null, new Gtk.CellRendererPixbuf(), "pixbuf",i++);
            _MenuTreeView.AppendColumn(null, new Gtk.CellRendererText(), "text", i++);
            _MenuTreeView.Selection.Changed += new EventHandler(_MenuTreeViewSelectionChanged);
            _MenuTreeView.Selection.Mode = Gtk.SelectionMode.Browse;
            _MenuTreeView.Model = ls;

            // select the first item
            Gtk.TreeIter iter;
            ls.GetIterFirst(out iter);
            _MenuTreeView.Selection.SelectIter(iter);
            
            _ServerListView = new ServerListView(_Dialog, _Glade);
            _FilterListWidget = new FilterListWidget(_Dialog, Frontend.UserConfig);
            _FilterListWidget.Changed += _OnChanged;
            _FilterListEventBox.Add(_FilterListWidget);
            _FilterListEventBox.ShowAll();

            _Load();
        }
        
        private void _Load()
        {
            Trace.Call();

            // root
            string startup_commands = String.Join("\n", (string[])Frontend.UserConfig["OnStartupCommands"]);
            ((Gtk.TextView)_Glade["OnStartupCommandsTextView"]).Buffer.Text  = startup_commands;
            
            // Connection
            string nicknames = String.Join(" ", (string[])Frontend.UserConfig["Connection/Nicknames"]);
            ((Gtk.Entry)_Glade["ConnectionNicknamesEntry"]).Text  = nicknames;
            ((Gtk.Entry)_Glade["ConnectionUsernameEntry"]).Text  = (string)Frontend.UserConfig["Connection/Username"];
            ((Gtk.Entry)_Glade["ConnectionRealnameEntry"]).Text  = (string)Frontend.UserConfig["Connection/Realname"];
            string connect_commands = String.Join("\n", (string[])Frontend.UserConfig["Connection/OnConnectCommands"]);
            ((Gtk.TextView)_Glade["OnConnectCommandsTextView"]).Buffer.Text = connect_commands;

            string encoding = (string)Frontend.UserConfig["Connection/Encoding"];
            encoding = encoding.ToUpper();

            Gtk.ComboBox cb = (Gtk.ComboBox)_Glade["EncodingComboBox"];
            // glade might initialize it already!
            cb.Clear();
            Gtk.CellRendererText cell = new Gtk.CellRendererText();
            cb.PackStart(cell, false);
            cb.AddAttribute(cell, "text", 0);
            Gtk.ListStore store = new Gtk.ListStore(typeof(string), typeof(string));
            store.AppendValues(String.Format("<{0}>", _("System Default")), String.Empty);
            ArrayList encodingList = new ArrayList();
            ArrayList bodyNameList = new ArrayList();
            foreach (EncodingInfo encInfo in Encoding.GetEncodings()) {
                try {
                    Encoding enc = Encoding.GetEncoding(encInfo.CodePage);
                    string encodingName = enc.EncodingName.ToUpper();

                    if (!enc.IsSingleByte && enc != Encoding.UTF8) {
                        // ignore multi byte encodings except UTF-8
                        continue;
                    }

                    // filter noise and duplicates
                    if (encodingName.IndexOf("DOS") != -1 ||
                        encodingName.IndexOf("MAC") != -1 ||
                        encodingName.IndexOf("EBCDIC") != -1 ||
                        encodingName.IndexOf("ISCII") != -1 ||
                        encodingList.Contains(encodingName) ||
                        bodyNameList.Contains(enc.BodyName)) {
                        continue;
                    }
#if LOG4NET
                    _Logger.Debug("_Load(): adding encoding: " + enc.BodyName);
#endif
                    encodingList.Add(encodingName);
                    bodyNameList.Add(enc.BodyName);
                    
                    encodingName = enc.EncodingName;
                    // remove all (DOS)  / (Windows) / (Mac) crap from the encoding name
                    if (enc.EncodingName.Contains(" (")) {
                        encodingName = encodingName.Substring(0, enc.EncodingName.IndexOf(" ("));
                    }
                    store.AppendValues(enc.BodyName.ToUpper() + " - " + encodingName, enc.BodyName.ToUpper());
                } catch (NotSupportedException) {
                }
            }
            cb.Model = store;
            cb.Active = 0;
            store.SetSortColumnId(0, Gtk.SortType.Ascending);
            int j = 0;
            foreach (object[] row in store) {
                string encodingName = (string) row[1];
                if (encodingName == encoding) {
                    cb.Active = j;
                    break;
                }
                j++;
            }

            // Connection - Proxy
            Gtk.ComboBox proxyTypeComboBox = ((Gtk.ComboBox)_Glade["ProxyTypeComboBox"]);
            ProxyType proxyType = (ProxyType) Enum.Parse(
                typeof(ProxyType),
                (string) Frontend.UserConfig["Connection/ProxyType"]
            );
            int i = 0;
            foreach (object[] row in  (Gtk.ListStore) proxyTypeComboBox.Model) {
                if (((ProxyType) row[0]) == proxyType) {
                    proxyTypeComboBox.Active = i;
                    break;
                }
                i++;
            }
            ((Gtk.Entry) _Glade["ProxyHostEntry"]).Text =
                (string) Frontend.UserConfig["Connection/ProxyHostname"];
            int proxyPort = (int) Frontend.UserConfig["Connection/ProxyPort"];
            if (proxyPort == -1) {
                proxyPort = 0;
            }
            ((Gtk.SpinButton) _Glade["ProxyPortSpinButton"]).Value = proxyPort;
            ((Gtk.Entry) _Glade["ProxyUsernameEntry"]).Text =
                (string) Frontend.UserConfig["Connection/ProxyUsername"];
            ((Gtk.Entry) _Glade["ProxyPasswordEntry"]).Text =
                (string) Frontend.UserConfig["Connection/ProxyPassword"];
            CheckProxyShowPasswordCheckButton();

            // MessageBuffer
            if (Frontend.EngineVersion >= new Version("0.8.1")) {
                // feature introduced in >= 0.8.1
                Gtk.ComboBox persistencyTypeComboBox =
                    ((Gtk.ComboBox)_Glade["PersistencyTypeComboBox"]);
                try {
                    var persistencyType = (MessageBufferPersistencyType) Enum.Parse(
                        typeof(MessageBufferPersistencyType),
                        (string) Frontend.UserConfig["MessageBuffer/PersistencyType"]
                    );
                    i = 0;
                    foreach (object[] row in (Gtk.ListStore) persistencyTypeComboBox.Model) {
                        if (((MessageBufferPersistencyType) row[0]) == persistencyType) {
                            persistencyTypeComboBox.Active = i;
                            break;
                        }
                        i++;
                    }
                } catch (ArgumentException) {
                    // for forward compatibility with newer engines
                    persistencyTypeComboBox.Active = -1;
                }
                ((Gtk.SpinButton)_Glade["VolatileMaxCapacitySpinButton"]).Value =
                    (double)(int)Frontend.UserConfig["MessageBuffer/Volatile/MaxCapacity"];
                ((Gtk.SpinButton)_Glade["PersistentMaxCapacitySpinButton"]).Value =
                    (double)(int)Frontend.UserConfig["MessageBuffer/Persistent/MaxCapacity"];
            }

            // Interface
            ((Gtk.CheckButton) _Glade["ShowAdvancedSettingsCheckButton"]).Active =
                (bool) Frontend.UserConfig["Interface/ShowAdvancedSettings"];
            CheckShowAdvancedSettingsCheckButton();

            // Interface/Notebook
            ((Gtk.Entry)_Glade["TimestampFormatEntry"]).Text =
                (string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"];
            ((Gtk.SpinButton)_Glade["BufferLinesSpinButton"]).Value =
                (double)(int)Frontend.UserConfig["Interface/Notebook/BufferLines"];
            ((Gtk.SpinButton)_Glade["EngineBufferLinesSpinButton"]).Value =
                (double)(int)Frontend.UserConfig["Interface/Notebook/EngineBufferLines"];
            ((Gtk.CheckButton)_Glade["StripColorsCheckButton"]).Active =
                (bool)Frontend.UserConfig["Interface/Notebook/StripColors"];
            ((Gtk.CheckButton)_Glade["StripFormattingsCheckButton"]).Active =
                (bool)Frontend.UserConfig["Interface/Notebook/StripFormattings"];
            switch ((string)Frontend.UserConfig["Interface/Notebook/TabPosition"]) {
                case "top":
                    ((Gtk.RadioButton)_Glade["TabPositionRadioButtonTop"]).Active = true;
                break;
                case "bottom":
                    ((Gtk.RadioButton)_Glade["TabPositionRadioButtonBottom"]).Active = true;
                break;
                case "left":
                    ((Gtk.RadioButton)_Glade["TabPositionRadioButtonLeft"]).Active = true;
                break;
                case "right":
                    ((Gtk.RadioButton)_Glade["TabPositionRadioButtonRight"]).Active = true;
                break;
                case "none":
                    ((Gtk.RadioButton)_Glade["TabPositionRadioButtonNone"]).Active = true;
                break;
            }
            if (Frontend.UserConfig["Interface/Notebook/AutoSwitchPersonChats"] != null) {
                ((Gtk.CheckButton) _Glade["AutoSwitchPersonChatsCheckButton"]).Active =
                    (bool) Frontend.UserConfig["Interface/Notebook/AutoSwitchPersonChats"];
            }
            if (Frontend.UserConfig["Interface/Notebook/AutoSwitchGroupChats"] != null) {
                ((Gtk.CheckButton) _Glade["AutoSwitchGroupChatsCheckButton"]).Active =
                    (bool) Frontend.UserConfig["Interface/Notebook/AutoSwitchGroupChats"];
            }

            // Interface/Notebook/Channel
            switch ((string)Frontend.UserConfig["Interface/Notebook/Channel/UserListPosition"]) {
                case "left":
                    ((Gtk.RadioButton)_Glade["UserListPositionRadioButtonLeft"]).Active = true;
                break;
                case "right":
                    ((Gtk.RadioButton)_Glade["UserListPositionRadioButtonRight"]).Active = true;
                break;
                case "none":
                    ((Gtk.RadioButton)_Glade["UserListPositionRadioButtonNone"]).Active = true;
                break;
            }
            switch ((string)Frontend.UserConfig["Interface/Notebook/Channel/TopicPosition"]) {
                case "top":
                    ((Gtk.RadioButton)_Glade["TopicPositionRadioButtonTop"]).Active = true;
                break;
                case "bottom":
                    ((Gtk.RadioButton)_Glade["TopicPositionRadioButtonBottom"]).Active = true;
                break;
                case "none":
                    ((Gtk.RadioButton)_Glade["TopicPositionRadioButtonNone"]).Active = true;
                break;
            }
            ((Gtk.CheckButton) _Glade["NickColorsCheckButton"]).Active =
                (bool) Frontend.UserConfig["Interface/Notebook/Channel/NickColors"];
            
            // Interface/Notebook/Tab
            Gtk.ColorButton colorButton;
            string colorHexCode;
            
            colorButton = (Gtk.ColorButton)_Glade["NoActivityColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Notebook/Tab/NoActivityColor"];
            colorButton.Color = ColorConverter.GetGdkColor(colorHexCode);

            colorButton = (Gtk.ColorButton)_Glade["ActivityColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Notebook/Tab/ActivityColor"];
            colorButton.Color = ColorConverter.GetGdkColor(colorHexCode);

            colorButton = (Gtk.ColorButton)_Glade["ModeColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Notebook/Tab/EventColor"];
            colorButton.Color = ColorConverter.GetGdkColor(colorHexCode);
            
            colorButton = (Gtk.ColorButton)_Glade["HighlightColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Notebook/Tab/HighlightColor"];
            colorButton.Color = ColorConverter.GetGdkColor(colorHexCode);
            
            // Interface/Chat
            colorButton = (Gtk.ColorButton)_Glade["ForegroundColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Chat/ForegroundColor"];
            if (String.IsNullOrEmpty(colorHexCode)) {
                ((Gtk.CheckButton)_Glade["OverrideForegroundColorCheckButton"]).Active = false;
            } else {
                ((Gtk.CheckButton)_Glade["OverrideForegroundColorCheckButton"]).Active = true;
                colorButton.Color = ColorConverter.GetGdkColor(colorHexCode);
            }
            
            colorButton = (Gtk.ColorButton)_Glade["BackgroundColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Chat/BackgroundColor"];
            if (String.IsNullOrEmpty(colorHexCode)) {
                ((Gtk.CheckButton)_Glade["OverrideBackgroundColorCheckButton"]).Active = false;
            } else {
                ((Gtk.CheckButton)_Glade["OverrideBackgroundColorCheckButton"]).Active = true;
                colorButton.Color = ColorConverter.GetGdkColor(colorHexCode);
            }
            
            Gtk.FontButton fontButton = (Gtk.FontButton)_Glade["FontButton"];
            string fontFamily = (string)Frontend.UserConfig["Interface/Chat/FontFamily"];
            string fontStyle = (string)Frontend.UserConfig["Interface/Chat/FontStyle"];
            int fontSize = 0;
            if (Frontend.UserConfig["Interface/Chat/FontSize"] != null) {
                fontSize = (int) Frontend.UserConfig["Interface/Chat/FontSize"];
            }
            if (String.IsNullOrEmpty(fontFamily) &&
                String.IsNullOrEmpty(fontStyle) &&
                fontSize == 0) {
                ((Gtk.CheckButton)_Glade["OverrideFontCheckButton"]).Active = false;
            } else {
                ((Gtk.CheckButton)_Glade["OverrideFontCheckButton"]).Active = true;
                Pango.FontDescription fontDescription = new Pango.FontDescription();
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
                fontButton.FontName = fontDescription.ToString();
            }
            
            Gtk.ComboBox wrapModeComboBox = ((Gtk.ComboBox)_Glade["WrapModeComboBox"]);
            Gtk.WrapMode wrapMode = (Gtk.WrapMode) Enum.Parse(
                typeof(Gtk.WrapMode),
                (string) Frontend.UserConfig["Interface/Chat/WrapMode"]
            );
            if (wrapMode == Gtk.WrapMode.Word) {
                wrapMode = Gtk.WrapMode.WordChar;
            }
            i = 0;
            foreach (object[] row in  (Gtk.ListStore) wrapModeComboBox.Model) {
                if (((Gtk.WrapMode) row[0]) == wrapMode) {
                    wrapModeComboBox.Active = i;
                    break;
                }
                i++;
            }
            
            // Interface/Entry
            ((Gtk.Entry)_Glade["CompletionCharacterEntry"]).Text =
                (string)Frontend.UserConfig["Interface/Entry/CompletionCharacter"];
            ((Gtk.Entry)_Glade["CommandCharacterEntry"]).Text =
                (string)Frontend.UserConfig["Interface/Entry/CommandCharacter"];
            ((Gtk.CheckButton)_Glade["BashStyleCompletionCheckButton"]).Active =
                (bool)Frontend.UserConfig["Interface/Entry/BashStyleCompletion"];
            ((Gtk.SpinButton)_Glade["CommandHistorySizeSpinButton"]).Value =
                (double)(int)Frontend.UserConfig["Interface/Entry/CommandHistorySize"];

            var highlight_words =
                (string[]) Frontend.UserConfig["Interface/Chat/HighlightWords"];
            // backwards compatibility with 0.7.x servers
            if (highlight_words == null) {
                highlight_words = new string[] {};
            }
            ((Gtk.TextView)_Glade["HighlightWordsTextView"]).Buffer.Text  =
                    String.Join("\n", highlight_words);

            ((Gtk.CheckButton)_Glade["BeepOnHighlightCheckButton"]).Active =
                (bool)Frontend.UserConfig["Sound/BeepOnHighlight"];
            
            // Interface/Notification
            string modeStr = (string) Frontend.UserConfig["Interface/Notification/NotificationAreaIconMode"];
            NotificationAreaIconMode mode = (NotificationAreaIconMode) Enum.Parse(
                typeof(NotificationAreaIconMode),
                modeStr
            );
            switch (mode) {
                case NotificationAreaIconMode.Never:
                    ((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Active = false;
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonClosed"]).Active = true;

                    // the toggle event is not raised as the checkbox is already unchecked by default
                    // thus we have to disable the radio buttons by hand
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonAlways"]).Sensitive = false;
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonMinimized"]).Sensitive = false;
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonClosed"]).Sensitive = false;
                    break;
                case NotificationAreaIconMode.Always:
                    ((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Active = true;
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonAlways"]).Active = true;
                    break;
                case NotificationAreaIconMode.Minimized:
                    // can't support this for now, see: http://projects.qnetp.net/issues/show/158
                    goto case NotificationAreaIconMode.Never;
                    /*
                    ((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Active = true;
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonMinimized"]).Active = true;
                    break;
                    */
                case NotificationAreaIconMode.Closed:
                    ((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Active = true;
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonClosed"]).Active = true;
                    break;
            }
            ((Gtk.CheckButton) _Glade["MessagingMenuCheckButton"]).Active =
                (bool) Frontend.UserConfig["Interface/Notification/MessagingMenuEnabled"];
            ((Gtk.CheckButton) _Glade["NotificationPopupsCheckButton"]).Active =
                (bool) Frontend.UserConfig["Interface/Notification/PopupsEnabled"];

            // Filters
            _FilterListWidget.InitProtocols(Frontend.Session.GetSupportedProtocols());
            _FilterListWidget.Load();
            
            // Servers
            _ServerListView.Load();
            
            // Logging
            ((Gtk.Button) _Glade["LoggingOpenButton"]).Visible = false;
            if (Frontend.UserConfig["Logging/Enabled"] != null) {
                ((Gtk.CheckButton) _Glade["LoggingEnabledCheckButton"]).Active =
                    (bool) Frontend.UserConfig["Logging/Enabled"];
                if (Frontend.IsLocalEngine) {
                    ((Gtk.Button) _Glade["LoggingOpenButton"]).Visible = true;
                }
            }
            if (Frontend.UserConfig["Logging/LogFilteredMessages"] != null) {
                ((Gtk.CheckButton) _Glade["LoggingLogFilteredMessagesCheckButton"]).Active =
                    (bool) Frontend.UserConfig["Logging/LogFilteredMessages"];
            }

            ((Gtk.Button)_Glade["ApplyButton"]).Sensitive = false;
        }
        
        private void _Save()
        {
            Trace.Call();
            
            if (((Gtk.Entry)_Glade["ConnectionNicknamesEntry"]).Text.Trim().Length == 0) {
                throw new ApplicationException(_("Nicknames(s) field must not be empty."));
            }
            
            string prefix;
            
            // root
            Frontend.UserConfig["OnStartupCommands"] = 
                ((Gtk.TextView)_Glade["OnStartupCommandsTextView"]).Buffer.Text.Split(new char[] {'\n'});
                
            // Connection
            Frontend.UserConfig["Connection/Nicknames"] = 
                ((Gtk.Entry)_Glade["ConnectionNicknamesEntry"]).Text.Split(new char[] {' '});
            Frontend.UserConfig["Connection/Username"] = 
                ((Gtk.Entry)_Glade["ConnectionUsernameEntry"]).Text;
            Frontend.UserConfig["Connection/Realname"] = 
                ((Gtk.Entry)_Glade["ConnectionRealnameEntry"]).Text;
            Frontend.UserConfig["Connection/OnConnectCommands"] = 
                ((Gtk.TextView)_Glade["OnConnectCommandsTextView"]).Buffer.Text.Split(new char[] {'\n'});
            
            Gtk.ComboBox cb = (Gtk.ComboBox)_Glade["EncodingComboBox"];
            Gtk.TreeIter iter;
            cb.GetActiveIter(out iter);
            string bodyName = (string) cb.Model.GetValue(iter, 1);
            Frontend.UserConfig["Connection/Encoding"] = bodyName;

            // Connection - Proxy
            cb = (Gtk.ComboBox) _Glade["ProxyTypeComboBox"];
            cb.GetActiveIter(out iter);
            var proxyType = (ProxyType) cb.Model.GetValue(iter, 0);
            Frontend.UserConfig["Connection/ProxyType"] = proxyType.ToString();
            Frontend.UserConfig["Connection/ProxyHostname"] =
                ((Gtk.Entry) _Glade["ProxyHostEntry"]).Text;
            Frontend.UserConfig["Connection/ProxyPort"] =
                ((Gtk.SpinButton) _Glade["ProxyPortSpinButton"]).ValueAsInt;
            Frontend.UserConfig["Connection/ProxyUsername"] =
                ((Gtk.Entry) _Glade["ProxyUsernameEntry"]).Text;
            Frontend.UserConfig["Connection/ProxyPassword"] =
                ((Gtk.Entry) _Glade["ProxyPasswordEntry"]).Text;

            int i;
            // MessageBuffer
            if (Frontend.EngineVersion >= new Version("0.8.1")) {
                var persistencyTypeComboBox = (Gtk.ComboBox) _Glade["PersistencyTypeComboBox"];
                // for forward compatibility with newer engines
                if (persistencyTypeComboBox.Active != -1) {
                    var persistencyType = MessageBufferPersistencyType.Volatile;
                    i = 0;
                    foreach (object[] row in (Gtk.ListStore) persistencyTypeComboBox.Model) {
                        if (persistencyTypeComboBox.Active == i) {
                            persistencyType = (MessageBufferPersistencyType) row[0];
                            break;
                        }
                        i++;
                    }
                    Frontend.UserConfig["MessageBuffer/PersistencyType"] =
                        persistencyType.ToString();
                }
                Frontend.UserConfig["MessageBuffer/Volatile/MaxCapacity"] =
                    (int)((Gtk.SpinButton)_Glade["VolatileMaxCapacitySpinButton"]).Value;
                Frontend.UserConfig["MessageBuffer/Persistent/MaxCapacity"] =
                    (int)((Gtk.SpinButton)_Glade["PersistentMaxCapacitySpinButton"]).Value;
            }

            // Interface
            Frontend.UserConfig["Interface/ShowAdvancedSettings"] =
                ((Gtk.CheckButton)_Glade["ShowAdvancedSettingsCheckButton"]).Active;

            // Interface/Notebook
            Frontend.UserConfig["Interface/Notebook/TimestampFormat"] =
                ((Gtk.Entry)_Glade["TimestampFormatEntry"]).Text;
            Frontend.UserConfig["Interface/Notebook/BufferLines"] =
                (int)((Gtk.SpinButton)_Glade["BufferLinesSpinButton"]).Value;
            Frontend.UserConfig["Interface/Notebook/EngineBufferLines"] =
                (int)((Gtk.SpinButton)_Glade["EngineBufferLinesSpinButton"]).Value;
            Frontend.UserConfig["Interface/Notebook/StripColors"] =
                ((Gtk.CheckButton)_Glade["StripColorsCheckButton"]).Active;
            Frontend.UserConfig["Interface/Notebook/StripFormattings"] =
                ((Gtk.CheckButton)_Glade["StripFormattingsCheckButton"]).Active;
                
            string tab_position = null;
            if (((Gtk.RadioButton)_Glade["TabPositionRadioButtonTop"]).Active) {
                tab_position = "top";
            } else if (((Gtk.RadioButton)_Glade["TabPositionRadioButtonBottom"]).Active) {
                tab_position = "bottom";
            } else if (((Gtk.RadioButton)_Glade["TabPositionRadioButtonLeft"]).Active) {
                tab_position = "left";
            } else if (((Gtk.RadioButton)_Glade["TabPositionRadioButtonRight"]).Active) {
                tab_position = "right";
            } else if (((Gtk.RadioButton)_Glade["TabPositionRadioButtonNone"]).Active) {
                tab_position = "none";
            }
            Frontend.UserConfig["Interface/Notebook/TabPosition"] = tab_position;

            Frontend.UserConfig["Interface/Notebook/AutoSwitchPersonChats"] =
                ((Gtk.CheckButton)_Glade["AutoSwitchPersonChatsCheckButton"]).Active;
            Frontend.UserConfig["Interface/Notebook/AutoSwitchGroupChats"] =
                ((Gtk.CheckButton)_Glade["AutoSwitchGroupChatsCheckButton"]).Active;

            // Interface/Notebook/Channel
            string userlist_position = null;
             if (((Gtk.RadioButton)_Glade["UserListPositionRadioButtonLeft"]).Active) {
                userlist_position = "left";
            } else if (((Gtk.RadioButton)_Glade["UserListPositionRadioButtonRight"]).Active) {
                userlist_position = "right";
            } else if (((Gtk.RadioButton)_Glade["UserListPositionRadioButtonNone"]).Active) {
                userlist_position = "none";
            }
            Frontend.UserConfig["Interface/Notebook/Channel/UserListPosition"] = userlist_position;
            
            Frontend.UserConfig["Interface/Notebook/Channel/NickColors"] =
                ((Gtk.CheckButton) _Glade["NickColorsCheckButton"]).Active;

            string topic_position = null;
             if (((Gtk.RadioButton)_Glade["TopicPositionRadioButtonTop"]).Active) {
                topic_position = "top";
            } else if (((Gtk.RadioButton)_Glade["TopicPositionRadioButtonBottom"]).Active) {
                topic_position = "bottom";
            } else if (((Gtk.RadioButton)_Glade["TopicPositionRadioButtonNone"]).Active) {
                topic_position = "none";
            }
            Frontend.UserConfig["Interface/Notebook/Channel/TopicPosition"] = topic_position;
            
            // Interface/Notebook/Tab
            prefix = "Interface/Notebook/Tab/";
            Frontend.UserConfig[prefix + "NoActivityColor"] =
                ColorConverter.GetHexCode(((Gtk.ColorButton)_Glade["NoActivityColorButton"]).Color);
            Frontend.UserConfig[prefix + "ActivityColor"] =
                ColorConverter.GetHexCode(((Gtk.ColorButton)_Glade["ActivityColorButton"]).Color);
            Frontend.UserConfig[prefix + "EventColor"] =
                ColorConverter.GetHexCode(((Gtk.ColorButton)_Glade["ModeColorButton"]).Color);
            Frontend.UserConfig[prefix + "HighlightColor"] =
                ColorConverter.GetHexCode(((Gtk.ColorButton)_Glade["HighlightColorButton"]).Color);
            
            // Interface/Chat
            prefix = "Interface/Chat/";
            if (((Gtk.CheckButton)_Glade["OverrideForegroundColorCheckButton"]).Active) {
                Frontend.UserConfig[prefix + "ForegroundColor"] = 
                    ColorConverter.GetHexCode(((Gtk.ColorButton)_Glade["ForegroundColorButton"]).Color);
            } else {
                Frontend.UserConfig[prefix + "ForegroundColor"] = String.Empty;
            }
            if (((Gtk.CheckButton)_Glade["OverrideBackgroundColorCheckButton"]).Active) {
                Frontend.UserConfig[prefix + "BackgroundColor"] = 
                    ColorConverter.GetHexCode(((Gtk.ColorButton)_Glade["BackgroundColorButton"]).Color);
            } else {
                Frontend.UserConfig[prefix + "BackgroundColor"] = String.Empty;
            }
            if (((Gtk.CheckButton)_Glade["OverrideFontCheckButton"]).Active) {
                string fontName = ((Gtk.FontButton)_Glade["FontButton"]).FontName;
                Pango.FontDescription fontDescription = Pango.FontDescription.FromString(fontName);
                Frontend.UserConfig[prefix + "FontFamily"] = fontDescription.Family;
                Frontend.UserConfig[prefix + "FontStyle"] = fontDescription.Weight + " " + fontDescription.Style;
                Frontend.UserConfig[prefix + "FontSize"] = fontDescription.Size / 1024;
            } else {
                Frontend.UserConfig[prefix + "FontFamily"] = String.Empty;
                Frontend.UserConfig[prefix + "FontStyle"] = String.Empty;
                Frontend.UserConfig[prefix + "FontSize"] = 0;
            }
            
            Gtk.ComboBox wrapModeComboBox = (Gtk.ComboBox) _Glade["WrapModeComboBox"];
            Gtk.WrapMode wrapMode = Gtk.WrapMode.Char;
            i = 0;
            foreach (object[] row in (Gtk.ListStore) wrapModeComboBox.Model) {
                if (wrapModeComboBox.Active == i) {
                    wrapMode = (Gtk.WrapMode) row[0];
                    break;
                }
                i++;
            }
            Frontend.UserConfig[prefix + "WrapMode"] = wrapMode.ToString();
            
            // Entry
            Frontend.UserConfig["Interface/Entry/CompletionCharacter"] =
                ((Gtk.Entry)_Glade["CompletionCharacterEntry"]).Text;
            Frontend.UserConfig["Interface/Entry/CommandCharacter"]   =
                ((Gtk.Entry)_Glade["CommandCharacterEntry"]).Text;
            Frontend.UserConfig["Interface/Entry/BashStyleCompletion"] =
                ((Gtk.CheckButton)_Glade["BashStyleCompletionCheckButton"]).Active;
            Frontend.UserConfig["Interface/Entry/CommandHistorySize"] =
                (int)((Gtk.SpinButton)_Glade["CommandHistorySizeSpinButton"]).Value;
            
            Frontend.UserConfig["Interface/Chat/HighlightWords"] = null;

            string[] highlight_words = ((Gtk.TextView) _Glade["HighlightWordsTextView"]).Buffer.Text.Split(new char[] { '\n' });
            foreach (string word in highlight_words) {
                if (word.StartsWith("/") && word.EndsWith("/")) {
                    try {
                        Regex regex = new Regex(word.Substring(1, word.Length - 2));
                    } catch (ArgumentException ex) {
                        throw new ApplicationException(
                            String.Format(
                                _("Invalid highlight regex: '{0}'. Reason: {1}"),
                                word, ex.Message
                            )
                        );
                    }
                }
            }

            Frontend.UserConfig["Interface/Chat/HighlightWords"] = highlight_words;

            Frontend.UserConfig["Sound/BeepOnHighlight"] =
                ((Gtk.CheckButton)_Glade["BeepOnHighlightCheckButton"]).Active;
            
            // Interface/Notification
            if (((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Active) {
                if (((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonAlways"]).Active) {
                    Frontend.UserConfig["Interface/Notification/NotificationAreaIconMode"] =
                        NotificationAreaIconMode.Always.ToString();
                } else if (((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonMinimized"]).Active) {
                        Frontend.UserConfig["Interface/Notification/NotificationAreaIconMode"] =
                            NotificationAreaIconMode.Minimized.ToString();
                } else if (((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonClosed"]).Active) {
                        Frontend.UserConfig["Interface/Notification/NotificationAreaIconMode"] =
                            NotificationAreaIconMode.Closed.ToString();
                }
            } else {
                    Frontend.UserConfig["Interface/Notification/NotificationAreaIconMode"] =
                        NotificationAreaIconMode.Never.ToString();
            }
            Frontend.UserConfig["Interface/Notification/MessagingMenuEnabled"] =
                ((Gtk.CheckButton)_Glade["MessagingMenuCheckButton"]).Active;
            Frontend.UserConfig["Interface/Notification/PopupsEnabled"] =
                ((Gtk.CheckButton)_Glade["NotificationPopupsCheckButton"]).Active;

            // Filters
            _FilterListWidget.Save();

            // Servers
            // _ServerListView saves directly after each change
            //_ServerListView.Save();
            
            // Logging
            Frontend.UserConfig["Logging/Enabled"] =
                ((Gtk.CheckButton) _Glade["LoggingEnabledCheckButton"]).Active;
            Frontend.UserConfig["Logging/LogFilteredMessages"] =
                ((Gtk.CheckButton) _Glade["LoggingLogFilteredMessagesCheckButton"]).Active;

            Frontend.Config.Save();
        }
        
        protected virtual void _OnChanged(object sender, EventArgs e)
        {
            ((Gtk.Button)_Glade["ApplyButton"]).Sensitive = true;
        }
        
        private void _OnOKButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                _Save();
                Frontend.Config.Load();
                Frontend.ApplyConfig(Frontend.UserConfig);
                _Dialog.Destroy();
            } catch (ApplicationException ex) {
                Frontend.ShowError(_Dialog, ex.Message);
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif                
                Frontend.ShowException(_Dialog, ex);
            }
        }

        private void _OnApplyButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                _Save();
                Frontend.Config.Load();
                _Load();
                Frontend.ApplyConfig(Frontend.UserConfig);
            } catch (ApplicationException ex) {
                Frontend.ShowError(_Dialog, ex.Message);
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif                
                Frontend.ShowException(_Dialog, ex);
            }
        }

        private void _OnCancelButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                _Dialog.Destroy();
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif                
                Frontend.ShowException(_Dialog, ex);
            }
        }
        
        private void _MenuTreeViewSelectionChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            Gtk.TreeIter iter;
            Gtk.TreeModel model;
            if (_MenuTreeView.Selection.GetSelected(out model, out iter)) {
                Page activePage = (Page)model.GetValue(iter, 0);
                _Notebook.CurrentPage = (int)activePage;
            }
        }
        
        private void OnOverrideForegroundColorCheckButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ((Gtk.ColorButton) _Glade["ForegroundColorButton"]).Sensitive = 
                    ((Gtk.CheckButton) _Glade["OverrideForegroundColorCheckButton"]).Active;
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        private void OnOverrideBackgroundColorCheckButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ((Gtk.ColorButton) _Glade["BackgroundColorButton"]).Sensitive = 
                    ((Gtk.CheckButton) _Glade["OverrideBackgroundColorCheckButton"]).Active;
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        private void OnOverrideFontCheckButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                ((Gtk.FontButton) _Glade["FontButton"]).Sensitive = 
                    ((Gtk.CheckButton) _Glade["OverrideFontCheckButton"]).Active;
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void OnNotificationAreaIconCheckButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                bool isActive = ((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Active;
                if (!isActive) {
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonClosed"]).Active = true;
                }
                ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonAlways"]).Sensitive = isActive;
                ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonMinimized"]).Sensitive = isActive;
                ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonClosed"]).Sensitive = isActive;
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        private void CheckShowAdvancedSettingsCheckButton()
        {
            Trace.Call();

            bool showAdvanced =
                ((Gtk.CheckButton) _Glade["ShowAdvancedSettingsCheckButton"]).Active;
            ((Gtk.Label) _Glade["ConnectionUsernameLabel"]).Visible = showAdvanced;
            ((Gtk.Entry) _Glade["ConnectionUsernameEntry"]).Visible = showAdvanced;
            ((Gtk.Label) _Glade["EncodingLabel"]).Visible = showAdvanced;
            ((Gtk.ComboBox) _Glade["EncodingComboBox"]).Visible = showAdvanced;
            ((Gtk.Frame) _Glade["NetworkProxyFrame"]).Visible = showAdvanced;
            ((Gtk.Frame) _Glade["GlobalCommandsFrame"]).Visible = showAdvanced;
        }

        private void CheckProxyShowPasswordCheckButton()
        {
            Trace.Call();

            ((Gtk.Entry) _Glade["ProxyPasswordEntry"]).Visibility =
                ((Gtk.CheckButton) _Glade["ProxyShowPasswordCheckButton"]).Active;
        }

        private void CheckProxyTypeComBoBox()
        {
            Trace.Call();

            var typoComboBox = (Gtk.ComboBox) _Glade["ProxyTypeComboBox"];
            var hostEntry = (Gtk.Entry) _Glade["ProxyHostEntry"];
            var portSpinButton = (Gtk.SpinButton) _Glade["ProxyPortSpinButton"];
            var userEntry = (Gtk.Entry) _Glade["ProxyUsernameEntry"];
            var passEntry = (Gtk.Entry) _Glade["ProxyPasswordEntry"];

            Gtk.TreeIter iter;
            typoComboBox.GetActiveIter(out iter);
            var proxyType = (ProxyType) typoComboBox.Model.GetValue(iter, 0);
            switch (proxyType) {
                case ProxyType.None:
                case ProxyType.System:
                    hostEntry.Sensitive = false;
                    portSpinButton.Sensitive = false;
                    userEntry.Sensitive = false;
                    passEntry.Sensitive = false;
                    break;
                case ProxyType.Http:
                    hostEntry.Sensitive = true;
                    portSpinButton.Sensitive = true;
                    userEntry.Sensitive = false;
                    userEntry.Text = String.Empty;
                    passEntry.Sensitive = false;
                    passEntry.Text = String.Empty;
                    break;
                case ProxyType.Socks4:
                case ProxyType.Socks4a:
                case ProxyType.Socks5:
                    hostEntry.Sensitive = true;
                    portSpinButton.Sensitive = true;
                    userEntry.Sensitive = true;
                    passEntry.Sensitive = true;
                    break;
            }
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
