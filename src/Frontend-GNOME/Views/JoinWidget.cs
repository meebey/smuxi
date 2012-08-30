// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2012 Mirco Bauer <meebey@meebey.net>
//
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
using System;
using System.Collections.Generic;
using Smuxi.Common;
using Smuxi.Engine;
using System.Runtime.InteropServices;

namespace Smuxi.Frontend.Gnome
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class JoinWidget : Gtk.Bin
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        public EventHandler<EventArgs> Activated;

        public new bool HasFocus {
            get {
                return f_ChatEntry.HasFocus;
            }
            set {
                f_ChatEntry.HasFocus = value;
            }
        }

        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern void gtk_entry_set_icon_from_pixbuf(IntPtr entry, int pos, IntPtr pixbuf);

        // Since: 2.16
        // void gtk_entry_set_icon_tooltip_text(GtkEntry *entry, GtkEntryIconPosition icon_pos, const gchar *tooltip)
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern void gtk_entry_set_icon_tooltip_text(IntPtr entry, int pos, IntPtr tooltip);

        // Since: 3.2
        // void gtk_entry_set_placeholder_text (GtkEntry *entry, const gchar *text)
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern void gtk_entry_set_placeholder_text(IntPtr entry, string text);

        public JoinWidget()
        {
            Build();

            try {
                gtk_entry_set_icon_from_pixbuf(f_ChatEntry.Handle, 0, GroupChatView.IconPixbuf.Handle);
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("JoinWidget(): gtk_entry_set_icon_from_pixbuf() failed!", ex);
#endif
            }
            try {
                var text = _("Enter which chat to join");
                IntPtr textPtr = GLib.Marshaller.StringToPtrGStrdup(text);
                gtk_entry_set_icon_tooltip_text(f_ChatEntry.Handle, 0, textPtr);
                GLib.Marshaller.Free(textPtr);
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("JoinWidget(): gtk_entry_set_icon_tooltip_text() failed!", ex);
#endif
            }
            try {
                //gtk_entry_set_placeholder_text(f_ChatEntry.Handle, "Enter chat name...");
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("JoinWidget(): gtk_entry_set_placeholder_text() failed!", ex);
#endif
            }

            f_ChatEntry.Activated += delegate {
                OnActivated(EventArgs.Empty);
            };
            f_JoinButton.Clicked += delegate {
                OnActivated(EventArgs.Empty);
            };
        }

        public void InitNetworks(IList<string> networks)
        {
            Trace.Call(networks);

            if (networks == null) {
                throw new ArgumentNullException("networks");
            }

            f_NetworkComboBox.Clear();
            var cell = new Gtk.CellRendererText();
            f_NetworkComboBox.PackStart(cell, false);
            f_NetworkComboBox.AddAttribute(cell, "text", 0);

            Gtk.ListStore store = new Gtk.ListStore(typeof(string));
            foreach (string network in networks) {
                if (String.IsNullOrEmpty(network)) {
                    continue;
                }
                store.AppendValues(network);
            }
            store.SetSortColumnId(0, Gtk.SortType.Ascending);
            f_NetworkComboBox.Model = store;
            f_NetworkComboBox.Active = 0;
        }

        public void ApplyConfig(UserConfig config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            var servers = new ServerListController(config);
            InitNetworks(servers.GetNetworks());
        }

        public Uri GetChatLink()
        {
            return new Uri(
                String.Format("smuxi://{0}/{1}",
                              f_NetworkComboBox.ActiveText,
                              f_ChatEntry.Text)
            );
        }

        public void Clear()
        {
            f_ChatEntry.Text = String.Empty;
        }

        protected virtual void OnActivated(EventArgs e)
        {
            if (Activated != null) {
                Activated(this, e);
            }
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
