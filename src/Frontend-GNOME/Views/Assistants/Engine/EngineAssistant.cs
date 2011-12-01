/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
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
using System.IO;
using Smuxi.Common;
using Smuxi.Engine;
using IOPath = System.IO.Path;

namespace Smuxi.Frontend.Gnome
{
    public class EngineAssistant : Gtk.Assistant
    {
        private FrontendConfig                   f_Config;
        private string                           f_EngineName;
        private EngineAssistantIntroWidget       f_IntroWidget;
        private EngineAssistantNameWidget        f_NameWidget;
        private int                              f_NamePage;
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
            f_IntroWidget = new EngineAssistantIntroWidget();

            AppendPage(f_IntroWidget);
            if (f_EngineName == null) {
                SetPageTitle(f_IntroWidget, _("Add Smuxi Engine"));
            } else {
                SetPageTitle(f_IntroWidget, _("Edit Smuxi Engine"));
            }
            SetPageType(f_IntroWidget, Gtk.AssistantPageType.Intro);
            SetPageComplete(f_IntroWidget, true);
        }

        private void InitNamePage()
        {
            f_NameWidget = new EngineAssistantNameWidget();

            f_NamePage = AppendPage(f_NameWidget);
            SetPageTitle(f_NameWidget, _("Name"));
            SetPageType(f_NameWidget, Gtk.AssistantPageType.Content);
            Prepare += delegate(object sender, Gtk.PrepareArgs e) {
                if (e.Page != f_NameWidget) {
                    return;
                }
                CheckNamePage();
            };

            f_NameWidget.EngineNameEntry.Changed += delegate {
                CheckNamePage();
            };

            if (f_EngineName != null) {
                // we can't rename engines for now
                f_NameWidget.EngineNameEntry.Text = f_EngineName;
                f_NameWidget.EngineNameEntry.Sensitive = false;
            }
        }

        private void CheckNamePage()
        {
            bool isComplete = true;
            if (f_NameWidget.EngineNameEntry.Text.Trim().Length == 0) {
                isComplete = false;
            }
            SetPageComplete(f_NameWidget, isComplete);
        }

        private void InitConnectionPage()
        {
            f_ConnectionWidget = new EngineAssistantConnectionWidget();

            AppendPage(f_ConnectionWidget);
            SetPageTitle(f_ConnectionWidget, _("Connection"));
            SetPageType(f_ConnectionWidget, Gtk.AssistantPageType.Content);
            Prepare += delegate(object sender, Gtk.PrepareArgs e) {
                if (e.Page != f_ConnectionWidget) {
                    return;
                }
                CheckConnectionPage();
            };

            f_ConnectionWidget.UseSshTunnelCheckButton.Toggled += delegate {
                bool isActive = f_ConnectionWidget.UseSshTunnelCheckButton.Active;
                f_ConnectionWidget.SshHostEntry.Sensitive = isActive;
                f_ConnectionWidget.SshPortSpinButton.Sensitive = isActive;
                f_ConnectionWidget.HostEntry.Sensitive = !isActive;
                if (isActive) {
                    f_ConnectionWidget.HostEntry.Text = "localhost";
                } else {
                    f_ConnectionWidget.HostEntry.Text = String.Empty;
                    f_ConnectionWidget.SshHostEntry.Text = String.Empty;
                    f_ConnectionWidget.SshPortSpinButton.Value = 22d;
                }

                CheckConnectionPage();
            };
            f_ConnectionWidget.SshHostEntry.Changed += delegate {
                CheckConnectionPage();
            };
            f_ConnectionWidget.HostEntry.Changed += delegate {
                CheckConnectionPage();
            };

            if (f_EngineName != null) {
                f_ConnectionWidget.UseSshTunnelCheckButton.Active = (bool)
                    f_Config["Engines/" + f_EngineName + "/UseSshTunnel"];
                f_ConnectionWidget.SshHostEntry.Text = (string)
                    f_Config["Engines/" + f_EngineName + "/SshHostname"];
                f_ConnectionWidget.SshPortSpinButton.Value = (double)(int)
                    f_Config["Engines/" + f_EngineName + "/SshPort"];

                f_ConnectionWidget.HostEntry.Text = (string)
                    f_Config["Engines/" + f_EngineName + "/Hostname"];
                f_ConnectionWidget.PortSpinButton.Value = (double)(int)
                    f_Config["Engines/" + f_EngineName + "/Port"];
            }
        }

        private void CheckConnectionPage()
        {
            bool isComplete = true;
            if (f_ConnectionWidget.UseSshTunnelCheckButton.Active &&
                f_ConnectionWidget.SshHostEntry.Text.Trim().Length == 0) {
                isComplete = false;
            }
            if (f_ConnectionWidget.HostEntry.Text.Trim().Length == 0) {
                isComplete = false;
            }
            SetPageComplete(f_ConnectionWidget, isComplete);
        }

        private void InitCredentialsPage()
        {
            f_CredentialsWidget = new EngineAssistantCredentialsWidget();

            AppendPage(f_CredentialsWidget);
            SetPageTitle(f_CredentialsWidget, _("Credentials"));
            SetPageType(f_CredentialsWidget, Gtk.AssistantPageType.Content);
            Prepare += delegate(object sender, Gtk.PrepareArgs e) {
                if (e.Page != f_CredentialsWidget) {
                    return;
                }
                CheckCredentialsPage();
            };

            f_CredentialsWidget.SshUsernameEntry.Changed += delegate {
                CheckCredentialsPage();
            };
            f_CredentialsWidget.UsernameEntry.Changed += delegate {
                CheckCredentialsPage();
            };
            f_CredentialsWidget.PasswordEntry.Changed += delegate {
                CheckCredentialsPage();
            };
            f_CredentialsWidget.VerifyPasswordEntry.Changed += delegate {
                CheckCredentialsPage();
            };

            // HACK: only show the SSH password field if plink is present as
            // OpenSSH doesn't support passing passwords via command line
            f_CredentialsWidget.SshPasswordVBox.Visible = File.Exists("plink.exe");

            if (f_EngineName != null) {
                f_CredentialsWidget.SshUsernameEntry.Text = (string)
                    f_Config["Engines/" + f_EngineName + "/SshUsername"];
                f_CredentialsWidget.SshPasswordEntry.Text = (string)
                    f_Config["Engines/" + f_EngineName + "/SshPassword"];
                var sshKeyfile = (string)
                    f_Config["Engines/" + f_EngineName + "/SshKeyfile"];
                if (!String.IsNullOrEmpty(sshKeyfile)) {
                    f_CredentialsWidget.SshKeyfileChooserButton.SetFilename(
                        sshKeyfile
                    );
                }
                var sshPath = IOPath.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.Personal
                    ),
                    ".ssh"
                );
                if (Directory.Exists(sshPath)) {
                    f_CredentialsWidget.SshKeyfileChooserButton.SetCurrentFolder(
                        sshPath
                    );
                }
                f_CredentialsWidget.UsernameEntry.Text = (string)
                    f_Config["Engines/" + f_EngineName + "/Username"];
                f_CredentialsWidget.PasswordEntry.Text = (string)
                    f_Config["Engines/" + f_EngineName + "/Password"];
                f_CredentialsWidget.VerifyPasswordEntry.Text = (string)
                    f_Config["Engines/" + f_EngineName + "/Password"];
            }
        }

        private void CheckCredentialsPage()
        {
            bool useSsh = f_ConnectionWidget.UseSshTunnelCheckButton.Active;
            f_CredentialsWidget.SshUsernameEntry.Sensitive = useSsh;
            if (!useSsh) {
                f_CredentialsWidget.SshUsernameEntry.Text = String.Empty;
            }

            bool isComplete = true;
            if (f_CredentialsWidget.UsernameEntry.Text.Trim().Length == 0 ||
                f_CredentialsWidget.PasswordEntry.Text.Trim().Length == 0) {
                isComplete = false;
            }
            if (f_CredentialsWidget.PasswordEntry.Text !=
                f_CredentialsWidget.VerifyPasswordEntry.Text) {
                isComplete = false;
            }
            SetPageComplete(f_CredentialsWidget, isComplete);
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
                // check if an engine wit that name exists already
                string[] engines = (string[]) f_Config["Engines/Engines"];
                foreach (string oldEngine in engines) {
                    if (engine == oldEngine) {
                        Gtk.MessageDialog md = new Gtk.MessageDialog(this,
                            Gtk.DialogFlags.Modal, Gtk.MessageType.Error,
                            Gtk.ButtonsType.Close, _("An engine with this name already exists! Please specify a different one."));
                        md.Run();
                        md.Destroy();

                        // jump back to the name page
                        // HACK: assistant API is buggy here, the "Apply" button
                        // will trigger a next page signal, thus we have to jump
                        // to one page before the name page :(
                        CurrentPage = f_NamePage - 1;
                        return;
                    }
                }

                string[] newEngines;
                if (engines.Length == 0) {
                    // there was no existing engines
                    newEngines = new string[] { engine };
                } else {
                    newEngines = new string[engines.Length + 1];
                    engines.CopyTo(newEngines, 0);
                    newEngines[engines.Length] = engine;
                }

                if (engines.Length == 1) {
                    f_Config["Engines/Default"] = engine;
                }
                f_Config["Engines/Engines"] = newEngines;
            }

            if (f_NameWidget.MakeDefaultEngineCheckButton.Active) {
                f_Config["Engines/Default"] = engine;
            }

            f_Config["Engines/"+engine+"/Username"] =
                f_CredentialsWidget.UsernameEntry.Text.Trim();
            f_Config["Engines/"+engine+"/Password"] =
                f_CredentialsWidget.PasswordEntry.Text.Trim();
            f_Config["Engines/"+engine+"/Hostname"] =
                f_ConnectionWidget.HostEntry.Text.Trim();
            f_Config["Engines/"+engine+"/Port"] =
                f_ConnectionWidget.PortSpinButton.ValueAsInt;
            
            f_Config["Engines/"+engine+"/UseSshTunnel"] =
                    f_ConnectionWidget.UseSshTunnelCheckButton.Active;
            f_Config["Engines/"+engine+"/SshUsername"] =
                f_CredentialsWidget.SshUsernameEntry.Text.Trim();
            if (f_CredentialsWidget.SshPasswordVBox.Visible) {
                f_Config["Engines/"+engine+"/SshPassword"] =
                    f_CredentialsWidget.SshPasswordEntry.Text;
            }
            f_Config["Engines/"+engine+"/SshKeyfile"] =
                f_CredentialsWidget.SshKeyfileChooserButton.Filename ?? String.Empty;
            f_Config["Engines/"+engine+"/SshHostname"] =
                f_ConnectionWidget.SshHostEntry.Text.Trim();
            f_Config["Engines/"+engine+"/SshPort"] =
                f_ConnectionWidget.SshPortSpinButton.ValueAsInt;

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
