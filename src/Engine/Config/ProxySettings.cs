// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2011 Mirco Bauer <meebey@meebey.net>
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
using System.Net;

namespace Smuxi.Engine
{
    public class ProxySettings
    {
        public ProxyType ProxyType { get; set; }
        public string ProxyHostname { get; set; }
        public int ProxyPort { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public WebProxy WebProxy { get; set; }

        public ProxySettings()
        {
            ProxyType = ProxyType.None;
        }

        public void ApplyConfig(UserConfig config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            var proxyType = (string) config["Connection/ProxyType"];
            ProxyType = (ProxyType) Enum.Parse(typeof(ProxyType), proxyType, true);
            ProxyHostname = (string) config["Connection/ProxyHostname"];
            ProxyPort = (int) config["Connection/ProxyPort"];
            ProxyUsername = (string) config["Connection/ProxyUsername"];
            ProxyPassword = (string) config["Connection/ProxyPassword"];

            switch (ProxyType) {
                case ProxyType.None:
                    WebProxy = null;
                    break;
                case ProxyType.System:
                    // TODO: add GNOME (gconf) and Windows (registry) support
                    var proxy = Environment.GetEnvironmentVariable("http_proxy");
                    if (!String.IsNullOrEmpty(proxy)) {
                        Uri systemProxy = null;
                        Uri.TryCreate(proxy, UriKind.Absolute, out systemProxy);
                        if (systemProxy != null && systemProxy.Scheme == "http") {
                            WebProxy = new WebProxy(systemProxy);
                        }
                    }
                    break;
                case ProxyType.Http:
                    var uriBuilder = new UriBuilder();
                    uriBuilder.Scheme = "http";
                    uriBuilder.Host = ProxyHostname;
                    uriBuilder.Port = ProxyPort;
                    uriBuilder.UserName = ProxyUsername;
                    uriBuilder.Password = ProxyPassword;
                    var proxyUri = uriBuilder.ToString();
                    WebProxy = new WebProxy(proxyUri);
                    break;
            }
        }
    }
}
