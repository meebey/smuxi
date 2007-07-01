/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
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
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Mono.Unix;
using Smuxi.Engine;
using Smuxi.Common;
//using Smuxi.Channels.Tcp;
#if CHANNEL_TCPEX
using TcpEx;
#endif
#if CHANNEL_BIRDIRTCP
using DotNetRemotingCC.Channels.BidirectionalTCP;
#endif

namespace Smuxi.Frontend.Gnome
{
    public class EngineManagerDialog : Gtk.Dialog
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private Gtk.ComboBox _ComboBox;
        private string       _SelectedEngine;  
        
        public EngineManagerDialog()
        {
            Modal = true;
            Title = "smuxi - " + _("Engine Manager");
                
            Gtk.HBox connect_hbox = new Gtk.HBox();
            Gtk.Image connect_image = new Gtk.Image(new Gdk.Pixbuf(null,
                "connect.png"));
            connect_hbox.Add(connect_image);
            connect_hbox.Add(new Gtk.Label(_("_Connect")));
            Gtk.Button connect_button = new Gtk.Button(connect_hbox);
            AddActionWidget(connect_button, 1);
            
            AddActionWidget(new Gtk.Button(Gtk.Stock.New), 3);
            
            Gtk.HBox edit_hbox = new Gtk.HBox();
            Gtk.Image edit_image = new Gtk.Image(new Gdk.Pixbuf(null,
                "edit.png"));
            edit_hbox.Add(edit_image);
            edit_hbox.Add(new Gtk.Label(Catalog.GetString("_Edit")));
            Gtk.Button edit_button = new Gtk.Button(edit_hbox);
            AddActionWidget(edit_button, 2);
            
            AddActionWidget(new Gtk.Button(Gtk.Stock.Delete), 4);
            AddActionWidget(new Gtk.Button(Gtk.Stock.Quit), 5);
            Response += new Gtk.ResponseHandler(_OnResponse);
            
            Gtk.VBox vbox = new Gtk.VBox();
            Gtk.Label label = new Gtk.Label("<b>" + 
                                            Catalog.GetString("Select to which smuxi engine you want to connect") +
                                            "</b>");
            label.UseMarkup = true;
            vbox.PackStart(label, false, false, 5);
            
            Gtk.HBox hbox = new Gtk.HBox();
            hbox.PackStart(new Gtk.Label(Catalog.GetString("Engine:")), false, false, 5);
            
            Gtk.ComboBox cb = Gtk.ComboBox.NewText();
            _ComboBox = cb;
            cb.Changed += new EventHandler(_OnComboBoxChanged);
            string[] engines = (string[])Frontend.FrontendConfig["Engines/Engines"];
            string default_engine = (string)Frontend.FrontendConfig["Engines/Default"];
            int item = 0;
            cb.AppendText("<" + _("Local Engine") + ">");
            item++;
            foreach (string engine in engines) {
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
            Trace.Call(sender, e);
            try {
#if LOG4NET
                _Logger.Debug("_OnResponse(): ResponseId: "+e.ResponseId);
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
                        NotImplementedMessageDialog nid = new NotImplementedMessageDialog(this);
                        nid.Run();
                        nid.Destroy();
                        
                        // Re-run the Dialog
                        Run();
                        break;
                }
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif
                CrashDialog.Show(this, ex);
            }
        }
        
        private void _OnConnectButtonPressed()
        {
            if (_SelectedEngine == null || _SelectedEngine == String.Empty) {
                Gtk.MessageDialog md = new Gtk.MessageDialog(this,
                    Gtk.DialogFlags.Modal, Gtk.MessageType.Error,
                    Gtk.ButtonsType.Close, Catalog.GetString("Please select an engine!"));
                md.Run();
                md.Destroy();
                // Re-run the Dialog
                Run();
                return;
            }
            
            if (_SelectedEngine == "<" + _("Local Engine") + ">") {
                Frontend.InitLocalEngine();
                Destroy();
                return;
            }

            string engine = _SelectedEngine;
            string username = (string)Frontend.FrontendConfig["Engines/"+engine+"/Username"];
            string password = (string)Frontend.FrontendConfig["Engines/"+engine+"/Password"];
            string hostname = (string)Frontend.FrontendConfig["Engines/"+engine+"/Hostname"];
            string bindAddress = (string)Frontend.FrontendConfig["Engines/"+engine+"/BindAddress"];
            int port = (int)Frontend.FrontendConfig["Engines/"+engine+"/Port"];
            //string formatter = (string)FrontendConfig["Engines/"+engine+"/Formatter"];
            string channel = (string)Frontend.FrontendConfig["Engines/"+engine+"/Channel"];
            
            IDictionary props = new Hashtable();
            props["port"] = "0";
            string error_msg = null;
            string connection_url = null;
            try {
                SessionManager sessm = null;
                switch (channel) {
                    case "TCP":
                        if (ChannelServices.GetChannel("tcp") == null) {
                            // frontend -> engine
                            BinaryClientFormatterSinkProvider cprovider =
                                new BinaryClientFormatterSinkProvider();

                            // engine -> frontend (back-connection)
                            BinaryServerFormatterSinkProvider sprovider =
                                new BinaryServerFormatterSinkProvider();
                            // required for MS .NET 1.1
                            sprovider.TypeFilterLevel = TypeFilterLevel.Full;
                            
                            if (bindAddress != null) {
                                props["machineName"] = bindAddress;
                            }
                            ChannelServices.RegisterChannel(new TcpChannel(props, cprovider, sprovider));
                        }
                        connection_url = "tcp://"+hostname+":"+port+"/SessionManager"; 
#if LOG4NET
                        _Logger.Info("Connecting to: "+connection_url);
#endif
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            connection_url);
                        break;
#if CHANNEL_TCPEX
                    case "TcpEx":
                        //props.Remove("port");
                        //props["name"] = "tcpex";
                        connection_url = "tcpex://"+hostname+":"+port+"/SessionManager"; 
                        if (ChannelServices.GetChannel("ExtendedTcp") == null) {
                            ChannelServices.RegisterChannel(new TcpExChannel(props, null, null));
                        }
#if LOG4NET
                        _Logger.Info("Connecting to: "+connection_url);
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
                        _Logger.Info("Connecting to: "+connection_url);
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
                        _Logger.Info("Connecting to: "+connection_url);
#endif
                        sessm = (SessionManager)Activator.GetObject(typeof(SessionManager),
                            connection_url);
                        break;
                    default:
                        error_msg += String.Format(
                                        _("Unknown channel ({0}), "+
                                          "only following channel types are supported:"),
                                        channel) + " HTTP TCP\n";
                        break;
                }
                // sessm can be null when there was an unknown channel used
                if (sessm == null) {
                    return;
                }
                
                if (sessm.EngineVersion.Major != Frontend.Version.Major ||
                    sessm.EngineVersion.Minor != Frontend.Version.Minor ||
                    sessm.EngineVersion.Build != Frontend.Version.Build) {                        
                        error_msg += String.Format(
                                        _("Your frontend version ({0}) is not matching the engine version ({1})!"),
                                        Frontend.Version, sessm.EngineVersion);
                    return;
                }
                
                Frontend.Session = sessm.Register(username, MD5.FromString(password), Frontend.MainWindow.UI);
                Frontend.EngineVersion = sessm.EngineVersion;
                if (Frontend.Session != null) {
                    // Dialog finished it's job, we are connected
                    Frontend.UserConfig = new UserConfig(Frontend.Session.Config,
                                                         username);
                    Frontend.UserConfig.IsCaching = true;
                    Frontend.ConnectEngineToGUI();
                    Destroy();
                } else {
                    error_msg += _("Registration at engine failed, "+
                                   "username and/or password was wrong, please verify them.") + "\n";
                }
            } catch (Exception ex) {
#if LOG4NET
                _Logger.Error(ex);
#endif
                error_msg += ex.Message + "\n";
                if (ex.InnerException != null) {
                    error_msg += " [" + ex.InnerException.Message + "]\n";
                }
            } finally {
                if (error_msg != null) {
                    string msg;
                    msg = _("Error occured while connecting to the engine!") + "\n\n";
                    if (connection_url != null) {
                        msg += String.Format(
                                _("Engine URL: {0}") + "\n",
                                connection_url);
                    }
                    msg += String.Format(_("Error: {0}"), error_msg);
                    
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
            string msg = String.Format(
                            _("Are you sure you want to delete the engine \"{0}\"?"),
                            _SelectedEngine);
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
        
        private void _OnComboBoxChanged(object sender, EventArgs e)
        {
            Trace.Call(sender, e);
            
            Gtk.TreeIter iter;
            if (_ComboBox.GetActiveIter(out iter)) {
               _SelectedEngine = (string)_ComboBox.Model.GetValue(iter, 0);
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
