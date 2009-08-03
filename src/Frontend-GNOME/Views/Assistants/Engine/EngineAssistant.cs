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
        private FrontendConfig                   f_Config;
        private string                           f_EngineName;
        private EngineAssistantNameWidget        f_NameWidget;
        private EngineAssistantConnectionWidget  f_ConnectionWidget;
        private EngineAssistantCredentialsWidget f_CredentialsWidget;
        
        public EngineAssistant(Gtk.Window parent, FrontendConfig config) :
                          this(parent, config, null)
        {
            Trace.Call(parent, config);
        }
        
        public EngineAssistant(Gtk.Window parent, FrontendConfig config,
                               string engineName)
        {
            Trace.Call(parent, config, engineName);

            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            f_Config = config;
            f_EngineName = engineName;
            
            TransientFor = parent;
            SetDefaultSize(640, 480);
            SetPosition(Gtk.WindowPosition.CenterOnParent);
            Title = _("Engine Assistant - Smuxi");

            Apply += OnApply;
            
            InitPages();
        }

        private void InitPages()
        {
            InitIntroPage();
            InitNamePage();
            InitConnectionPage();
            InitCredentialsPage();
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
            SetPageTitle(page, _("Name"));
            SetPageType(page, Gtk.AssistantPageType.Content);
            f_NameWidget = page;

            if (f_EngineName != null) {
                // we can't rename engines for now
                page.EngineNameEntry.Text = f_EngineName;
                page.EngineNameEntry.Sensitive = false;
            }
        }

        private void InitConnectionPage()
        {
            EngineAssistantConnectionWidget page = new EngineAssistantConnectionWidget();
            page.UseSshTunnelCheckButton.Toggled += delegate {
                bool isActive = page.UseSshTunnelCheckButton.Active;
                page.SshHostEntry.Sensitive = isActive;
                page.SshPortSpinButton.Sensitive = isActive;
                page.HostEntry.Sensitive = !isActive;
                f_CredentialsWidget.SshUsernameEntry.Sensitive = isActive;
                if (isActive) {
                    page.HostEntry.Text = "localhost";
                    SetPageComplete(page, false);
                } else {
                    page.SshHostEntry.Text = String.Empty;
                    page.SshPortSpinButton.Value = 22d;
                    f_CredentialsWidget.SshUsernameEntry.Text = String.Empty;
                    SetPageComplete(page, true);
                }
            };
            page.SshHostEntry.Changed += delegate {
                if (!page.UseSshTunnelCheckButton.Active) {
                    return;
                }
                SetPageComplete(page, page.SshHostEntry.Text.Trim().Length > 0);
            };
            page.HostEntry.Changed += delegate {
                SetPageComplete(page, page.HostEntry.Text.Trim().Length > 0);
            };
            AppendPage(page);
            SetPageTitle(page, _("Connection"));
            SetPageType(page, Gtk.AssistantPageType.Content);
            f_ConnectionWidget = page;
            
            if (f_EngineName != null) {
                page.UseSshTunnelCheckButton.Active = (bool) f_Config["Engines/" + f_EngineName + "/UseSshTunnel"];
                page.SshHostEntry.Text = (string) f_Config["Engines/" + f_EngineName + "/SshHostname"];
                page.SshPortSpinButton.Value = (double)(int) f_Config["Engines/" + f_EngineName + "/SshPort"];
                
                page.HostEntry.Text = (string) f_Config["Engines/" + f_EngineName + "/Hostname"];
                page.PortSpinButton.Value = (double)(int) f_Config["Engines/" + f_EngineName + "/Port"];
            }
        }

        private void InitCredentialsPage()
        {
            EngineAssistantCredentialsWidget page = new EngineAssistantCredentialsWidget();
            page.SshUsernameEntry.Changed += delegate {
                CheckCredentialsPage();
            };
            page.UsernameEntry.Changed += delegate {
                CheckCredentialsPage();
            };
            page.PasswordEntry.Changed += delegate {
                CheckCredentialsPage();
            };
            page.VerifyPasswordEntry.Changed += delegate {
                CheckCredentialsPage();
            };
            AppendPage(page);
            SetPageTitle(page, _("Credentials"));
            SetPageType(page, Gtk.AssistantPageType.Content);
            f_CredentialsWidget = page;
            
            if (f_EngineName != null) {
                page.SshUsernameEntry.Text = (string) f_Config["Engines/" + f_EngineName + "/SshUsername"];
                page.UsernameEntry.Text = (string) f_Config["Engines/" + f_EngineName + "/Username"];
                page.PasswordEntry.Text = (string) f_Config["Engines/" + f_EngineName + "/Password"];
                page.VerifyPasswordEntry.Text = (string) f_Config["Engines/" + f_EngineName + "/Password"];
            }
        }
        
        private void CheckCredentialsPage()
        {
            SetPageComplete(
                f_CredentialsWidget,
                f_CredentialsWidget.UsernameEntry.Text.Trim().Length > 0 &&
                    f_CredentialsWidget.PasswordEntry.Text.Trim().Length > 0 &&
                    f_CredentialsWidget.PasswordEntry.Text ==
                        f_CredentialsWidget.VerifyPasswordEntry.Text
            );
        }
        
        private void InitConfirmPage()
        {
            Gtk.Label page = new Gtk.Label(_("Now you can use the Smuxi Engine"));
            AppendPage(page);
            SetPageTitle(page, _("Thank you"));
            SetPageType(page, Gtk.AssistantPageType.Confirm);
            SetPageComplete(page, true);
        }

        protected virtual void OnApply(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            string engine = f_NameWidget.EngineNameEntry.Text;
            if (f_EngineName == null) {
                string[] engines = (string[]) f_Config["Engines/Engines"];
                
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
            
            f_Config["Engines/"+engine+"/Username"] = f_CredentialsWidget.UsernameEntry.Text.Trim();
            f_Config["Engines/"+engine+"/Password"] = f_CredentialsWidget.PasswordEntry.Text.Trim();
            f_Config["Engines/"+engine+"/Hostname"] = f_ConnectionWidget.HostEntry.Text.Trim();
            f_Config["Engines/"+engine+"/Port"] = f_ConnectionWidget.PortSpinButton.ValueAsInt;
            bool useSsh = f_ConnectionWidget.UseSshTunnelCheckButton.Active;
            f_Config["Engines/"+engine+"/UseSshTunnel"] = useSsh;
            if (useSsh) {
                f_Config["Engines/"+engine+"/SshUsername"] = f_CredentialsWidget.SshUsernameEntry.Text.Trim();
                f_Config["Engines/"+engine+"/SshHostname"] = f_ConnectionWidget.SshHostEntry.Text.Trim();
                f_Config["Engines/"+engine+"/SshPort"] = f_ConnectionWidget.SshPortSpinButton.ValueAsInt;
            }
            
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
