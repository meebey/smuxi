/**
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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
    public class CommandData
    {
        private string          _Data;
        private string[]        _DataArray;
        private string          _Parameter;
        private bool            _IsCommand;
        private string          _CommandCharacter;
        private string          _Command;
        private FrontendManager _FrontendManager;
        
        public string Data {
            get {
                return _Data;
            }
        }
        
        public string[] DataArray {
            get {
                return _DataArray;
            }
        }
        
        public string Parameter {
            get {
                return _Parameter;
            }
        }
        
        public bool IsCommand {
            get {
                return _IsCommand;
            }
        }
        
        public string CommandCharacter {
            get {
                return _CommandCharacter;
            }
        }
        
        public string Command {
            get {
                return _Command;
            }
        }
        
        public FrontendManager FrontendManager {
            get {
                return _FrontendManager;
            }
        }
        
        public CommandData(FrontendManager fm, string cmdChar, string data)
        {
            _Data = data;
            _DataArray = data.Split(new char[] {' '});
            _Parameter = String.Join(" ", _DataArray, 1, _DataArray.Length-1);
            _IsCommand = (data[0] == cmdChar[0]);
            _CommandCharacter = cmdChar;
            if (_IsCommand) {
                _Command = (_DataArray[0].Length > 1) ? _DataArray[0].Substring(1).ToLower() : "";
            }
            _FrontendManager = fm;
        }
        
        public CommandData(FrontendManager fm, string parameter) : this(fm, "/", "/cmd "+parameter)
        {
        }
    }
}
