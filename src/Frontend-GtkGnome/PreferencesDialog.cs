/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
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
    public class PreferencesDialog
    {
        private Gtk.Dialog _Dialog;
        private Glade.XML  _Glade;
        
        public PreferencesDialog()
        {
            _Glade = new Glade.XML(null, "preferences.glade", "PreferencesDialog", null);
            _Glade.Autoconnect(this);
            _Dialog = (Gtk.Dialog)_Glade["PreferencesDialog"];
            ((Gtk.TextView)_Glade["OnConnectCommandsTextView"]).Buffer.Changed += new EventHandler(_OnChanged);
            ((Gtk.TextView)_Glade["OnStartupCommandsTextView"]).Buffer.Changed += new EventHandler(_OnChanged);
            _Load();
        }
        
        private void _Load()
        {
            // root
            string startup_commands = String.Join("\n", (string[])Frontend.UserConfig["OnStartupCommands"]);
            ((Gtk.TextView)_Glade["OnStartupCommandsTextView"]).Buffer.Text  = startup_commands;
            
            // Connection
            string nicknames = String.Join(" ", (string[])Frontend.UserConfig["Connection/Nicknames"]);
            ((Gtk.Entry)_Glade["ConnectionNicknamesEntry"]).Text  = nicknames;
            ((Gtk.Entry)_Glade["ConnectionUsernameEntry"]).Text  = (string)Frontend.UserConfig["Connection/Username"];
            ((Gtk.Entry)_Glade["ConnectionRealnameEntry"]).Text  = (string)Frontend.UserConfig["Connection/Realname"];
            string connect_commands = String.Join("\n", (string[])Frontend.UserConfig["Connection/OnConnectCommands"]);
            ((Gtk.TextView)_Glade["OnConnectCommandsTextView"]).Buffer.Text = connect_commands;
                    
            // Interface
            ((Gtk.Entry)_Glade["TimestampFormatEntry"]).Text =
                (string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"];
                
            // Interface/Notebook
            ((Gtk.SpinButton)_Glade["BufferLinesSpinButton"]).Value =
                (double)(int)Frontend.UserConfig["Interface/Notebook/BufferLines"];
            ((Gtk.SpinButton)_Glade["EngineBufferLinesSpinButton"]).Value =
                (double)(int)Frontend.UserConfig["Interface/Notebook/EngineBufferLines"];
            switch ((string)Frontend.UserConfig["Interface/Notebook/TabPosition"]) {
                case "top":
                    ((Gtk.RadioButton)_Glade["TabPositionRadioButtonTop"]).Active = true;
                break;
                case "bottom":
                    ((Gtk.RadioButton)_Glade["TabPositionRadioButtonBottom"]).Active = true;
                break;
                case "left":
                    ((Gtk.RadioButton)_Glade["TabPositionRadioButtonLeft"]).Active = true;
                break;
                case "right":
                    ((Gtk.RadioButton)_Glade["TabPositionRadioButtonRight"]).Active = true;
                break;
                case "none":
                    ((Gtk.RadioButton)_Glade["TabPositionRadioButtonNone"]).Active = true;
                break;
            }
            switch ((string)Frontend.UserConfig["Interface/Notebook/Channel/UserListPosition"]) {
                case "left":
                    ((Gtk.RadioButton)_Glade["UserListPositionRadioButtonLeft"]).Active = true;
                break;
                case "right":
                    ((Gtk.RadioButton)_Glade["UserListPositionRadioButtonRight"]).Active = true;
                break;
                case "none":
                    ((Gtk.RadioButton)_Glade["UserListPositionRadioButtonNone"]).Active = true;
                break;
            }
            switch ((string)Frontend.UserConfig["Interface/Notebook/Channel/TopicPosition"]) {
                case "top":
                    ((Gtk.RadioButton)_Glade["TopicPositionRadioButtonTop"]).Active = true;
                break;
                case "bottom":
                    ((Gtk.RadioButton)_Glade["TopicPositionRadioButtonBottom"]).Active = true;
                break;
                case "none":
                    ((Gtk.RadioButton)_Glade["TopicPositionRadioButtonNone"]).Active = true;
                break;
            }

            // Interface/Entry
            ((Gtk.Entry)_Glade["CompletionCharacterEntry"]).Text =
                (string)Frontend.UserConfig["Interface/Entry/CompletionCharacter"];
            ((Gtk.Entry)_Glade["CommandCharacterEntry"]).Text =
                (string)Frontend.UserConfig["Interface/Entry/CommandCharacter"];
            ((Gtk.CheckButton)_Glade["BashStyleCompletionCheckButton"]).Active =
                (bool)Frontend.UserConfig["Interface/Entry/BashStyleCompletion"];
            ((Gtk.SpinButton)_Glade["CommandHistorySizeSpinButton"]).Value =
                (double)(int)Frontend.UserConfig["Interface/Entry/CommandHistorySize"];

            ((Gtk.Button)_Glade["ApplyButton"]).Sensitive = false;
        }
        
        private void _Save()
        {
            // root
            Frontend.UserConfig["OnStartupCommands"] = 
                ((Gtk.TextView)_Glade["OnStartupCommandsTextView"]).Buffer.Text.Split(new char[] {'\n'});
                
            // Connection
            Frontend.UserConfig["Connection/Nicknames"] = 
                ((Gtk.Entry)_Glade["ConnectionNicknamesEntry"]).Text.Split(new char[] {' '});
            Frontend.UserConfig["Connection/Username"] = 
                ((Gtk.Entry)_Glade["ConnectionUsernameEntry"]).Text;
            Frontend.UserConfig["Connection/Realname"] = 
                ((Gtk.Entry)_Glade["ConnectionRealnameEntry"]).Text;
            Frontend.UserConfig["Connection/OnConnectCommands"] = 
                ((Gtk.TextView)_Glade["OnConnectCommandsTextView"]).Buffer.Text.Split(new char[] {'\n'});
            
            // Interface
            Frontend.UserConfig["Interface/Notebook/TimestampFormat"] =
                ((Gtk.Entry)_Glade["TimestampFormatEntry"]).Text;
                
            Frontend.UserConfig["Interface/Notebook/BufferLines"] =
                (int)((Gtk.SpinButton)_Glade["BufferLinesSpinButton"]).Value;
            Frontend.UserConfig["Interface/Notebook/EngineBufferLines"] =
                (int)((Gtk.SpinButton)_Glade["EngineBufferLinesSpinButton"]).Value;
                
            string tab_position = null;
            if (((Gtk.RadioButton)_Glade["TabPositionRadioButtonTop"]).Active) {
                tab_position = "top";
            } else if (((Gtk.RadioButton)_Glade["TabPositionRadioButtonBottom"]).Active) {
                tab_position = "bottom";
            } else if (((Gtk.RadioButton)_Glade["TabPositionRadioButtonLeft"]).Active) {
                tab_position = "left";
            } else if (((Gtk.RadioButton)_Glade["TabPositionRadioButtonRight"]).Active) {
                tab_position = "right";
            } else if (((Gtk.RadioButton)_Glade["TabPositionRadioButtonNone"]).Active) {
                tab_position = "none";
            }
            Frontend.UserConfig["Interface/Notebook/TabPosition"] = tab_position;
            
            string userlist_position = null;
             if (((Gtk.RadioButton)_Glade["UserListPositionRadioButtonLeft"]).Active) {
                userlist_position = "left";
            } else if (((Gtk.RadioButton)_Glade["UserListPositionRadioButtonRight"]).Active) {
                userlist_position = "right";
            } else if (((Gtk.RadioButton)_Glade["UserListPositionRadioButtonNone"]).Active) {
                userlist_position = "none";
            }
            Frontend.UserConfig["Interface/Notebook/Channel/UserListPosition"] = userlist_position;

            string topic_position = null;
             if (((Gtk.RadioButton)_Glade["TopicPositionRadioButtonTop"]).Active) {
                topic_position = "top";
            } else if (((Gtk.RadioButton)_Glade["TopicPositionRadioButtonBottom"]).Active) {
                topic_position = "bottom";
            } else if (((Gtk.RadioButton)_Glade["TopicPositionRadioButtonNone"]).Active) {
                topic_position = "none";
            }
            Frontend.UserConfig["Interface/Notebook/Channel/TopicPosition"] = topic_position;
            
            // Entry
            Frontend.UserConfig["Interface/Entry/CompletionCharacter"] =
                ((Gtk.Entry)_Glade["CompletionCharacterEntry"]).Text;
            Frontend.UserConfig["Interface/Entry/CommandCharacter"]   =
                ((Gtk.Entry)_Glade["CommandCharacterEntry"]).Text;
            Frontend.UserConfig["Interface/Entry/BashStyleCompletion"] =
                ((Gtk.CheckButton)_Glade["BashStyleCompletionCheckButton"]).Active;
            Frontend.UserConfig["Interface/Entry/CommandHistorySize"] =
                (int)((Gtk.SpinButton)_Glade["CommandHistorySizeSpinButton"]).Value;
            
            Frontend.Config.Save();
        }
        
        private void _OnChanged(object obj, EventArgs args)
        {
            ((Gtk.Button)_Glade["ApplyButton"]).Sensitive = true;
        }
        
        private void _OnOKButtonClicked(object obj, EventArgs args)
        {
            _Save();
            Frontend.Config.Load();
            _Dialog.Destroy();
        }

        private void _OnApplyButtonClicked(object obj, EventArgs args)
        {
            _Save();
            _Load();
            Frontend.Config.Load();
        }

        private void _OnCancelButtonClicked(object obj, EventArgs args)
        {
            _Dialog.Destroy();
        }
    }
}
