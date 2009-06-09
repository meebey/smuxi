/*
 * $Id: PreferencesDialog.cs 73 2005-06-27 12:42:06Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GtkGnome/PreferencesDialog.cs $
 * $Rev: 73 $
 * $Author: meebey $
 * $Date: 2005-06-27 14:42:06 +0200 (Mon, 27 Jun 2005) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2009 Mirco Bauer <meebey@meebey.net>
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
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class EngineAssistant : Gtk.Assistant
    {
        private FrontendConfig                  f_Config;
        private string                          f_EngineName;
        private EngineAssistantNameWidget       f_NameWidget;
        private EngineAssistantConnectionWidget f_ConnectionWidget;
        
        public EngineAssistant(Gtk.Window parent, FrontendConfig config,
                               string engineName)
        {
            Trace.Call(parent, config);

            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            f_Config = config;

            TransientFor = parent;
            SetDefaultSize(640, 480);
            Title = _("Engine Assistant - Smuxi");

            Apply += OnApply;
            
            InitPages();
        }

        private void InitPages()
        {
            InitIntroPage();
            InitNamePage();
            InitConnectionPage();
            InitConfirmPage();
        }

        private void InitIntroPage()
        {
            EngineAssistantIntroWidget page = new EngineAssistantIntroWidget();
            AppendPage(page);
            if (f_EngineName == null) {
                SetPageTitle(page, _("Add Smuxi Engine"));
            } else {
                SetPageTitle(page, _("Edit Smuxi Engine"));
            }
            SetPageType(page, Gtk.AssistantPageType.Intro);
            SetPageComplete(page, true);
        }

        private void InitNamePage()
        {
            EngineAssistantNameWidget page = new EngineAssistantNameWidget();
            page.EngineNameEntry.Changed += delegate {
                SetPageComplete(page, page.EngineNameEntry.Text.Trim().Length > 0);
            };
            AppendPage(page);
            SetPageType(page, Gtk.AssistantPageType.Content);
            f_NameWidget = page;
        }

        private void InitConnectionPage()
        {
            EngineAssistantConnectionWidget page = new EngineAssistantConnectionWidget();
            page.HostEntry.Changed += delegate {
                SetPageComplete(page, page.HostEntry.Text.Trim().Length > 0);
            };
            AppendPage(page);
            SetPageType(page, Gtk.AssistantPageType.Content);
            f_ConnectionWidget = page;
        }

        private void InitConfirmPage()
        {
            Gtk.Label page = new Gtk.Label(_("Now you can use the new Smuxi Engine"));
            AppendPage(page);
            SetPageTitle(page, _("Thank you"));
            SetPageType(page, Gtk.AssistantPageType.Confirm);
        }

        protected virtual void OnApply(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            string engine = f_NameWidget.EngineNameEntry.Text;
            if (f_EngineName == null) {
                string[] engines = (string[]) _Config["Engines/Engines"];
                
                string[] newEngines;
                if (engines.Length == 0) {
                    // there was no existing engines
                    newEngines = new string[] { engine };
                } else {
                    newEngines = new string[engines.Length + 1];
                    engines.CopyTo(newEngines, 0);
                    newEngines[engines.Length] = engine;
                }
                
                if (engines.Length == 1 ||
                    f_NameWidget.MakeDefaultEngineCheckButton.Active) {
                    f_Config["Engines/Default"] = engine;
                }
                f_Config["Engines/Engines"] = newEngines;
            }
            
            //f_Config["Engines/"+engine+"/Username"] = _UsernameEntry.Text;
            //f_Config["Engines/"+engine+"/Password"] = _PasswordEntry.Text;
            f_Config["Engines/"+engine+"/Hostname"] = f_ConnectionWidget.HostEntry.Text;
            f_Config["Engines/"+engine+"/Port"] = f_ConnectionWidget.PortSpinButton.ValueAsInt;
            
            // HACK: we don't really support any other channels/formatters (yet)
            f_Config["Engines/"+engine+"/Channel"] = "TCP";
            f_Config["Engines/"+engine+"/Formatter"] = "binary";
            
            f_Config.Save();
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
