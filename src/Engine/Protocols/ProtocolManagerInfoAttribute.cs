/*
 * $Id: CommandModel.cs 179 2007-04-21 15:01:29Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/CommandModel.cs $
 * $Rev: 179 $
 * $Author: meebey $
 * $Date: 2007-04-21 17:01:29 +0200 (Sat, 21 Apr 2007) $
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
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class ProtocolManagerInfoAttribute : Attribute
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif

        private string _Name;
        private string _Description;
        private string _Alias;
        
        public string Name {
            get {
                return _Name;
            }
            set {
                _Name = value;
            }
        }
        
        public string Description {
            get {
                return _Description;
            }
            set {
                _Description = value;
            }
        }
        
        public string Alias {
            get {
                return _Alias;
            }
            set {
                _Alias = value;
            }
        }
        
        public ProtocolManagerInfoAttribute()
        {
        }
        
        public ProtocolManagerInfoAttribute(string name, string description, string alias)
        {
            _Name = name;
            _Description = description;
            _Alias = alias;
        }
    }
}
