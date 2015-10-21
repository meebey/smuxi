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
using System.Reflection;
using Funq;
using ServiceStack;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;

namespace Smuxi.Engine
{
    [CLSCompliant(false)]
    public class WebServiceHost : AppHostHttpListenerBase
    {
        SessionManager SessionManager { get; set; }

        public WebServiceHost(SessionManager sessionManager)
        {
            if (sessionManager == null) {
                throw new ArgumentNullException("sessionManager");
            }

            SessionManager = sessionManager;

            EndpointHost.ConfigureHost(
                this,
                "Smuxi REST web service",
                CreateServiceManager(Assembly.GetExecutingAssembly())
            );
            EndpointHostConfig.Instance.ServiceStackHandlerFactoryPath = null;
            EndpointHostConfig.Instance.MetadataRedirectPath = "metadata";
        }

        public override void Configure(Container container)
        {
            // in memory session cache?
            container.Register<ICacheClient>(new MemoryCacheClient());

            // AuthFeature already activates SessionFeature
            //Plugins.Add(new SessionFeature());

            // TODO: implement UserAuthRepository

            Plugins.Add(
                new AuthFeature(
                    () => new WebSession(),
                    new IAuthProvider[] {
                        new SmuxiBasicAuthProvider(SessionManager),
                        new SmuxiCredentialsAuthProvider(SessionManager)
                    }
                )
            );
            // debug info
            Plugins.Add(new RequestInfoFeature());

            // enable basic CORS support, see:
            // https://stackoverflow.com/questions/8211930/servicestack-rest-api-and-cors
            /*
            Plugins.Add(new CorsFeature());
            PreRequestFilters.Add((httpReq, httpRes) => {
                // handle request and close responses after emitting global
                // HTTP headers as provided by the CorsFeature plugin
                if (httpReq.HttpMethod == "OPTIONS") {
                    httpRes.EndRequest();
                }
            });
            */

            // enable enhanced CORS support, as we don't have a static vhost
            // thus we simply return Origin from the HTTP request
            RequestFilters.Add((httpReq, httpRes, requestDto) => {
                // Implementation as specified here:
                // http://www.html5rocks.com/static/images/cors_server_flowchart.png
                // TODO: should we send Access-Control-Expose-Headers?
                if (!String.IsNullOrEmpty(httpReq.Headers["Origin"])) {
                    // CORS simple request
                    httpRes.AddHeader("Access-Control-Allow-Origin", httpReq.Headers["Origin"]);
                    httpRes.AddHeader("Access-Control-Allow-Credentials", "true");
                    if (httpReq.HttpMethod == "OPTIONS" &&
                        !String.IsNullOrEmpty(httpReq.Headers["Access-Control-Request-Method"])) {
                        // CORS preflight request
                        if (requestDto is Auth) {
                            // /auth/{provider} only supports POST, DELETE and OPTIONS
                            // GET redirects to login (HTML) page
                            httpRes.AddHeader("Access-Control-Allow-Methods", "POST, DELETE, OPTIONS");
                        } else {
                            httpRes.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                        }
                        httpRes.AddHeader("Access-Control-Allow-Headers", "Accept, Content-Type");
                        httpRes.End();
                    }
                }
            });
        }
    }
}
