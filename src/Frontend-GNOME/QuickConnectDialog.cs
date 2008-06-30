/*
 * $Id: PreferencesDialog.cs 73 2005-06-27 12:42:06Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GtkGnome/PreferencesDialog.cs $
 * $Rev: 73 $
 * $Author: meebey $
 * $Date: 2005-06-27 14:42:06 +0200 (Mon, 27 Jun 2005) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public partial class QuickConnectDialog : Gtk.Dialog 
    {
        private ServerModel f_ServerModel;
        
        public ServerModel Server {
            get {
                return f_ServerModel;
            }
        }
        
        public QuickConnectDialog()
        {
            Build();
            
            // initialize protocols
            f_ProtocolComboBox.Clear();
            f_ProtocolComboBox.Changed += OnProtocolComboBoxChanged;
            Gtk.CellRenderer cell = new Gtk.CellRendererText();
            f_ProtocolComboBox.PackStart(cell, false);
            f_ProtocolComboBox.AddAttribute(cell, "text", 0);
            IList<string> supportedProtocols = Frontend.Session.GetSupportedProtocols();
            Gtk.ListStore store = new Gtk.ListStore(typeof(string));
            foreach (string protocol in supportedProtocols) {
                store.AppendValues(protocol);
            }
            store.SetSortColumnId(0, Gtk.SortType.Ascending);
            f_ProtocolComboBox.Model = store;
            f_ProtocolComboBox.Active = 0;
        }
        
        protected virtual void OnProtocolComboBoxChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                // HACK: hardcoded default list, not so nice
                // suggest sane port defaults
                switch (f_ProtocolComboBox.ActiveText) {
                    case "IRC":
                        f_HostnameEntry.Sensitive = true;
                        
                        f_PortSpinButton.Value = 6667;
                        f_PortSpinButton.Sensitive = true;
                        break;
                    case "XMPP":
                        f_HostnameEntry.Sensitive = true;
                        
                        f_PortSpinButton.Value = 5222;
                        f_PortSpinButton.Sensitive = true;
                        break;
                    case "AIM":
                    case "ICQ":
                    case "MSNP":
                        f_HostnameEntry.Text = String.Empty;
                        f_HostnameEntry.Sensitive = false;
                        
                        f_PortSpinButton.Value = 0;
                        f_PortSpinButton.Sensitive = false;
                        break;
                }
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
        
        protected virtual void OnConnectButtonClicked(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            try {
                f_ServerModel = new ServerModel();
                f_ServerModel.Protocol = f_ProtocolComboBox.ActiveText;
                f_ServerModel.Hostname = f_HostnameEntry.Text;
                f_ServerModel.Port     = f_PortSpinButton.ValueAsInt;
                f_ServerModel.Username = f_UsernameEntry.Text;
                f_ServerModel.Password = f_PasswordEntry.Text;
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }
    }
}
