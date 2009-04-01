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
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class EngineAssistant : Gtk.Assistant
    {
        private FrontendConfig f_Config;
        
        public EngineAssistant(FrontendConfig config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            
            f_Config = config;
            
            SetDefaultSize(640, 480);
            Title = _("Engine Assistant - Smuxi");
            InitPages();
        }
        
        private void InitPages()
        {
            InitIntroPage();
            InitNamePage();
            InitConfirmPage();
        }
        
        private void InitIntroPage()
        {
            EngineAssistantIntroWidget page = new EngineAssistantIntroWidget();
            AppendPage(page);
            SetPageTitle(page, _("Add Smuxi Engine"));
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
        }
        
        private void InitConfirmPage()
        {
            Gtk.Label page = new Gtk.Label(_("Now you can use the new Smuxi Engine"));
            AppendPage(page);
            SetPageTitle(page, _("Thank you"));
            SetPageType(page, Gtk.AssistantPageType.Confirm);
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
