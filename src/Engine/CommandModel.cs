/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
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
using System.Collections.Generic;
using System.Runtime.Serialization;
using Smuxi.Common;
using System.Text.RegularExpressions;

namespace Smuxi.Engine
{
    [Serializable]
    public class CommandModel : ITraceable, ISerializable
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private string          _Data;
        private string[]        _DataArray;
        private string          _Parameter;
        private bool            _IsCommand;
        private string          _CommandCharacter;
        private string          _Command;
        private FrontendManager _FrontendManager;
        private ChatModel       _Chat;
        
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
        
        public ChatModel Chat {
            get {
                return _Chat;
            }
        }
        
        public CommandModel(FrontendManager fm, ChatModel chat, string cmdChar, string data)
        {
            Trace.Call(fm, chat == null ? "(null)" : chat.GetType().ToString(), cmdChar, data);
            
            _Data = data;
            _CommandCharacter = cmdChar;
            _FrontendManager = fm;
            _Chat = chat;

            try {
                EnhancedParse(data);
            } catch (FormatException) {
                SimpleParse(data);
            }
        }
        
        public CommandModel(FrontendManager fm, ChatModel chat, string parameter) :
                       this(fm, chat, "/", "/cmd " + parameter)
        {
        }
        
        protected CommandModel(SerializationInfo info, StreamingContext ctx)
        {
            SerializationReader sr = SerializationReader.GetReader(info);
            SetObjectData(sr);

            _FrontendManager = (FrontendManager) info.GetValue("_FrontendManager", typeof(FrontendManager));
            _Chat            = (ChatModel) info.GetValue("_Chat", typeof(ChatModel));
        }
        
        protected virtual void SetObjectData(SerializationReader sr)
        {
            // FIXME: optimize this by re-parsing instead of deserializing
            _Data             = sr.ReadString();
            _DataArray        = _Data.Split(new char[] {' '});
            _Parameter        = sr.ReadString();
            _IsCommand        = sr.ReadBoolean();
            _CommandCharacter = sr.ReadString();
            _Command          = sr.ReadString();
            //_FrontendManager  = (FrontendManager) sr.ReadObject();
            //_Chat             = (ChatModel) sr.ReadObject();
        }
        
        protected virtual void GetObjectData(SerializationWriter sw)
        {
            sw.Write(_Data);
            //sw.Write(_DataArray);
            sw.Write(_Parameter);
            sw.Write(_IsCommand);
            sw.Write(_CommandCharacter);
            sw.Write(_Command);
            //sw.WriteObject(_FrontendManager);
            //sw.WriteObject(_Chat);
        }
        
        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctx) 
        {
            SerializationWriter sw = SerializationWriter.GetWriter(); 
            GetObjectData(sw);
            sw.AddToInfo(info);

            info.AddValue("_FrontendManager", _FrontendManager);
            info.AddValue("_Chat", _Chat);
        }
        
        public string ToTraceString()
        {
            return _Data;
        }

        void EnhancedParse(string data)
        {
            string regex = Regex.Escape(_CommandCharacter);
            regex += "(?<command>[a-z]+)"; // commands can only contain english keyboard letters
            string quoted_parameter = @"            ""(?<parameters>[^""]*)""";
            string normal_parameter = @"            (?<parameters>[^ ]+)";
            string parameters = @"            ( +(" + quoted_parameter + "|" + normal_parameter + "))*";
            regex += parameters + " *"; // may end with spaces
            regex = "^" + regex + "$"; // parse full string
            var match = Regex.Match(data, regex, RegexOptions.IgnoreCase);

            if (data.Contains(" ")) {
                _Parameter = data.Substring(data.IndexOf(' ') + 1);
            } else {
                _Parameter = "";
            }
            if (match.Success) {
                _IsCommand = true;
                _Command = match.Groups["command"].Value;
                var list = new List<string>();
                list.Add(_CommandCharacter + _Command);
                foreach (Capture cap in match.Groups["parameters"].Captures) {
                    list.Add(cap.Value);
                }
                _DataArray = list.ToArray();
            } else {
                if (data.StartsWith(_CommandCharacter + _CommandCharacter)) {
                    _Data = data.Substring(_CommandCharacter.Length);
                } else if (data.StartsWith(_CommandCharacter)) {
                    throw new FormatException("command could not be parsed by command regex, regex must be broken");
                }
                _DataArray = new string[1];
                _DataArray[0] = _Data;
            }
        }

        void SimpleParse(string data)
        {
            _DataArray = data.Split(new char[] {' '});
            _Parameter = String.Join(" ", _DataArray, 1, _DataArray.Length - 1);
            if (data.StartsWith(_CommandCharacter) &&
                !data.StartsWith(_CommandCharacter + _CommandCharacter)) {
                _Command = (_DataArray [0].Length > _CommandCharacter.Length) ?
                    _DataArray [0].Substring(_CommandCharacter.Length).ToLower() : String.Empty;
            } else if (data.StartsWith(_CommandCharacter + _CommandCharacter)) {
                _Data = data.Substring(_CommandCharacter.Length);
                _DataArray [0] = _DataArray [0].Substring(_CommandCharacter.Length);
            }
        }
    }
}
