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

namespace Meebey.Smuxi.Engine
{
    public enum UrlProtocol {
        Http,
        Https,
        Ftp,
        Ftps,
        Telnet,
    }
    
    // TODO: extend TextMessagePartModel instead?
    [Serializable]
    public class UrlMessagePartModel : MessagePartModel
    {
        private string      _Url;
        private UrlProtocol _Protocol;
        
        public string Url {
            get {
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
        
        public UrlMessagePartModel() :
                              base(MessagePartType.Url)
        {
        }
        
        public UrlMessagePartModel(string url, UrlProtocol protocol) :
                              base(MessagePartType.Url)
        {
            _Url = url;
            _Protocol = protocol;
        }
    }
}
