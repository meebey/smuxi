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
using System.Text;
using System.Collections;
using System.Globalization;
using Smuxi;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class PreferencesDialog
    {
        public enum Page : int {
            Connection = 0,
            Interface,
            Servers,
            Filters,
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
#endregion
        
        private ChannelFilterListView _ChannelFilterListView;
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
            ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonClosed"]).Toggled += _OnChanged;
            
            ((Gtk.CheckButton)_Glade["OverrideForegroundColorCheckButton"]).Toggled += OnOverrideForegroundColorCheckButtonToggled;
            ((Gtk.CheckButton)_Glade["OverrideBackgroundColorCheckButton"]).Toggled += OnOverrideBackgroundColorCheckButtonToggled;
            ((Gtk.CheckButton)_Glade["OverrideFontCheckButton"]).Toggled += OnOverrideFontCheckButtonToggled;

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
            /*
            ls.AppendValues(Page.Filters, _Dialog.RenderIcon(
                                                Gtk.Stock.Delete,
                                                Gtk.IconSize.SmallToolbar, null),
                            _("Filters"));
            */
            
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
            
            _ChannelFilterListView = new ChannelFilterListView(_Glade);
            _ServerListView = new ServerListView(parent, _Glade);
            
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
            
            // Interface
            ((Gtk.Entry)_Glade["TimestampFormatEntry"]).Text =
                (string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"];
                
            // Interface/Notebook
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
            colorButton.Color = ColorTools.GetGdkColor(colorHexCode);

            colorButton = (Gtk.ColorButton)_Glade["ActivityColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Notebook/Tab/ActivityColor"];
            colorButton.Color = ColorTools.GetGdkColor(colorHexCode);

            colorButton = (Gtk.ColorButton)_Glade["ModeColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Notebook/Tab/EventColor"];
            colorButton.Color = ColorTools.GetGdkColor(colorHexCode);
            
            colorButton = (Gtk.ColorButton)_Glade["HighlightColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Notebook/Tab/HighlightColor"];
            colorButton.Color = ColorTools.GetGdkColor(colorHexCode);
            
            // Interface/Chat
            colorButton = (Gtk.ColorButton)_Glade["ForegroundColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Chat/ForegroundColor"];
            if (String.IsNullOrEmpty(colorHexCode)) {
                ((Gtk.CheckButton)_Glade["OverrideForegroundColorCheckButton"]).Active = false;
            } else {
                ((Gtk.CheckButton)_Glade["OverrideForegroundColorCheckButton"]).Active = true;
                colorButton.Color = ColorTools.GetGdkColor(colorHexCode);
            }
            
            colorButton = (Gtk.ColorButton)_Glade["BackgroundColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Chat/BackgroundColor"];
            if (String.IsNullOrEmpty(colorHexCode)) {
                ((Gtk.CheckButton)_Glade["OverrideBackgroundColorCheckButton"]).Active = false;
            } else {
                ((Gtk.CheckButton)_Glade["OverrideBackgroundColorCheckButton"]).Active = true;
                colorButton.Color = ColorTools.GetGdkColor(colorHexCode);
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
            int i = 0;
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
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonMinimized"]).Active = true;
                    break;
                case NotificationAreaIconMode.Always:
                    ((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Active = true;
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonAlways"]).Active = true;
                    break;
                case NotificationAreaIconMode.Minimized:
                    ((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Active = true;
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonMinimized"]).Active = true;
                    break;
                case NotificationAreaIconMode.Closed:
                    ((Gtk.CheckButton) _Glade["NotificationAreaIconCheckButton"]).Active = true;
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonClosed"]).Active = true;
                    break;
            }
            
            // Filters
            _ChannelFilterListView.Load();
            
            // Servers
            _ServerListView.Load();
            
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
            
            // Interface
            Frontend.UserConfig["Interface/Notebook/TimestampFormat"] =
                ((Gtk.Entry)_Glade["TimestampFormatEntry"]).Text;
                
            Frontend.UserConfig["Interface/Notebook/BufferLines"] =
                (int)((Gtk.SpinButton)_Glade["BufferLinesSpinButton"]).Value;
            Frontend.UserConfig["Interface/Notebook/EngineBufferLines"] =
                (int)((Gtk.SpinButton)_Glade["EngineBufferLinesSpinButton"]).Value;
            Frontend.UserConfig["Interface/Notebook/StripColors"] =
                ((Gtk.CheckButton)_Glade["StripColorsCheckButton"]).Active;
            Frontend.UserConfig["Interface/Notebook/StripFormatting"] =
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
                ColorTools.GetHexCodeColor(((Gtk.ColorButton)_Glade["NoActivityColorButton"]).Color);
            Frontend.UserConfig[prefix + "ActivityColor"] =
                ColorTools.GetHexCodeColor(((Gtk.ColorButton)_Glade["ActivityColorButton"]).Color);
            Frontend.UserConfig[prefix + "EventColor"] =
                ColorTools.GetHexCodeColor(((Gtk.ColorButton)_Glade["ModeColorButton"]).Color);
            Frontend.UserConfig[prefix + "HighlightColor"] =
                ColorTools.GetHexCodeColor(((Gtk.ColorButton)_Glade["HighlightColorButton"]).Color);
            
            // Interface/Chat
            prefix = "Interface/Chat/";
            if (((Gtk.CheckButton)_Glade["OverrideForegroundColorCheckButton"]).Active) {
                Frontend.UserConfig[prefix + "ForegroundColor"] = 
                    ColorTools.GetHexCodeColor(((Gtk.ColorButton)_Glade["ForegroundColorButton"]).Color);
            } else {
                Frontend.UserConfig[prefix + "ForegroundColor"] = String.Empty;
            }
            if (((Gtk.CheckButton)_Glade["OverrideBackgroundColorCheckButton"]).Active) {
                Frontend.UserConfig[prefix + "BackgroundColor"] = 
                    ColorTools.GetHexCodeColor(((Gtk.ColorButton)_Glade["BackgroundColorButton"]).Color);
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
            int i = 0;
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
            
            // Filters
            _ChannelFilterListView.Save();
            
            // Servers
            //_ServerListView.Save();
            
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
                Frontend.UserConfig.ClearCache();
                Frontend.ApplyConfig(Frontend.UserConfig);
                _Dialog.Destroy();
            } catch (ApplicationException ex) {
                Frontend.ShowError(_Dialog, ex.Message);
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
                _Logger.Error("BaseException", ex.GetBaseException());
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
                Frontend.UserConfig.ClearCache();
                _Load();
                Frontend.ApplyConfig(Frontend.UserConfig);
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
                    ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonMinimized"]).Active = true;
                }
                ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonAlways"]).Sensitive = isActive;
                ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonMinimized"]).Sensitive = isActive;
                ((Gtk.RadioButton) _Glade["NotificationAreaIconRadioButtonClosed"]).Sensitive = isActive;
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
