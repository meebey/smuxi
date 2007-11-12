/*
 * $Id: Config.cs 100 2005-08-07 14:54:22Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/Config.cs $
 * $Rev: 100 $
 * $Author: meebey $
 * $Date: 2005-08-07 16:54:22 +0200 (Sun, 07 Aug 2005) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public enum UrlProtocol {
        Unknown,
        Irc,
        Http,
        Https,
        Ftp,
        Ftps,
        Telnet,
    }
    
    [Serializable]
    public class UrlMessagePartModel : TextMessagePartModel
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private UrlProtocol _Protocol;
        
        public string Url {
            get {
                return Text;
            }
            set {
                Text = value;
            }
        }
        
        public UrlProtocol Protocol {
            get {
                return _Protocol;
            }
            set {
                _Protocol = value;
            }
        }
        
        public UrlMessagePartModel(string url) :
                              base(url)
        {
            _Protocol = ParseProtocol(url);
        }
        
        public UrlMessagePartModel(string url, UrlProtocol protocol) :
                              base(url)
        {
            _Protocol = protocol;
        }
        
        protected UrlMessagePartModel(SerializationInfo info, StreamingContext ctx) :
                                 base(info, ctx)
        {
        }
        
        protected override void SetObjectData(SerializationReader sr)
        {
            base.SetObjectData(sr);
            
            _Protocol = (UrlProtocol) sr.ReadInt32();
        }
        
        protected override void GetObjectData(SerializationWriter sw)
        {
            base.GetObjectData(sw);

            sw.Write((Int32) _Protocol);
        }

        protected static UrlProtocol ParseProtocol(string url)
        {
            Match match = Regex.Match(url, @"^([a-zA-Z0-9\-]+):\/\/");
            if (!match.Success) {
#if LOG4NET
                _Logger.Error("ParseProtocol(url): could not parse (via regex) protocol in URL: " + url);
#endif
                return UrlProtocol.Unknown;
            }
            
            string protocol = match.Groups[1].Value;
            try {
                return (UrlProtocol) Enum.Parse(typeof(UrlProtocol), protocol, true);
            } catch (ArgumentException ex) {
#if LOG4NET
                _Logger.Error("ParseProtocol(url): error parsing protocol: " + protocol, ex);
#endif
            }
            return UrlProtocol.Unknown;
        }
    }
}
