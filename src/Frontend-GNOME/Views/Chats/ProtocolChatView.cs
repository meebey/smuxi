/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006, 2009-2011 Mirco Bauer <meebey@meebey.net>
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
using System.Net;
using System.Net.Security;
using System.Web;
using System.Linq;
using System.Text.RegularExpressions;
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

            // Twitter
            NetworkWebsiteUrls.Add("Twitter", "http://www.twitter.com/");

            // XMPP
            NetworkWebsiteUrls.Add("talk.google.com", "http://www.google.com/talk/");
            NetworkWebsiteUrls.Add("chat.facebook.com", "http://www.facebook.com/");
        }

        public ProtocolChatView(ChatModel chat) : base(chat)
        {
            Trace.Call(chat);
            
            ProxySettings = new ProxySettings();

            Add(OutputScrolledWindow);
            ShowAll();
        }

        public override void Sync()
        {
            Trace.Call();

            base.Sync();

            Host = ProtocolManager.Host;
            Port = ProtocolManager.Port;

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

            var cachePath = Platform.CachePath;
            var iconPath = SysPath.Combine(cachePath, "server-icons");
            // REMOTING CALL
            var protocol = ProtocolManager.Protocol;
            iconPath = SysPath.Combine(iconPath, protocol);
            if (!Directory.Exists(iconPath)) {
                Directory.CreateDirectory(iconPath);
            }
            iconPath = SysPath.Combine(iconPath,
                                       String.Format("{0}.ico", ID));
            var iconFile = new FileInfo(iconPath);
            if (iconFile.Exists && iconFile.Length > 0) {
                // cached icon, use right away
                UpdateServerIcon(iconPath);
            }

            string websiteUrl = null;
            lock (NetworkWebsiteUrls) {
                if (!NetworkWebsiteUrls.TryGetValue(ID, out websiteUrl)) {
                    // unknown network, nothing to download
                    return;
                }
                // download in background so Sync() doesn't get slowed down
                ThreadPool.QueueUserWorkItem(delegate {
                    try {
                        // HACK: work around Mono's buggy certificate validation
                        ServicePointManager.ServerCertificateValidationCallback += ValidateCertificate;
                        DownloadServerIcon(websiteUrl, iconFile);
                        iconFile.Refresh();
                        if (!iconFile.Exists || iconFile.Length == 0) {
                            return;
                        }
                        UpdateServerIcon(iconPath);
                    } catch (Exception ex) {
#if LOG4NET
                        f_Logger.Error("CheckIcon(): Exception", ex);
#endif
                    } finally {
                        ServicePointManager.ServerCertificateValidationCallback -= ValidateCertificate;
                    }
                });
            }
        }

        void DownloadServerIcon(string websiteUrl, FileInfo iconFile)
        {
            Trace.Call(websiteUrl, iconFile);

            var webClient = new WebClient();
            // ignore proxy settings of remote engines
            WebProxy proxy = null;
            if (Frontend.IsLocalEngine) {
                proxy = ProxySettings.GetWebProxy(websiteUrl);
                if (proxy == null) {
                    // HACK: WebClient will always use the system proxy if set to
                    // null so explicitely override this by setting an empty proxy
                    proxy = new WebProxy();
                }
            }
            webClient.Proxy = proxy;
            var content = webClient.DownloadString(websiteUrl);
            var links = new List<Dictionary<string, string>>();
            foreach (Match linkMatch in Regex.Matches(content, @"<link[\s]+([^>]*?)/?>")) {
                var attributes = new Dictionary<string, string>();
                foreach (Match attrMatch in Regex.Matches(linkMatch.Value, @"([\w]+)[\s]*=[\s]*[""']([^""']*)[""'][\s]*")) {
                    var key = attrMatch.Groups[1].Value;
                    var value = attrMatch.Groups[2].Value;
                    attributes.Add(key, value);
                }
                links.Add(attributes);
            }
            string faviconRel = null;
            foreach (var link in links) {
                var iconLink = false;
                foreach (var attribute in link) {
                    if (attribute.Key != "rel" ||
                        !attribute.Value.Split(' ').Contains("icon")) {
                        continue;
                    }
                    iconLink = true;
                    break;
                }
                if (!iconLink) {
                    continue;
                }
                foreach (var attribute in link) {
                    if (attribute.Key != "href") {
                        continue;
                    }
                    // yay, we have found the favicon in all this junk
                    faviconRel = attribute.Value;
                    break;
                }
            }
            string faviconUrl = null;
            if (String.IsNullOrEmpty(faviconRel)) {
                faviconRel = "/favicon.ico";
            }
            faviconUrl = new Uri(new Uri(websiteUrl), faviconRel).ToString();
#if LOG4NET
            f_Logger.DebugFormat("DownloadServerIcon(): favicon URL: {0}",
                                 faviconUrl);
#endif

            var iconRequest = WebRequest.Create(faviconUrl);
            iconRequest.Proxy = proxy;
            if (iconRequest is HttpWebRequest) {
                var iconHttpRequest = (HttpWebRequest) iconRequest;
                if (iconFile.Exists) {
                    iconHttpRequest.IfModifiedSince = iconFile.LastWriteTime;
                }
            }

            WebResponse iconResponse;
            try {
                iconResponse = iconRequest.GetResponse();
            } catch (WebException ex) {
                if (ex.Response is HttpWebResponse) {
                    var iconHttpResponse = (HttpWebResponse) ex.Response;
                    if (iconHttpResponse.StatusCode == HttpStatusCode.NotModified) {
                        // icon hasn't changed, nothing to do
                        return;
                    }
                }
                throw;
            }

            // save new or modified icon file
            using (var iconStream = iconFile.OpenWrite())
            using (var httpStream = iconResponse.GetResponseStream()) {
                byte[] buffer = new byte[4096];
                int read;
                while ((read = httpStream.Read(buffer, 0, buffer.Length)) > 0) {
                    iconStream.Write(buffer, 0, read);
                }
            }
        }

        void UpdateServerIcon(string iconPath)
        {
            Trace.Call(iconPath);

            ServerIconPixbuf = new Gdk.Pixbuf(iconPath, 16, 16);
            GLib.Idle.Add(delegate {
                TabImage.Pixbuf = ServerIconPixbuf;
                return false;
            });
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
