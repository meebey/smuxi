/*
 * $Id: PreferencesDialog.cs 142 2007-01-02 22:19:08Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/PreferencesDialog.cs $
 * $Rev: 142 $
 * $Author: meebey $
 * $Date: 2007-01-02 23:19:08 +0100 (Tue, 02 Jan 2007) $
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
using System.Collections.Generic;
using Smuxi;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    public class ChannelFilterListView
    {
        private ChannelFiltersController _Controller;
        private Gtk.TreeView             _TreeView;
        private Gtk.ListStore            _ListStore;
        private Gtk.Button               _AddButton;
        private Gtk.Button               _RemoveButton;
        
        public ChannelFilterListView(Glade.XML gladeXml)
        {
            _Controller = new ChannelFiltersController(Frontend.UserConfig);
            
            _TreeView     = (Gtk.TreeView) gladeXml["ChannelFiltersTreeView"];
            _AddButton    = (Gtk.Button) gladeXml["ChannelFiltersAddButton"];
            _RemoveButton = (Gtk.Button) gladeXml["ChannelFiltersRemoveButton"];
            
            _AddButton.Clicked += new EventHandler(_OnAddButtonClicked);
            
            _ListStore = new Gtk.ListStore(typeof(string), // channel name
                                           typeof(bool), // joins
                                           typeof(bool), // parts
                                           typeof(bool) // quits
                                           );
            _TreeView.Model = _ListStore;
            int i = 0;
            Gtk.CellRendererText textCellr;
            Gtk.CellRendererToggle toggleCellr;
            
            textCellr = new Gtk.CellRendererText();
            textCellr.Editable = true;
            textCellr.Edited += delegate(object sender, Gtk.EditedArgs e) {
                Gtk.TreeIter iter;
                _ListStore.GetIterFromString(out iter, e.Path);
                _ListStore.SetValue(iter, 0, e.NewText);
            };
            _TreeView.AppendColumn(_("Pattern"), textCellr, "text", i);
            
            i++;
            toggleCellr = new Gtk.CellRendererToggle();
            toggleCellr.Activatable = true;
            toggleCellr.Toggled += delegate(object sender, Gtk.ToggledArgs e) {
                Gtk.TreeIter iter;
                _ListStore.GetIterFromString(out iter, e.Path);
                bool value = (bool) _ListStore.GetValue(iter, 1);
                _ListStore.SetValue(iter, 1, !value);
            };
            _TreeView.AppendColumn(_("Joins"), toggleCellr, "active", i);
            
            i++;
            toggleCellr = new Gtk.CellRendererToggle();
            toggleCellr.Activatable = true;
            toggleCellr.Toggled += delegate(object sender, Gtk.ToggledArgs e) {
                Gtk.TreeIter iter;
                _ListStore.GetIterFromString(out iter, e.Path);
                bool value = (bool) _ListStore.GetValue(iter, 2);
                _ListStore.SetValue(iter, 2, !value);
            };
            _TreeView.AppendColumn(_("Parts"), new Gtk.CellRendererToggle(), "active", i); 

            i++;
            toggleCellr = new Gtk.CellRendererToggle();
            toggleCellr.Activatable = true;
            toggleCellr.Toggled += delegate(object sender, Gtk.ToggledArgs e) {
                Gtk.TreeIter iter;
                _ListStore.GetIterFromString(out iter, e.Path);
                bool value = (bool) _ListStore.GetValue(iter, 3);
                _ListStore.SetValue(iter, 3, !value);
            };
            _TreeView.AppendColumn(_("Quits"), new Gtk.CellRendererToggle(), "active", i);
        }
        
        public void Save()
        {
        }
        
        public void Load()
        {
            IList<ChannelFilterModel> filters = _Controller.GetFilterList();
            foreach (ChannelFilterModel filter in filters) {
                _ListStore.AppendValues(filter.Pattern,
                                        filter.FilterJoins,
                                        filter.FilterParts,
                                        filter.FilterQuits);
            }
        }
        
        private void _OnAddButtonClicked(object sender, EventArgs e)
        {
            _ListStore.AppendValues(String.Empty, false, false, false);
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
