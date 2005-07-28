/*
 * $Id: PreferencesDialog.cs 73 2005-06-27 12:42:06Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GtkGnome/PreferencesDialog.cs $
 * $Rev: 73 $
 * $Author: meebey $
 * $Date: 2005-06-27 14:42:06 +0200 (Mon, 27 Jun 2005) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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
using Meebey.Smuxi;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class FirstStartDruid
    {
#if UI_GNOME
        private Gnome.Druid             _Druid;
        private Gnome.DruidPageEdge     _FirstPage;
        private Gnome.DruidPageStandard _Page1;
        private Gnome.DruidPageStandard _LocalEnginePage;
        private Gnome.DruidPageEdge     _LastPage;
        private Gtk.ComboBox            _ModeComboBox;
        private string                  _SelectedMode;
        
        public FirstStartDruid()
        {
            // FirstPage
            _FirstPage = new Gnome.DruidPageEdge(Gnome.EdgePosition.Start, true,
                "Smuxi's first start",
                "Welcome to the smuxi\n"+
                "You started smuxi for the first time and it needs some answers from you.\n\n"+
                "Click \"Forward\" to begin.",
                null, null, null);
            _FirstPage.CancelClicked += new Gnome.CancelClickedHandler(_OnCancel);

            // page 1
            _Page1 = new Gnome.DruidPageStandard();
            _Page1.CancelClicked += new Gnome.CancelClickedHandler(_OnCancel);
            _Page1.Prepared += new Gnome.PreparedHandler(_OnPage1Prepared);
            _Page1.NextClicked += new Gnome.NextClickedHandler(_OnPage1NextClicked);
            _ModeComboBox = Gtk.ComboBox.NewText();
            _ModeComboBox.AppendText("Local");
            _ModeComboBox.AppendText("Remote");
            _ModeComboBox.Changed += new EventHandler(_OnModeComboBoxChanged);
            _ModeComboBox.Active = 0;
            _Page1.AppendItem("_Default Engine Mode:",
                _ModeComboBox, "When smuxi is started which mode it should use by default");
                
            // LastPage
            _LastPage = new Gnome.DruidPageEdge(Gnome.EdgePosition.Finish, true,
                "Thank you", "You can now use smuxi", null,
                null, null);
            _LastPage.CancelClicked += new Gnome.CancelClickedHandler(_OnCancel);
            _LastPage.FinishClicked += new Gnome.FinishClickedHandler(_OnFinishClicked);
            
            _Druid = new Gnome.Druid("First Start Druid", true);
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
        
        private void _OnFinishClicked(object sender, Gnome.FinishClickedArgs e)
        {
            Engine.FrontendConfig fc = Frontend.FrontendConfig;
            if (_SelectedMode == "Remote") {
                new NewEngineDruid();
            } else {
                fc["Engines/Default"] = String.Empty;
                fc.Save();
                Frontend.InitLocalEngine();
            }
            _Druid.Destroy();
        }

#if GTK_SHARP_2
        private void _OnModeComboBoxChanged(object sender, EventArgs e)
        {
            Gtk.TreeIter iter;
            if (_ModeComboBox.GetActiveIter(out iter)) {
               _SelectedMode = (string)_ModeComboBox.Model.GetValue(iter, 0);
            }
        }
#endif
        
        private void _OnPage1Prepared(object sender, EventArgs e)
        {
            _UpdatePage1Buttons();
        }
        
        private void _OnPage1Changed(object sender, EventArgs e)
        {
            _UpdatePage1Buttons();
        }
        
        private void _OnPage1NextClicked(object sender, Gnome.NextClickedArgs e)
        {
            if (_SelectedMode == "Local") {
                if (_LocalEnginePage == null) {
                    _LocalEnginePage = new Gnome.DruidPageStandard();
                    
                    _Druid.InsertPage(_Page1, _LocalEnginePage);
                }
                _Druid.Page = _LocalEnginePage;
                e.RetVal = true;
            }
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
#endif
    }
}
