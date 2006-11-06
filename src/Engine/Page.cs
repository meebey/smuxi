/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
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
using System.Collections;
using Meebey.Smuxi.Common;

namespace Meebey.Smuxi.Engine
{
    public class Page : PermanentRemoteObject, ITraceable
    {
        private string           _Name;
        private PageType         _PageType;
        private NetworkType      _NetworkType;
        private INetworkManager  _NetworkManager;
        private IList            _Buffer = new ArrayList();
        private bool             _IsEnabled = true;
        
        public string Name {
            get {
                return _Name;
            }
        }
        
        public PageType PageType {
            get {
                return _PageType;
            }
        }
        
        public NetworkType NetworkType {
            get {
                return _NetworkType;
            }
        }
        
        public INetworkManager NetworkManager {
            get {
                return _NetworkManager;
            }
        }
        
        public IList Buffer {
            get {
                return (IList) ((ICloneable)_Buffer).Clone();
            }
        }
        
        public IList UnsafeBuffer {
            get {
                return _Buffer;
            }
        }
        
        public virtual bool IsEnabled {
        	get {
        		return _IsEnabled;
        	}
        	internal set {
        	    _IsEnabled = value;
        	}
        }
        
        public Page(string name, PageType ptype, NetworkType ntype, INetworkManager nm)
        {
            _Name = name;
            _PageType = ptype;
            _NetworkType = ntype;
            _NetworkManager = nm;
        }
        
        public string ToTraceString()
        {
        	string nm = (_NetworkManager != null) ? _NetworkManager.ToString() : "null" ;  
        	return  nm + "/" + _Name; 
        }
    }
}
