// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
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
#if GTK_BUILDER
using System;
using Smuxi.Common;
using UI = Gtk.Builder.ObjectAttribute;

namespace Smuxi.Frontend.Gnome
{
    public class PreferencesDialog : Gtk.Dialog
    {
        [UI("CategoryNotebook")] Gtk.Notebook f_CategoryNotebook;
        [UI("ConnectionToggleButton")] Gtk.ToggleButton f_ConnectionToggleButton;
        [UI("InterfaceToggleButton")] Gtk.ToggleButton f_InterfaceToggleButton;
        [UI("ServersToggleButton")] Gtk.ToggleButton f_ServersToggleButton;
        [UI("FiltersToggleButton")] Gtk.ToggleButton f_FiltersToggleButton;
        [UI("LoggingToggleButton")] Gtk.ToggleButton f_LoggingToggleButton;

        public PreferencesDialog(Gtk.Window parent, Gtk.Builder builder, IntPtr handle) :
                            base(handle)
        {
            Trace.Call(parent, builder, handle);

            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            if (builder == null) {
                throw new ArgumentNullException("builder");
            }
            if (handle == IntPtr.Zero) {
                throw new ArgumentException("handle", "handle must not be zero.");
            }

            builder.Autoconnect(this);
            f_CategoryNotebook.ShowTabs = false;
            f_ConnectionToggleButton.Active = true;

            ShowAll();
        }

        protected virtual void OnResponse(object sender, Gtk.ResponseArgs e)
        {
            Trace.Call(sender, e);

            switch (e.ResponseId) {
                case Gtk.ResponseType.Close:
                    Destroy();
                    break;
            }
        }

        protected virtual void OnConnectionToggleButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_ConnectionToggleButton.Active) {
                f_CategoryNotebook.Page = (int) Page.Connection;
                f_InterfaceToggleButton.Active = false;
                f_ServersToggleButton.Active = false;
                f_FiltersToggleButton.Active = false;
                f_LoggingToggleButton.Active = false;
            }
        }

        protected virtual void OnInterfaceToggleButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_InterfaceToggleButton.Active) {
                f_CategoryNotebook.Page = (int) Page.Interface;
                f_ConnectionToggleButton.Active = false;
                f_ServersToggleButton.Active = false;
                f_FiltersToggleButton.Active = false;
                f_LoggingToggleButton.Active = false;
            }
        }

        protected virtual void OnServersToggleButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_ServersToggleButton.Active) {
                f_CategoryNotebook.Page = (int) Page.Servers;
                f_ConnectionToggleButton.Active = false;
                f_InterfaceToggleButton.Active = false;
                f_FiltersToggleButton.Active = false;
                f_LoggingToggleButton.Active = false;
            }
        }

        protected virtual void OnFiltersToggleButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_FiltersToggleButton.Active) {
                f_CategoryNotebook.Page = (int) Page.Filters;
                f_ConnectionToggleButton.Active = false;
                f_InterfaceToggleButton.Active = false;
                f_ServersToggleButton.Active = false;
                f_LoggingToggleButton.Active = false;
            }
        }

        protected virtual void OnLoggingToggleButtonToggled(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            if (f_LoggingToggleButton.Active) {
                f_CategoryNotebook.Page = (int) Page.Logging;
                f_ConnectionToggleButton.Active = false;
                f_InterfaceToggleButton.Active = false;
                f_ServersToggleButton.Active = false;
                f_FiltersToggleButton.Active = false;
            }
        }

        public enum Page {
            Connection,
            Interface,
            Servers,
            Filters,
            Logging,
        }

        public enum InterfacePage {
            Messages,
            Tabs,
            Notifications,
            Input,
            Appearance,
        }
    }
}
#endif
