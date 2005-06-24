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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using Meebey.Smuxi.Engine;

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class EngineManagerDialog : Gtk.Dialog
    {
        private Gtk.ComboBox _ComboBox;
        private string       _SelectedEngine;  
        
        public EngineManagerDialog()
        {
            Modal = true;
            Title = "smuxi - Engine Manager";
            //SetDefaultSize(320, 240);
            AddButton("Connect", 1);
            AddButton("Edit", 2);
            AddButton("New", 3);
            AddButton("Delete", 4);
            Response += new Gtk.ResponseHandler(_OnResponse);
            Close += new EventHandler(_OnClose);
            
            Gtk.VBox vbox = new Gtk.VBox();
            vbox.PackStart(new Gtk.Label("Select to which smuxi engine you want to connect"), false, false, 5);
            
            Gtk.HBox hbox = new Gtk.HBox();
            hbox.PackStart(new Gtk.Label("Engine:"), false, false, 5);
                                       
            Gtk.ComboBox cb = Gtk.ComboBox.NewText();
            _ComboBox = cb;
            cb.Changed += new EventHandler(_OnComboBoxChanged);
            string default_engine = (string)Frontend.FrontendConfig["Engines/Default"];
            int item = 0;
            foreach (string engine in (string[])Frontend.FrontendConfig["Engines/Engines"]) {
                cb.AppendText(engine);
                if (engine == default_engine) {
                    cb.Active = item;
                }
                item++;
            }
            hbox.PackStart(cb, true, true, 10); 
            
            vbox.PackStart(hbox, false, false, 10);
            
            VBox.Add(vbox);
            
            ShowAll();
        }
        
        private void _OnResponse(object sender, Gtk.ResponseArgs e)
        {
            switch ((int)e.ResponseId) {
                case 1:
                    _OnConnectButtonPressed();
                    break;
                default:
                    Gtk.MessageDialog md = new Gtk.MessageDialog(this, Gtk.DialogFlags.Modal,
                        Gtk.MessageType.Info, Gtk.ButtonsType.Close, "Sorry, not implemented yet!");
                    md.Run();
                    md.Destroy();
                    
                    // Re-run the Dialog
                    Run();
                    break;
            }
        }
        
        private void _OnConnectButtonPressed()
        {
            string engine = _SelectedEngine;
            string username = (string)Frontend.FrontendConfig["Engines/"+engine+"/Username"];
            string password = (string)Frontend.FrontendConfig["Engines/"+engine+"/Password"];
            string hostname = (string)Frontend.FrontendConfig["Engines/"+engine+"/Hostname"];
            int port = (int)Frontend.FrontendConfig["Engines/"+engine+"/Port"];
            //string formatter = (string)FrontendConfig["Engines/"+engine+"/Formatter"];
            string channel = (string)Frontend.FrontendConfig["Engines/"+engine+"/Channel"];
            
            string error_msg = null;
            string connection_url = null;
            try {
                SessionManager sessm = null;
                switch (channel) {
                    case "TCP":
                        connection_url = "tcp://"+hostname+":"+port+"/SessionManager"; 
#if LOG4NET
                        Logger.Main.Info("Connecting to: "+connection_url);
#endif
                        if (ChannelServices.GetChannel("tcp") == null) {
                            ChannelServices.RegisterChannel(new TcpChannel());
                        }
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            connection_url);
                        break;
#if CHANNEL_TCPEX
                    case "TcpEx":
                        connection_url = "tcpex://"+hostname+":"+port+"/SessionManager"; 
#if LOG4NET
                        Logger.Main.Info("Connecting to: "+connection_url);
#endif
                        if (ChannelServices.GetChannel("tcpex") == null) {
                            ChannelServices.RegisterChannel(new TcpExChannel());
                        }
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            connection_url);
                        break;
#endif
                    case "HTTP":
                        connection_url = "http://"+hostname+":"+port+"/SessionManager"; 
#if LOG4NET
                        Logger.Main.Info("Connecting to: "+connection_url);
#endif
                        if (ChannelServices.GetChannel("http") == null) {
                            ChannelServices.RegisterChannel(new HttpChannel());
                        }
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            connection_url);
                        break;
                    default:
                        error_msg += "Unknown channel ("+channel+"), "+
                                    "only following channel types are supported: HTTP and TCP\n";
                        break;
                }
                Frontend.Session = sessm.Register(username, password, Frontend.UI);
                if (Frontend.Session == null) {
                    error_msg += "Registration at engine failed, "+
                                "username and/or password was wrong, please verify them.\n";
                } else {
                    // Dialog finished it's job
                    Destroy();
                }
            } catch (Exception ex) {
                error_msg += ex.Message+"\n";
                if (ex.InnerException != null) {
                    error_msg += " ["+ex.InnerException.Message+"]\n";
                }
            } finally {
                if (error_msg != null) {
                    string msg;
                    msg = "Error occured while connecting to the engine!\n\n";
                    msg += (connection_url != null ? "Engine URL: "+connection_url+"\n" : "");
                    msg += "Error: "+error_msg;
                    
                    Gtk.MessageDialog md = new Gtk.MessageDialog(this, Gtk.DialogFlags.Modal,
                        Gtk.MessageType.Error, Gtk.ButtonsType.Close, msg);
                    md.Run();
                    md.Destroy();
                    
                    // Re-run the Dialog
                    Run();
                }
            }
        }
        
        private void _OnComboBoxChanged(object sender, EventArgs e)
        {
            Gtk.TreeIter iter;
            if (_ComboBox.GetActiveIter(out iter)) {
               _SelectedEngine = (string)_ComboBox.Model.GetValue(iter, 0);
            }
        }
        
        private void _OnClose(object sender, EventArgs e)
        {
            Frontend.Quit();
        }
    }
}
