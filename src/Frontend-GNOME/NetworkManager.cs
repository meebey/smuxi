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
    public enum State : int {
        Unknown = 0,
        Asleep,
        Connecting,
        Connected,
        Disconnected
    }

    public delegate void StateChangedEventHandler(State state);

    [Interface("org.freedesktop.NetworkManager")]
    public interface INetworkManager
    {
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

        void OnStateChanged(State state)
        {
            Trace.Call(state);

            if (Frontend.Session == null) {
                return;
            }

            switch (state) {
                case State.Disconnected:
                    if (!Frontend.IsLocalEngine) {
                        ChatViewManager.IsSensitive = false;
                    }
                    break;
                case State.Connected:
                    if (Frontend.IsLocalEngine) {
                        // reconnect local protocol managers
                        foreach (var protocolManager in Frontend.Session.ProtocolManagers) {
                            protocolManager.Reconnect(Frontend.FrontendManager);
                        }
                    } else {
                        Frontend.ReconnectEngineToGUI();
                        ChatViewManager.IsSensitive = true;
                    }
                    break;
            }
        }
    }
}
#endif
