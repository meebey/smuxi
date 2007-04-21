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
using Mono.Unix;
#if UI_GNOME
using GNOME = Gnome;
#endif

namespace Smuxi.Frontend.Gnome
{
    public class FirstStartDruid
    {
#if UI_GNOME
        private GNOME.Druid             _Druid;
        private GNOME.DruidPageEdge     _FirstPage;
        private GNOME.DruidPageStandard _Page1;
        private GNOME.DruidPageStandard _LocalEnginePage;
        private GNOME.DruidPageEdge     _LastPage;
        private Gtk.ComboBox            _ModeComboBox;
        private string                  _SelectedMode;
        
        public FirstStartDruid()
        {
            // FirstPage
            _FirstPage = new GNOME.DruidPageEdge(GNOME.EdgePosition.Start, true,
                _("Smuxi's first start"),
                _("Welcome to the smuxi\n"+
                "You started smuxi for the first time and it needs some answers from you.\n\n"+
                "Click \"Forward\" to begin."),
                null, null, null);
            _FirstPage.CancelClicked += new GNOME.CancelClickedHandler(_OnCancel);

            // page 1
            _Page1 = new GNOME.DruidPageStandard();
            _Page1.CancelClicked += new GNOME.CancelClickedHandler(_OnCancel);
            _Page1.Prepared += new GNOME.PreparedHandler(_OnPage1Prepared);
            _Page1.NextClicked += new GNOME.NextClickedHandler(_OnPage1NextClicked);
            _ModeComboBox = Gtk.ComboBox.NewText();
            _ModeComboBox.AppendText(_("Local"));
            _ModeComboBox.AppendText(_("Remote"));
            _ModeComboBox.Changed += new EventHandler(_OnModeComboBoxChanged);
            _ModeComboBox.Active = 0;
            _Page1.AppendItem(_("_Default Engine Mode:"),
                _ModeComboBox, _("When smuxi is started which mode it should use by default"));
                
            // LastPage
            _LastPage = new GNOME.DruidPageEdge(GNOME.EdgePosition.Finish, true,
                _("Thank you"), _("Now you can use smuxi"), null,
                null, null);
            _LastPage.CancelClicked += new GNOME.CancelClickedHandler(_OnCancel);
            _LastPage.FinishClicked += new GNOME.FinishClickedHandler(_OnFinishClicked);
            
            _Druid = new GNOME.Druid(_("First Start Druid"), true);
            _Druid.Cancel += new EventHandler(_OnCancel);
            
            _Druid.AppendPage(_FirstPage);
            _Druid.AppendPage(_Page1);
            _Druid.AppendPage(_LastPage);
            _Druid.ShowAll();
        }
        
        private void _OnCancel(object sender, EventArgs e)
        {
            _Druid.Destroy();
            Frontend.Quit();
        }
        
        private void _OnFinishClicked(object sender, GNOME.FinishClickedArgs e)
        {
            Engine.FrontendConfig fc = Frontend.FrontendConfig;
            if (_SelectedMode == _("Remote")) {
                new NewEngineDruid();
            } else {
                fc["Engines/Default"] = String.Empty;
                fc.Save();
                Frontend.InitLocalEngine();
            }
            _Druid.Destroy();
        }

        private void _OnModeComboBoxChanged(object sender, EventArgs e)
        {
            Gtk.TreeIter iter;
            if (_ModeComboBox.GetActiveIter(out iter)) {
               _SelectedMode = (string)_ModeComboBox.Model.GetValue(iter, 0);
            }
        }
        
        private void _OnPage1Prepared(object sender, EventArgs e)
        {
            _UpdatePage1Buttons();
        }
        
        /*
        private void _OnPage1Changed(object sender, EventArgs e)
        {
            _UpdatePage1Buttons();
        }
        */
        
        private void _OnPage1NextClicked(object sender, GNOME.NextClickedArgs e)
        {
            /*
            // BUG: why did I wanted to a insert a special page for local?
            if (_SelectedMode == _("Local")) {
                if (_LocalEnginePage == null) {
                    _LocalEnginePage = new Gnome.DruidPageStandard();
                    
                    _Druid.InsertPage(_Page1, _LocalEnginePage);
                }
                _Druid.Page = _LocalEnginePage;
                e.RetVal = true;
            }
            */
        }
        
        private void _UpdatePage1Buttons()
        {
            /*
            if (_EngineNameEntry.Text.Trim().Length > 0) {
                _Druid.SetButtonsSensitive(true, true, true, false);
            } else {
                _Druid.SetButtonsSensitive(true, false, true, false);
            }
            */
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
#endif
    }
}
