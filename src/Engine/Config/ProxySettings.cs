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
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Smuxi.Engine
{
    public class ProxySettings
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        public ProxyType ProxyType { get; set; }
        public string ProxyHostname { get; set; }
        public int ProxyPort { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public IWebProxy SystemWebProxy { get; set; }
        public WebProxy DefaultWebProxy { get; set; }

        static ProxySettings()
        {
            try {
                WorkaroundNoProxyMonoBug();
            } catch {
            }
        }

        public ProxySettings()
        {
            ProxyType = ProxyType.None;
        }

        public WebProxy GetWebProxy(Uri destination)
        {
            if (destination == null) {
                throw new ArgumentNullException("destination");
            }

            if (SystemWebProxy == null &&
                DefaultWebProxy == null) {
#if LOG4NET
                f_Logger.DebugFormat("GetWebProxy(<{0}>): returning no proxy",
                                     destination);
#endif
                // no proxy
                return null;
            }

            if (SystemWebProxy == null) {
                if (DefaultWebProxy.Address.Scheme.StartsWith("socks") &&
                    destination.Scheme.StartsWith("http")) {
#if LOG4NET
                    f_Logger.DebugFormat("GetWebProxy(<{0}>): ignoring " +
                                         "SOCKS proxy for HTTP destination: {1}",
                                         destination, DefaultWebProxy.Address);
#endif
                    return null;
                }

#if LOG4NET
                f_Logger.DebugFormat("GetWebProxy(<{0}>): returning default proxy: {1}",
                                     destination, DefaultWebProxy.Address);
#endif
                return DefaultWebProxy;
            }
            var proxyUri = SystemWebProxy.GetProxy(destination);
            if (proxyUri == destination) {
#if LOG4NET
                f_Logger.DebugFormat("GetWebProxy(<{0}>): returning no proxy",
                                     destination);
#endif
                // no proxy
                return null;
            }
#if LOG4NET
            f_Logger.DebugFormat("GetWebProxy(<{0}>): returning system proxy: {1}",
                                 destination, proxyUri);
#endif
            return new WebProxy(proxyUri);
        }

        public WebProxy GetWebProxy(string destination)
        {
            if (destination == null) {
                throw new ArgumentNullException("destination");
            }
            return GetWebProxy(new Uri(destination));
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
                    DefaultWebProxy = null;
                    SystemWebProxy = null;
                    break;
                case ProxyType.System:
                    // TODO: add GNOME (gconf) support
                    var no_proxy = Environment.GetEnvironmentVariable("no_proxy");
                    var proxy = WebRequest.GetSystemWebProxy();
                    if (!String.IsNullOrEmpty(no_proxy) && proxy is WebProxy) {
                        var webProxy = (WebProxy) proxy;
                        // BypassArrayList expects regexes while no_proxy
                        // contains domains
                        var bypassUriRegexes = new List<string>();
                        foreach (var domain in no_proxy.Split(',')) {
                            string domainRegex = null;
                            if (domain.StartsWith(".")) {
                                domainRegex = String.Format(
                                    @"^[a-z]+://(.+\.)?{0}",
                                    Regex.Escape(domain.Substring(1))
                                );
                            } else if (!Regex.IsMatch(domain, @"^[a-z]+://")) {
                                domainRegex = String.Format(
                                    @"^[a-z]+://{0}",
                                    Regex.Escape(domain)
                                );
                            } else {
                                domainRegex = Regex.Escape(domain);
                            }
                            bypassUriRegexes.Add(domainRegex);
                        }
                        webProxy.BypassArrayList.AddRange(bypassUriRegexes);
                    }
                    DefaultWebProxy = null;
                    SystemWebProxy = proxy;
                    break;
                default:
                    var uriBuilder = new UriBuilder();
                    uriBuilder.Scheme = ProxyType.ToString().ToLower();
                    uriBuilder.Host = ProxyHostname;
                    uriBuilder.Port = ProxyPort;
                    uriBuilder.UserName = ProxyUsername;
                    uriBuilder.Password = ProxyPassword;
                    var proxyUri = uriBuilder.ToString();
                    DefaultWebProxy = new WebProxy(proxyUri);
                    SystemWebProxy = null;
                    break;
            }
        }

        static void WorkaroundNoProxyMonoBug()
        {
            // HACK: workaround bug in Mono 2.10.8 throwing
            // ArgumentOutOfRangeException because it always tries to remove
            // *.local from the no_proxy envrionment variable, see:
            // https://www.smuxi.org/issues/show/873
            var no_proxy = Environment.GetEnvironmentVariable("no_proxy");
            if (no_proxy == null) {
                // nothing to workaround
                return;
            }

            try {
                WebRequest.GetSystemWebProxy();
            } catch (ArgumentOutOfRangeException) {
#if LOG4NET
                f_Logger.Debug("WorkaroundNoProxyMonoBug(): enabling no_proxy workaround...");
#endif
                if (!no_proxy.Contains("*.local")) {
                    var no_proxy_with_local = no_proxy + ",*.local";
                    Environment.SetEnvironmentVariable("no_proxy",
                                                       no_proxy_with_local);
                }
            }
        }
    }
}
