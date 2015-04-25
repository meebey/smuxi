/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006, 2009-2014 Mirco Bauer <meebey@meebey.net>
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
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Collections.Generic;
using SysPath = System.IO.Path;
using Smuxi.Engine;
using Smuxi.Common;

namespace Smuxi.Frontend.Gnome
{
    [ChatViewInfo(ChatType = ChatType.Protocol)]
    public class ProtocolChatView : ChatView
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        public static Gdk.Pixbuf IconPixbuf { get; private set; }
        static Dictionary<string, string> NetworkWebsiteUrls { get; set; }
        ProxySettings ProxySettings { get; set; }
        Gdk.Pixbuf ServerIconPixbuf { get; set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public string NetworkID { get; private set; }
        Gtk.ImageMenuItem  ReconnectItem { get; set; }

        protected override Gtk.Image DefaultTabImage {
            get {
                var icon = IconPixbuf;
                if (ServerIconPixbuf != null) {
                    icon = ServerIconPixbuf;
                }
                return new Gtk.Image(icon);
            }
        }

        static ProtocolChatView()
        {
            IconPixbuf = Frontend.LoadIcon(
                "smuxi-protocol-chat", 16, "protocol-chat_256x256.png"
            );
            NetworkWebsiteUrls = new Dictionary<string, string>(
                StringComparer.InvariantCultureIgnoreCase
            );

            // IRC
            NetworkWebsiteUrls.Add("OFTC", "http://www.oftc.net/");
            NetworkWebsiteUrls.Add("freenode", "http://freenode.net/");
            NetworkWebsiteUrls.Add("QuakeNet", "http://www.quakenet.org/");
            NetworkWebsiteUrls.Add("IRCnet", "http://www.ircnet.org/");
            NetworkWebsiteUrls.Add("DALnet", "http://www.dal.net/");
            NetworkWebsiteUrls.Add("GameSurge", "https://gamesurge.net/");
            NetworkWebsiteUrls.Add("EFnet", "http://www.efnet.org/");
            NetworkWebsiteUrls.Add("GIMPnet", "http://www.gimp.org/");
            NetworkWebsiteUrls.Add("GSDnet", "http://www.gsd-software.net/");
            NetworkWebsiteUrls.Add("ustream", "http://www.ustream.tv/");
            NetworkWebsiteUrls.Add("Infinity-IRC", "http://www.infinityirc.com/");
            NetworkWebsiteUrls.Add("GeekShed", "http://www.geekshed.net/");

            // Twitter
            NetworkWebsiteUrls.Add("Twitter", "http://www.twitter.com/");

            // XMPP - with federation
            NetworkWebsiteUrls.Add("XMPP", "http://xmpp.org/");
            NetworkWebsiteUrls.Add("jabber.org", "http://planet.jabber.org/");
            NetworkWebsiteUrls.Add("jabber.de", "http://www.jabber.de/");
            NetworkWebsiteUrls.Add("jabber.at", "http://planet.jabber.org/");
            NetworkWebsiteUrls.Add("jabber.ccc.de", "http://web.jabber.ccc.de/");
            NetworkWebsiteUrls.Add("xmpp-gmx.gmx.net", "http://planet.jabber.org/");
            NetworkWebsiteUrls.Add("xmpp-webde.gmx.net", "http://planet.jabber.org/");
            NetworkWebsiteUrls.Add("jabber.gmx.net", "http://planet.jabber.org/");
            // XMPP - without federation
            NetworkWebsiteUrls.Add("talk.google.com", "http://www.google.com/talk/");
            NetworkWebsiteUrls.Add("chat.facebook.com", "http://www.facebook.com/");

            // JabbR
            NetworkWebsiteUrls.Add("jabbr.net", "http://jabbr.net/");

            // Campfire
            NetworkWebsiteUrls.Add("Campfire", "http://campfirenow.com");

            // support downloading favicons via https
            var whitelist = Session.CertificateValidator.HostnameWhitelist;
            lock (whitelist) {
                foreach (var url in NetworkWebsiteUrls.Values) {
                    var uri = new Uri(url);
                    var hostname = uri.Host;
                    if (whitelist.Contains(hostname)) {
                        continue;
                    }
                    whitelist.Add(hostname);
                }
            }
        }

        public ProtocolChatView(ChatModel chat) : base(chat)
        {
            Trace.Call(chat);
            
            ProxySettings = new ProxySettings();

            Add(OutputScrolledWindow);

            ReconnectItem = new Gtk.ImageMenuItem(_("Reconnect"));
            ReconnectItem.Image = new Gtk.Image(Gtk.Stock.Refresh, Gtk.IconSize.Menu);
            ReconnectItem.Activated += new EventHandler(OnTabMenuReconnectActivated);

            ShowAll();
        }

        protected ProtocolChatView(IntPtr handle) : base(handle)
        {
        }

        public override void Sync(int msgCount)
        {
            Trace.Call(msgCount);

            base.Sync(msgCount);

            Host = ProtocolManager.Host;
            Port = ProtocolManager.Port;
            NetworkID = ProtocolManager.NetworkID;

            try {
                CheckIcon();
            } catch (Exception ex) {
#if LOG4NET
                f_Logger.Error("Sync(): CheckIcon() threw exception!", ex);
#endif
            }
        }

        public override void Close()
        {
            Trace.Call();
            
            // show warning if there are open chats (besides protocol chat)
            var ownedChats = 0;
            foreach (var chatView in Frontend.MainWindow.ChatViewManager.Chats) {
                if (chatView.ProtocolManager == ProtocolManager) {
                    ownedChats++;
                }
            }
            if (ownedChats > 1) {
                Gtk.MessageDialog md = new Gtk.MessageDialog(
                    Frontend.MainWindow,
                    Gtk.DialogFlags.Modal,
                    Gtk.MessageType.Warning,
                    Gtk.ButtonsType.YesNo,
                    _("Closing the protocol chat will also close all open chats connected to it!\n"+
                      "Are you sure you want to do this?"));
                int result = md.Run();
                md.Destroy();
                if ((Gtk.ResponseType) result != Gtk.ResponseType.Yes) {
                    return;
                }
            }

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    // no need to call base.Close() as CommandNetwork() will
                    // deal with it
                    Frontend.Session.CommandNetwork(
                        new CommandModel(
                            Frontend.FrontendManager,
                            ChatModel,
                            "close"
                        )
                    );
                } catch (Exception ex) {
                    Frontend.ShowException(ex);
                }
            });
        }

        public override void ApplyConfig(UserConfig config)
        {
            Trace.Call(config);

            if (config == null) {
                throw new ArgumentNullException("config");
            }

            base.ApplyConfig(config);

            ProxySettings.ApplyConfig(config);
        }

        void CheckIcon()
        {
            Trace.Call();

            var iconCache = new IconCache("server-icons");
            // REMOTING CALL
            var protocol = ProtocolManager.Protocol;

            string iconName = String.Format("{0}.ico", ID);
            string iconPath;
            if (iconCache.TryGetIcon(protocol, iconName, out iconPath)) {
                UpdateServerIcon(iconPath);
            }

            string websiteUrl = null;
            lock (NetworkWebsiteUrls) {
                if (!NetworkWebsiteUrls.TryGetValue(ID, out websiteUrl) &&
                    !NetworkWebsiteUrls.TryGetValue(protocol, out websiteUrl)) {
                    // unknown network and protocol, nothing to download
                    return;
                }

                // download in background so Sync() doesn't get slowed down
                WebProxy proxy = null;
                // ignore the proxy settings of remote engines
                if (Frontend.IsLocalEngine) {
                    proxy = ProxySettings.GetWebProxy(websiteUrl);
                    if (proxy == null) {
                        // HACK: WebClient will always use the system proxy if set to
                        // null so explicitely override this by setting an empty proxy
                        proxy = new WebProxy();
                    }
                }
                iconCache.Proxy = proxy;
                iconCache.BeginDownloadIcon(protocol, iconName, websiteUrl, UpdateServerIcon, null);
            }
        }

        void UpdateServerIcon(string iconPath)
        {
            Trace.Call(iconPath);

            ServerIconPixbuf = new Gdk.Pixbuf(iconPath, 16, 16);
            GLib.Idle.Add(delegate {
                TabImage.Pixbuf = ServerIconPixbuf;
                OnStatusChanged(EventArgs.Empty);
                return false;
            });
        }

        protected override void OnTabMenuShown(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            base.OnTabMenuShown(sender, e);

            TabMenu.Prepend(ReconnectItem);
            TabMenu.ShowAll();
        }

        protected virtual void OnTabMenuReconnectActivated(object sender, EventArgs e)
        {
            Trace.Call(sender, e);

            try {
                var pm = ProtocolManager;
                if (pm == null) {
                    return;
                }

                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        pm.Reconnect(Frontend.FrontendManager);
                    } catch (Exception ex) {
                        Frontend.ShowException(ex);
                    }
                });
            } catch (Exception ex) {
                Frontend.ShowException(ex);
            }
        }

        static bool ValidateCertificate(object sender,
                                        X509Certificate certificate,
                                        X509Chain chain,
                                        SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None) {
                return true;
            }

#if LOG4NET
            f_Logger.Warn(
                "ValidateCertificate(): Certificate error: " +
                sslPolicyErrors
            );
#endif
            return true;
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
