// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2015 Carlos Martín Nieto
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
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class IconCache
    {
        #if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endif

        ProxySettings ProxySettings { get; set; }

        readonly string cachePath = Platform.CachePath;
        readonly string iconsPath;

        /// <summary>
        /// Try to get a cached icon
        /// </summary>
        /// <returns><c>true</c>, if get icon is cached, <c>false</c> otherwise.</returns>
        /// <param name="protocol">The protocol of the channel or server</param>
        /// <param name="iconName">Name of the icon, including file extension</param>
        /// <param name="value">Path to the cached icon, if cached</param>
        public bool TryGetIcon(string protocol, string iconName, out string value)
        {
            var iconPath = Path.Combine(iconsPath, protocol, iconName);
            var iconFile = new FileInfo(iconPath);
            if (iconFile.Exists && iconFile.Length > 0) {
                value = iconPath;
                return true;
            }

            value = null;
            return false;
        }

        void ensureDirectoryExists(string path)
        {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Download an icon into the cache.
        ///
        /// The download will happen in the background and onSuccess will be called
        /// when done. If there is an error during the download, an error will be
        /// logged and onSuccess will not be called.
        ///
        /// The icon will
        /// </summary>
        /// <param name="protocol">The protocol of the channel or server</param>
        /// <param name="iconName">Name of the icon, including extension</param>
        /// <param name="websiteUrl">The url from which to download the icon</param>
        /// <param name="onSuccess">Function to call after downloading the icon</param>
        public void DownloadIcon(string protocol, string iconName, string websiteUrl, Action<string> onSuccess)
        {
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    var protocolPath = Path.Combine(iconsPath, protocol);
                    ensureDirectoryExists(protocolPath);
                    var iconPath = Path.Combine(protocolPath, iconName);
                    var iconFile = new FileInfo(iconPath);
                    DownloadServerIcon(websiteUrl, iconFile);
                    iconFile.Refresh();
                    if (!iconFile.Exists || iconFile.Length == 0) {
                        return;
                    }

                    onSuccess(iconPath);
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error("DownloadIcon(): Exception", ex);
#endif
                }
            });
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
                webClient.Proxy = proxy;
            }
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
            // ignore proxy settings of remote engines
            if (Frontend.IsLocalEngine) {
                iconRequest.Proxy = proxy;
            }
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

        /// <summary>
        /// Initializes a new instance of the <see cref="Smuxi.Frontend.Gnome.IconCache"/> class.
        /// </summary>
        /// <param name="kind">The kind of icons to cache (e.g. "server-icons", "emoji")</param>
        public IconCache(string kind)
        {
            ProxySettings = new ProxySettings();

            iconsPath = Path.Combine(cachePath, kind);
            ensureDirectoryExists(iconsPath);
        }
    }
}

