// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class CertificateValidator
    {
#if LOG4NET
        static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        public List<string> HostnameWhitelist { get; private set; }
        public List<string> HashWhitelist { get; private set; }

        public CertificateValidator()
        {
            HostnameWhitelist = new List<string>();
            HashWhitelist = new List<string>();

#if LOG4NET
            if (ServicePointManager.ServerCertificateValidationCallback != null) {
                Logger.Warn(
                    "CertificateValidator.ctor(): overwriting existing " +
                    "ServicePointManager.ServerCertificateValidationCallback"
                );
            }
#endif
            ServicePointManager.ServerCertificateValidationCallback = ValidateCertificate;
        }

        bool ValidateCertificate(object sender,
                                 X509Certificate certificate,
                                 X509Chain chain,
                                 SslPolicyErrors sslPolicyErrors)
        {
            Trace.Call(sender, "(X509Certificate)", chain, sslPolicyErrors);

            if (sslPolicyErrors == SslPolicyErrors.None) {
                return true;
            }

            var hash = certificate.GetCertHashString();
            string hostname = null;
            if (sender is HttpWebRequest) {
                var request = (HttpWebRequest) sender;
                hostname = request.RequestUri.Host;
            }
            var certInfo =  String.Format(
                "\n Subject: '{0}'" +
                "\n Issuer: '{1}'" +
                "\n Hash: '{2}'" +
                "\n Hostname: '{3}'",
                certificate.Subject, certificate.Issuer, hash, hostname
            );

            lock (HashWhitelist) {
                if (HashWhitelist.Contains(hash)) {
#if LOG4NET
                    Logger.DebugFormat(
                        "ValidateCertificate(): Validated certificate " +
                        "via hash whitelist: {0}", certInfo
                    );
#endif
                    return true;
                }
            }

            if (hostname != null) {
                lock (HostnameWhitelist) {
                    if (HostnameWhitelist.Contains(hostname)) {
#if LOG4NET
                        Logger.DebugFormat(
                            "ValidateCertificate(): Validated certificate " +
                            "via hostname whitelist: {0}", certInfo
                        );
#endif
                        return true;
                    }
                }
            }

#if LOG4NET
            Logger.ErrorFormat(
                "ValidateCertificate(): Validation failed: {0}" +
                "\n SslPolicyErrors: {1}",
                certInfo, sslPolicyErrors
            );
#endif
            return false;
        }
    }
}
