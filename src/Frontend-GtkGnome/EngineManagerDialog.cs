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
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using Meebey.Smuxi.Engine;
#if CHANNEL_TCPEX
using TcpEx;
#endif
#if CHANNEL_BIRDIRTCP
using DotNetRemotingCC.Channels.BidirectionalTCP;
#endif

namespace Meebey.Smuxi.FrontendGtkGnome
{
    public class EngineManagerDialog : Gtk.Dialog
    {
#if GTK_1
        private Gtk.Combo    _Combo;
#elif GTK_2
        private Gtk.ComboBox _ComboBox;
#endif
        private string       _SelectedEngine;  
        
        public EngineManagerDialog()
        {
            Modal = true;
            Title = "smuxi - Engine Manager";
            AddButton("Connect" , 1);
            AddActionWidget(new Gtk.Button(Gtk.Stock.New), 3);
            AddButton("Edit", 2);
            AddActionWidget(new Gtk.Button(Gtk.Stock.Delete), 4);
            AddActionWidget(new Gtk.Button(Gtk.Stock.Quit), 5);
            Response += new Gtk.ResponseHandler(_OnResponse);
            
            Gtk.VBox vbox = new Gtk.VBox();
            Gtk.Label label = new Gtk.Label("<b>Select to which smuxi engine you want to connect</b>");
            label.UseMarkup = true;
            vbox.PackStart(label, false, false, 5);
            
            Gtk.HBox hbox = new Gtk.HBox();
            hbox.PackStart(new Gtk.Label("Engine:"), false, false, 5);

#if GTK_1
            Gtk.Combo c = new Gtk.Combo();
            _Combo = c;
            c.DisableActivate();
#elif GTK_2
            Gtk.ComboBox cb = Gtk.ComboBox.NewText();
            _ComboBox = cb;
            cb.Changed += new EventHandler(_OnComboBoxChanged);
#endif
            string[] engines = (string[])Frontend.FrontendConfig["Engines/Engines"];
#if GTK_2
            string default_engine = (string)Frontend.FrontendConfig["Engines/Default"];
            int item = 0;
            foreach (string engine in engines) {
                cb.AppendText(engine);
                if (engine == default_engine) {
                    cb.Active = item;
                }
                item++;
            }
#endif

#if GTK_1
            c.PopdownStrings = engines;

            hbox.PackStart(c, true, true, 10); 
#elif GTK_2
            hbox.PackStart(cb, true, true, 10); 
#endif
            
            vbox.PackStart(hbox, false, false, 10);
            
            VBox.Add(vbox);
            
            ShowAll();
        }
        
        private void _OnResponse(object sender, Gtk.ResponseArgs e)
        {
#if LOG4NET
            Logger.UI.Debug("ResponseId: "+e.ResponseId);
#endif                
            switch ((int)e.ResponseId) {
                case 1:
                    _OnConnectButtonPressed();
                    break;
#if UI_GNOME
                case 3:
                    _OnNewButtonPressed();
                    break;
#endif
                case 4:
                    _OnDeleteButtonPressed();
                    break;
                case 5:
                    _OnQuitButtonPressed();
                    break;
                case (int)Gtk.ResponseType.DeleteEvent:
                    _OnDeleteEvent();
                    break;
                default:
                    new NotImplementedMessageDialog();
                    // Re-run the Dialog
                    Run();
                    break;
            }
        }
        
        private void _OnConnectButtonPressed()
        {
#if GTK_1
            _SelectedEngine = _Combo.Entry.Text;
#endif
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
                        if (ChannelServices.GetChannel("tcp") == null) {
                            ChannelServices.RegisterChannel(new TcpChannel());
                        }
#if LOG4NET
                        Logger.Main.Info("Connecting to: "+connection_url);
#endif
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            connection_url);
                        break;
#if CHANNEL_TCPEX
                    case "TcpEx":
                        connection_url = "tcpex://"+hostname+":"+port+"/SessionManager"; 
                        if (ChannelServices.GetChannel("tcpex") == null) {
                            ChannelServices.RegisterChannel(new TcpExChannel());
                        }
#if LOG4NET
                        Logger.Main.Info("Connecting to: "+connection_url);
#endif
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            connection_url);
                        break;
#endif
#if CHANNEL_BIRDIRTCP
                    case "BirDirTcp":
                        string ip = System.Net.Dns.Resolve(hostname).AddressList[0].ToString();
                        connection_url = "birdirtcp://"+ip+":"+port+"/SessionManager"; 
                        if (ChannelServices.GetChannel("birdirtcp") == null) {
                            ChannelServices.RegisterChannel(new BidirTcpClientChannel());
                        }
#if LOG4NET
                        Logger.Main.Info("Connecting to: "+connection_url);
#endif
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            connection_url);
                        break;
#endif
                    case "HTTP":
                        connection_url = "http://"+hostname+":"+port+"/SessionManager"; 
                        if (ChannelServices.GetChannel("http") == null) {
                            ChannelServices.RegisterChannel(new HttpChannel());
                        }
#if LOG4NET
                        Logger.Main.Info("Connecting to: "+connection_url);
#endif
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            connection_url);
                        break;
                    default:
                        error_msg += "Unknown channel ("+channel+"), "+
                                    "only following channel types are supported: HTTP and TCP\n";
                        break;
                }
                // sessm can be null when there was an unknown channel used
                if (sessm != null) {
                    Frontend.Session = sessm.Register(username, password, Frontend.UI);
                    if (Frontend.Session != null) {
                        // Dialog finished it's job, we are connected
                        Frontend.InitGUI();
                        Destroy();
                    } else {
                        error_msg += "Registration at engine failed, "+
                                    "username and/or password was wrong, please verify them.\n";
                    }
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
                    msg += (connection_url != null ? "Engine URL: "+connection_url+"\n" : String.Empty);
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

#if UI_GNOME
        private void _OnNewButtonPressed()
        {
            // the druid will spawn EngineManagerDialog when it's canceled or finished
            Destroy();
            new NewEngineDruid();
        }
#endif

        private void _OnDeleteButtonPressed()
        {
            string msg = "Are you sure you want to delete the engine \""+
                _SelectedEngine+"\"?";
            Gtk.MessageDialog md = new Gtk.MessageDialog(this, Gtk.DialogFlags.Modal,
            Gtk.MessageType.Warning, Gtk.ButtonsType.YesNo, msg);
            int res = md.Run();
            if ((Gtk.ResponseType)res == Gtk.ResponseType.Yes) {
                _DeleteEngine(_SelectedEngine);
                Destroy();
                new EngineManagerDialog();
            }
            md.Destroy();
        }
        
        private void _DeleteEngine(string engine)
        {
            StringCollection new_engines = new StringCollection();
            string[] current_engines = (string[])Frontend.FrontendConfig["Engines/Engines"];
            foreach (string eng in current_engines) {
                if (eng != engine) {
                    new_engines.Add(eng);
                }
            }
            string[] new_engines_array = new string[new_engines.Count]; 
            new_engines.CopyTo(new_engines_array, 0);
            Frontend.FrontendConfig["Engines/Engines"] = new_engines_array;
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Username");
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Password");
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Hostname");
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Port");
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Channel");
            Frontend.FrontendConfig.Remove("Engines/"+engine+"/Formatter");
            Frontend.FrontendConfig.Remove("Engines/"+engine);
            Frontend.FrontendConfig.Save();
            Frontend.FrontendConfig.Load();
        }
        
        private void _OnQuitButtonPressed()
        {
            Frontend.Quit();
        }
        
        private void _OnDeleteEvent()
        {
            Frontend.Quit();
        }
        
#if GTK_2
        private void _OnComboBoxChanged(object sender, EventArgs e)
        {
            Gtk.TreeIter iter;
            if (_ComboBox.GetActiveIter(out iter)) {
               _SelectedEngine = (string)_ComboBox.Model.GetValue(iter, 0);
            }
        }
#endif
        
    }
}
