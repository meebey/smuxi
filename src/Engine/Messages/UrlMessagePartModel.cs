/*
 * $Id: Config.cs 100 2005-08-07 14:54:22Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/Config.cs $
 * $Rev: 100 $
 * $Author: meebey $
 * $Date: 2005-08-07 16:54:22 +0200 (Sun, 07 Aug 2005) $
 *
 * Smuxi - Smart MUltipleXed Irc
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
        None,
        Unknown,
        Irc,
        Http,
        Https,
        Ftp,
        Ftps,
        Telnet,
        MailTo
    }
    
    [Serializable]
    public class UrlMessagePartModel : TextMessagePartModel
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private UrlProtocol _Protocol;
        private string      _Url;
        
        public string Url {
            get {
                if (_Url == null) {
                    return Text;
                }
                return _Url;
            }
            set {
                _Url = value;
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
                              this(url, null)
        {
        }
        
        public UrlMessagePartModel(TextMessagePartModel model)
            : base(model)
        {
            if (model is UrlMessagePartModel) {
                _Protocol = (model as UrlMessagePartModel)._Protocol;
                _Url = (model as UrlMessagePartModel)._Url;
            }
        }

        public UrlMessagePartModel(string url, string text):
                              this(url, text, UrlProtocol.None)

        {
            _Protocol = ParseProtocol(url);
            if (_Protocol == UrlProtocol.None) {
                // assume http if no protocol was specified
                _Protocol = UrlProtocol.Http;
                // text should stay pristine
                if (Text == null) {
                    Text = _Url;
                }
                _Url = String.Format("http://{0}", _Url);
            }
        }
        
        public UrlMessagePartModel(string url, string text, UrlProtocol protocol) :
                              base(text)
        {
            _Url = url;
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
            _Url = sr.ReadString();
        }
        
        protected override void GetObjectData(SerializationWriter sw)
        {
            base.GetObjectData(sw);

            sw.Write((Int32) _Protocol);
            sw.Write(_Url);
        }

        protected static UrlProtocol ParseProtocol(string url)
        {
            Match match = Regex.Match(url, @"^([a-zA-Z0-9\-]+):");
            if (!match.Success) {
                return UrlProtocol.None;
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

        public override string ToString()
        {
            if (Text == null) {
                return _Url;
            } else if (_Url == null) {
                return Text;
            } else if (Text == _Url) {
                return _Url;
            } else if (Text.Contains(_Url)) {
                return Text;
            } else {
                return "[" + _Url + " " + Text + "]";
            }
        }
    }
}
