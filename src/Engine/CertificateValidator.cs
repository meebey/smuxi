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
using System.IO;
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

            string hostname = null;
            if (sender is HttpWebRequest) {
                var request = (HttpWebRequest) sender;
                hostname = request.RequestUri.Host;
            }

            var hash = certificate.GetCertHashString();
            var certInfo =  String.Format(
                "\n Subject: '{0}'" +
                "\n Issuer: '{1}'" +
                "\n Hash: '{2}'" +
                "\n Hostname: '{3}'",
                certificate.Subject, certificate.Issuer, hash, hostname
            );

            DateTime start, stop;
            start = DateTime.UtcNow;
            int err = 0;
            PolarSSL.X509Certificate polarCertChain = new PolarSSL.X509Certificate();
            foreach (var entry in chain.ChainElements) {
                var certBytes = entry.Certificate.GetRawCertData();
                err = PolarSSL.PolarSsl.x509parse_crt_der(
                    ref polarCertChain,
                    certBytes,
                    new UIntPtr((uint)certBytes.LongLength)
                );
                if (err != 0) {
                    // shit
                    return false;
                }
            }

            PolarSSL.X509Certificate polarTrustChain = new PolarSSL.X509Certificate();
            // load CA from disk!
            //err = PolarSSL.PolarSsl.x509parse_crtfile(ref polarTrustChain, "/etc/ssl/certs/Equifax_Secure_CA.pem");
            err = PolarSSL.PolarSsl.x509parse_crtfile(ref polarTrustChain, "/etc/ssl/certs/ca-certificates.crt");

            PolarSSL.X509CertificateRevocationList polarCrl = new PolarSSL.X509CertificateRevocationList();
            int verifyFlags = 0;
            err = PolarSSL.PolarSsl.x509parse_verify(
                ref polarCertChain, ref polarTrustChain, ref polarCrl, hostname,
                ref verifyFlags, null, IntPtr.Zero
            );
            stop = DateTime.UtcNow;
#if LOG4NET
            Logger.DebugFormat(
                "ValidateCertificate(): PolarSSL verify took: {0:0.00} ms ",
                (stop - start).TotalMilliseconds
            );
#endif
            if (err == 0) {
#if LOG4NET
                Logger.DebugFormat(
                    "ValidateCertificate(): Validated certificate " +
                    "via PolarSSL: {0}", certInfo
                );
#endif
                return true;
            }

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
