// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014 Mirco Bauer <meebey@meebey.net>
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
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;

namespace Smuxi.Engine
{
    [CLSCompliant(false)]
    public class SmuxiCredentialsAuthProvider : CredentialsAuthProvider
    {
        SessionManager SessionManager { get; set; }

        public SmuxiCredentialsAuthProvider(SessionManager sessionManager)
        {
            if (sessionManager == null) {
                throw new ArgumentNullException("sessionManager");
            }

            SessionManager = sessionManager;
        }

        public override bool TryAuthenticate(IServiceBase authService, string userName, string password)
        {
            var smuxiSession = SessionManager.GetSession(userName, password);
            if (smuxiSession == null) {
                return false;
            }

            var session = (WebSession) authService.GetSession();
            session.IsAuthenticated = true;
            session.SmuxiSession = smuxiSession;
            authService.SaveSession(session, SessionExpiry);

            var httpRequest = authService.RequestContext.Get<IHttpRequest>();
            var redirect = httpRequest.GetParam("redirect");
            if (!String.IsNullOrEmpty(redirect)) {
                // else ServiceStack throws NRE
                session.ReferrerUrl = redirect;
            }
            return true;
        }
    }
}
