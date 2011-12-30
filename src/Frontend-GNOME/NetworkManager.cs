// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2011 
// 
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

#if IPC_DBUS
using System;
    #if DBUS_SHARP
using DBus;
    #else
using NDesk.DBus;
    #endif
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    public enum StateNM8 : int {
        Unknown = 0,
        Asleep,
        Connecting,
        Connected,
        Disconnected
    }

    public enum StateNM9 : int {
        Unknown         = 0,
        Asleep          = 10,
        Disconnected    = 20,
        Disconnecting   = 30,
        Connecting      = 40,
        ConnectedLocal  = 50,
        ConnectedSite   = 60,
        ConnectedGlobal = 70
    }

    public delegate void StateChangedEventHandler(int state);

    [Interface("org.freedesktop.NetworkManager")]
    public interface INetworkManager
    {
        string Version();
        event StateChangedEventHandler StateChanged;
    }

    public class NetworkManager
    {
#if LOG4NET
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        const string BusName = "org.freedesktop.NetworkManager";
        const string ObjectPath = "/org/freedesktop/NetworkManager";
        INetworkManager Manager { get; set; }
        ChatViewManager ChatViewManager { get; set; }
        bool IsInitialized { get; set; }
        bool WasLocalEngine { get; set; }

        public NetworkManager(ChatViewManager chatViewManager)
        {
            if (chatViewManager == null) {
                throw new ArgumentNullException("mainWindow");
            }

            ChatViewManager = chatViewManager;

            try {
                Init();
            } catch (Exception ex) {
#if LOG4NET
                Logger.Error("NetworkManager(): initialization failed: ", ex);
#endif
            }
        }

        void Init()
        {
            BusG.Init();

            if (!Bus.System.NameHasOwner(BusName)) {
                return;
            }

            Manager = Bus.System.GetObject<INetworkManager>(
                BusName, new ObjectPath(ObjectPath)
            );
            Manager.StateChanged += OnStateChanged;

            IsInitialized = true;
        }

        void OnStateChanged(int state)
        {
            Trace.Call(state);

            if (!Frontend.HadSession) {
                return;
            }

            switch (state) {
                case (int) StateNM9.Disconnecting:
                    if (!Frontend.IsLocalEngine) {
                        Frontend.DisconnectEngineFromGUI(true);
                    }
                    break;
                case (int) StateNM8.Disconnected:
                case (int) StateNM9.Disconnected:
                    WasLocalEngine = Frontend.IsLocalEngine;
                    if (!Frontend.IsLocalEngine) {
                        Frontend.DisconnectEngineFromGUI(false);
                    }
                    break;
                case (int) StateNM8.Connected:
                case (int) StateNM9.ConnectedSite:
                case (int) StateNM9.ConnectedGlobal:
                    if (WasLocalEngine) {
                        // reconnect local protocol managers
                        foreach (var protocolManager in Frontend.Session.ProtocolManagers) {
                            protocolManager.Reconnect(Frontend.FrontendManager);
                        }
                    } else {
                        Frontend.ReconnectEngineToGUI(false);
                    }
                    break;
            }
        }
    }
}
#endif
