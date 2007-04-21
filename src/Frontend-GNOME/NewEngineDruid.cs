/*
 * $Id: PreferencesDialog.cs 73 2005-06-27 12:42:06Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GtkGnome/PreferencesDialog.cs $
 * $Rev: 73 $
 * $Author: meebey $
 * $Date: 2005-06-27 14:42:06 +0200 (Mon, 27 Jun 2005) $
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
using Smuxi;

namespace Smuxi.FrontendGnome
{
    public class NewEngineDruid
    {
#if UI_GNOME
        private Gnome.Druid             _Druid;
        private Gnome.DruidPageEdge     _Page1;
        private Gnome.DruidPageStandard _Page2;
        private Gnome.DruidPageStandard _Page3;
        private Gnome.DruidPageStandard _Page4;
        private Gnome.DruidPageEdge     _Page5;
        private Gtk.Entry               _EngineNameEntry;
        private Gtk.Entry               _HostEntry;
        private Gtk.SpinButton          _PortSpinButton;
        private Gtk.ComboBox            _ChannelComboBox;
        private string                  _SelectedChannel;
        private Gtk.ComboBox            _FormatterComboBox;
        private string                  _SelectedFormatter;
        private Gtk.Entry               _UsernameEntry;
        private Gtk.Entry               _PasswordEntry;
        private Gtk.Entry               _Password2Entry;
        
        public NewEngineDruid()
        {
            // page 1
            _Page1 = new Gnome.DruidPageEdge(Gnome.EdgePosition.Start, true,
                _("Add smuxi engine"),
                _("Welcome to the Smuxi Engine Configuration Assistent.\n"+
                "You need to enter some information before you can use the engine.\n\n"+
                "Click \"Forward\" to begin."),
                null, null, null);
            _Page1.CancelClicked += new Gnome.CancelClickedHandler(_OnCancel);

            // page 2
            _Page2 = new Gnome.DruidPageStandard();
            _Page2.CancelClicked += new Gnome.CancelClickedHandler(_OnCancel);
            _Page2.Prepared += new Gnome.PreparedHandler(_OnPage2Prepared);
            _EngineNameEntry = new Gtk.Entry();
            _EngineNameEntry.Changed += new EventHandler(_OnPage2Changed);
            _Page2.AppendItem(_("_Engine Name:"),
                _EngineNameEntry, _("Profile name of the new engine entry"));
            
            // page 3
            _Page3 = new Gnome.DruidPageStandard();
            _Page3.CancelClicked += new Gnome.CancelClickedHandler(_OnCancel);
            _Page3.Prepared += new Gnome.PreparedHandler(_OnPage3Prepared);
            
            _HostEntry = new Gtk.Entry();
            _HostEntry.Changed += new EventHandler(_OnPage3Changed);
            _Page3.AppendItem(_("_Host:"),
                _HostEntry, _("DNS or IP address of the smuxi engine"));

            _PortSpinButton = new Gtk.SpinButton(1, 65535, 1);
            _PortSpinButton.Numeric = true;
            _PortSpinButton.Value = 7689;
            _Page3.AppendItem(_("_Port:"),
                _PortSpinButton, _("TCP port of the smuxi engine"));
            
            _ChannelComboBox = Gtk.ComboBox.NewText();
            _ChannelComboBox.AppendText("TCP");
            _ChannelComboBox.Changed += new EventHandler(_OnChannelComboBoxChanged);
            _ChannelComboBox.Active = 0;
            _Page3.AppendItem(_("_Channel:"), _ChannelComboBox,
                _(".NET Remoting Channel which will be used for communication\n"+
                "between the frontend and the engine"));
            
            _FormatterComboBox = Gtk.ComboBox.NewText();
            _FormatterComboBox.AppendText("binary");
            _FormatterComboBox.Changed += new EventHandler(_OnFormatterComboBoxChanged);
            _FormatterComboBox.Active = 0;
            _Page3.AppendItem(_("_Formatter:"), _FormatterComboBox,
                _(".NET Remoting Data Formatter"));
            
            // page 4
            _Page4 = new Gnome.DruidPageStandard();
            _Page4.CancelClicked += new Gnome.CancelClickedHandler(_OnCancel);
            _Page4.Prepared += new Gnome.PreparedHandler(_OnPage4Prepared);
            
            _UsernameEntry = new Gtk.Entry();
            _UsernameEntry.Changed += new EventHandler(_OnPage4Changed);
            _Page4.AppendItem(_("_Username:"), _UsernameEntry,
                _("Username which will be used to register at the smuxi engine"));
            
            _PasswordEntry = new Gtk.Entry();
            _PasswordEntry.Visibility = false;
            _PasswordEntry.Changed += new EventHandler(_OnPage4Changed);
            _Page4.AppendItem(_("_Password:"), _PasswordEntry,
                _("Password of the user"));
            
            _Password2Entry = new Gtk.Entry();
            _Password2Entry.Visibility = false;
            _Password2Entry.Changed += new EventHandler(_OnPage4Changed);
            _Page4.AppendItem(_("_Verify Password:"), _Password2Entry,
                _("Repeat the password for verification"));
            
            // page 5
            _Page5 = new Gnome.DruidPageEdge(Gnome.EdgePosition.Finish, true,
                _("Thank you"), _("Now you can use the added smuxi engine"), null,
                null, null);
            _Page5.CancelClicked += new Gnome.CancelClickedHandler(_OnCancel);
            _Page5.FinishClicked += new Gnome.FinishClickedHandler(_OnFinishClicked);
            
            _Druid = new Gnome.Druid(_("Adding new engine to smuxi"), true);
            _Druid.Cancel += new EventHandler(_OnCancel);
            
            _Druid.AppendPage(_Page1);
            _Druid.AppendPage(_Page2);
            _Druid.AppendPage(_Page3);
            _Druid.AppendPage(_Page4);
            _Druid.AppendPage(_Page5);
            _Druid.ShowAll();
        }
        
        private void _OnCancel(object sender, EventArgs e)
        {
            _Druid.Destroy();
            if (Frontend.Session == null) {
                new EngineManagerDialog();
            }
        }
        
        private void _OnFinishClicked(object sender, Gnome.FinishClickedArgs e)
        {
            string new_engine = _EngineNameEntry.Text;
            string[] engines = (string[])Frontend.FrontendConfig["Engines/Engines"];
            string[] new_engines;
            Engine.FrontendConfig fc = Frontend.FrontendConfig;
            
            if (engines.Length == 0) {
                // there was no existing engines
                new_engines = new string[] {new_engine};
                fc["Engines/Default"] = new_engine;
            } else {
                new_engines = new string[engines.Length+1];
                engines.CopyTo(new_engines, 0);
                new_engines[engines.Length] = new_engine;
            }
            
            fc["Engines/Engines"] = new_engines;
            fc["Engines/"+new_engine+"/Username"] = _UsernameEntry.Text;
            fc["Engines/"+new_engine+"/Password"] = _PasswordEntry.Text;
            fc["Engines/"+new_engine+"/Hostname"] = _HostEntry.Text;
            fc["Engines/"+new_engine+"/Port"] = (int)_PortSpinButton.Value;
            fc["Engines/"+new_engine+"/Channel"] = _SelectedChannel;
            fc["Engines/"+new_engine+"/Formatter"] = _SelectedFormatter;
            fc.Save();
            fc.Load();
            
            _Druid.Destroy();
            if (Frontend.Session == null) {
                new EngineManagerDialog();
            }
        }

        private void _OnChannelComboBoxChanged(object sender, EventArgs e)
        {
            Gtk.TreeIter iter;
            if (_ChannelComboBox.GetActiveIter(out iter)) {
               _SelectedChannel = (string)_ChannelComboBox.Model.GetValue(iter, 0);
            }
        }

        private void _OnFormatterComboBoxChanged(object sender, EventArgs e)
        {
            Gtk.TreeIter iter;
            if (_FormatterComboBox.GetActiveIter(out iter)) {
               _SelectedFormatter = (string)_FormatterComboBox.Model.GetValue(iter, 0);
            }
        }
        
        private void _OnPage2Prepared(object sender, EventArgs e)
        {
            _UpdatePage2Buttons();
        }
        
        private void _OnPage2Changed(object sender, EventArgs e)
        {
            _UpdatePage2Buttons();
        }
        
        private void _UpdatePage2Buttons()
        {
            if (_EngineNameEntry.Text.Trim().Length > 0) {
                _Druid.SetButtonsSensitive(true, true, true, false);
            } else {
                _Druid.SetButtonsSensitive(true, false, true, false);
            }
        }
        
        private void _OnPage3Prepared(object sender, EventArgs e)
        {
            _UpdatePage3Buttons();
        }
        
        private void _OnPage3Changed(object sender, EventArgs e)
        {
            _UpdatePage3Buttons();
        }
        
        private void _UpdatePage3Buttons()
        {
            if (_HostEntry.Text.Trim().Length > 0) {
                _Druid.SetButtonsSensitive(true, true, true, false);
            } else {
                _Druid.SetButtonsSensitive(true, false, true, false);
            }
        }
        
        private void _OnPage4Prepared(object sender, EventArgs e)
        {
            _UpdatePage4Buttons();
        }
        
        private void _OnPage4Changed(object sender, EventArgs e)
        {
            _UpdatePage4Buttons();
        }
        
        private void _UpdatePage4Buttons()
        {
            if ((_UsernameEntry.Text.Trim().Length > 0) &&
                (_PasswordEntry.Text.Trim().Length > 0) &&
                (_PasswordEntry.Text == _Password2Entry.Text)) {
                _Druid.SetButtonsSensitive(true, true, true, false);
            } else {
                _Druid.SetButtonsSensitive(true, false, true, false);
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
#endif
    }
}
