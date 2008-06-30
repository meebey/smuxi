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
        
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private Gtk.Dialog _Dialog;
        private Glade.XML  _Glade;
        
#region Widgets
        [Glade.Widget("Notebook")]
        private Gtk.Notebook _Notebook;
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
        
        public PreferencesDialog()
        {
            Trace.Call();

            _Glade = new Glade.XML(null, Frontend.GladeFilename, "PreferencesDialog", null);
            //_Glade.BindFields(this);
            // changed signal is used in all settings, so use glade for now
            _Glade.Autoconnect(this);
            _Dialog = (Gtk.Dialog)_Glade["PreferencesDialog"];
            
            ((Gtk.Button)_Glade["OKButton"]).Clicked += new EventHandler(_OnOKButtonClicked);
            ((Gtk.Button)_Glade["ApplyButton"]).Clicked += new EventHandler(_OnApplyButtonClicked);
            ((Gtk.Button)_Glade["CancelButton"]).Clicked += new EventHandler(_OnCancelButtonClicked);
            
            ((Gtk.TextView)_Glade["OnConnectCommandsTextView"]).Buffer.Changed += new EventHandler(_OnChanged);
            ((Gtk.TextView)_Glade["OnStartupCommandsTextView"]).Buffer.Changed += new EventHandler(_OnChanged);
            
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
            ls.AppendValues(Page.Filters, _Dialog.RenderIcon(
                                                Gtk.Stock.Delete,
                                                Gtk.IconSize.SmallToolbar, null),
                            _("Filters"));
            
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
            _ServerListView = new ServerListView(_Glade);
            
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
            store.AppendValues(String.Empty, String.Empty);
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
            colorButton.Color = _HexStringToGdkColor(colorHexCode);

            colorButton = (Gtk.ColorButton)_Glade["ActivityColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Notebook/Tab/ActivityColor"];
            colorButton.Color = _HexStringToGdkColor(colorHexCode);

            colorButton = (Gtk.ColorButton)_Glade["ModeColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Notebook/Tab/ModeColor"];
            colorButton.Color = _HexStringToGdkColor(colorHexCode);
            
            colorButton = (Gtk.ColorButton)_Glade["HighlightColorButton"];
            colorHexCode = (string)Frontend.UserConfig["Interface/Notebook/Tab/HighlightColor"];
            colorButton.Color = _HexStringToGdkColor(colorHexCode);
            
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
            
            // Filters
            _ChannelFilterListView.Load();
            
            // Servers
            _ServerListView.Load();
            
            ((Gtk.Button)_Glade["ApplyButton"]).Sensitive = false;
        }
        
        private void _Save()
        {
            Trace.Call();
            
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
                _GdkColorToHexString(((Gtk.ColorButton)_Glade["NoActivityColorButton"]).Color);
            Frontend.UserConfig[prefix + "ActivityColor"] =
                _GdkColorToHexString(((Gtk.ColorButton)_Glade["ActivityColorButton"]).Color);
            Frontend.UserConfig[prefix + "ModeColor"] =
                _GdkColorToHexString(((Gtk.ColorButton)_Glade["ModeColorButton"]).Color);
            Frontend.UserConfig[prefix + "HighlightColor"] =
                _GdkColorToHexString(((Gtk.ColorButton)_Glade["HighlightColorButton"]).Color);
            
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
            
            // Filters
            _ChannelFilterListView.Save();
            
            // Servers
            //_ServerListView.Save();
            
            Frontend.Config.Save();
        }
        
        private Gdk.Color _HexStringToGdkColor(string color)
        {
            color = color.Substring(1); // remove #
            int red   = Int16.Parse(color.Substring(0, 2), NumberStyles.HexNumber);
            int green = Int16.Parse(color.Substring(2, 2), NumberStyles.HexNumber);
            int blue  = Int16.Parse(color.Substring(4, 2), NumberStyles.HexNumber);
            return new Gdk.Color((byte)red, (byte)green, (byte)blue);
        }
        
        private string _GdkColorToHexString(Gdk.Color color)
        {
            string res = "#";
            res += ((byte)color.Red).ToString("X2");
            res += ((byte)color.Green).ToString("X2"); 
            res += ((byte)color.Blue).ToString("X2");
            return res;
        }
        
        private void _OnChanged(object sender, EventArgs e)
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
                _Dialog.Destroy();
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
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
