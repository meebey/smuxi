/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2008, 2010-2011 Mirco Bauer <meebey@meebey.net>
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public abstract class ChatModel : PermanentRemoteObject, ITraceable
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private string               _ID;
        private string               _Name;
        private ChatType             _ChatType;
        private IProtocolManager     _ProtocolManager;
        //private List<MessageModel>   _Messages = new List<MessageModel>();
        private bool                 _IsEnabled = true;
        // TODO: make persistent
        private DateTime             _LastSeenHighlight;
        private string               _LogFile;
        // TODO: make persistent
        public  int                  Position { get; set; }
        public  IMessageBuffer       MessageBuffer { get; private set; }
        public  int                  MessagesSyncCount { get; set; }

        public string ID {
            get {
                return _ID;
            }
        }
        
        public string Name {
            get {
                return _Name;
            }
        }
        
        public ChatType ChatType {
            get {
                return _ChatType;
            }
        }
        
        public IProtocolManager ProtocolManager {
            get {
                return _ProtocolManager;
            }
        }

        [Obsolete("Use ChatModel.MessageBuffer instead.")]
        public IList<MessageModel> Messages {
            get {
                // during cloning, someone could modify it and break the enumerator
                lock (MessageBuffer) {
                    if (MessageBuffer.Count == 0) {
                        return new List<MessageModel>(0);
                    }
                    if (MessagesSyncCount <= 0) {
                        return new List<MessageModel>(MessageBuffer);
                    } else {
                        var offset = MessageBuffer.Count - MessagesSyncCount;
                        if (offset < 0) {
                            offset = 0;
                        }
                        return MessageBuffer.GetRange(offset, MessagesSyncCount);
                    }
                }
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

        public DateTime LastSeenHighlight {
            get {
                return _LastSeenHighlight;
            }
            set {
                _LastSeenHighlight = value;
            }
        }

        public string LogFile {
            get {
                if (_LogFile == null) {
                    _LogFile = GetLogFile();
                }
                return _LogFile;
            }
        }

        protected ChatModel(string id, string name, ChatType chatType, IProtocolManager networkManager)
        {
            _ID = id;
            _Name = name;
            _ChatType = chatType;
            _ProtocolManager = networkManager;
            Position = -1;
            if (ProtocolManager == null) {
                InitMessageBuffer(MessageBufferPersistencyType.Volatile);
            }
        }
        
        public string ToTraceString()
        {
            string nm = (_ProtocolManager != null) ? _ProtocolManager.ToString() : "(null)";  
            return  nm + "/" + _Name; 
        }

        public void ApplyConfig(UserConfig config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            MessagesSyncCount =
                (int) config["Interface/Notebook/EngineBufferLines"];

            var enumStr = (string) config["MessageBuffer/PersistencyType"];
            MessageBufferPersistencyType persistency;
            try {
                persistency = (MessageBufferPersistencyType) Enum.Parse(
                    typeof(MessageBufferPersistencyType), enumStr, true
                );
            } catch (ArgumentException ex) {
#if LOG4NET
                _Logger.Error("ApplyConfig(): failed to parse " +
                              "PersistencyType: " + enumStr, ex);
#endif
                persistency = MessageBufferPersistencyType.Volatile;
            }
            InitMessageBuffer(persistency);

            var maxCapacityKey = String.Format("MessageBuffer/{0}/MaxCapacity",
                                               persistency.ToString());
            MessageBuffer.MaxCapacity = (int) config[maxCapacityKey];
        }

        public void Close()
        {
            MessageBuffer.Dispose();
        }

        private string GetLogFile()
        {
            if (_ProtocolManager == null) {
                return null;
            }

            var logPath = Platform.LogPath;
            var protocol = _ProtocolManager.Protocol.ToLower();
            var network = _ProtocolManager.NetworkID.ToLower();
            logPath = Path.Combine(logPath, protocol);
            if (network != protocol) {
                logPath = Path.Combine(logPath, network);
            }
            if (logPath.IndexOfAny(Path.GetInvalidPathChars()) != -1) {
#if LOG4NET
                _Logger.Warn(
                    "GetLogFile(): logPath '" + logPath + "' contains " +
                     "invalid chars, removing them!"
                );
#endif
                // remove invalid chars
                foreach (char invalidChar in Path.GetInvalidPathChars()) {
                    logPath = logPath.Replace(invalidChar.ToString(),
                                              String.Empty);
                }
            }

            if (!Directory.Exists(logPath)) {
                Directory.CreateDirectory(logPath);
            }
            
            var chatId = ID.Replace(" ", "_").ToLower();
            if (chatId.IndexOfAny(Path.GetInvalidFileNameChars()) != -1) {
#if LOG4NET
                _Logger.Warn(
                    "GetLogFile(): chatId '" + logPath + "' contains " +
                     "invalid chars, removing them!"
                );
#endif
                // remove invalid chars
                foreach (char invalidChar in Path.GetInvalidFileNameChars()) {
                    chatId = chatId.Replace(invalidChar.ToString(),
                                            String.Empty);
                }
            }
            logPath = Path.Combine(logPath, String.Format("{0}.log", chatId));
            logPath = logPath.Replace("..", String.Empty);
            return logPath;
        }

        void InitMessageBuffer(MessageBufferPersistencyType persistency)
        {
            Trace.Call(persistency);

            if (MessageBuffer != null) {
                return;
            }

            switch (persistency) {
                case MessageBufferPersistencyType.Volatile:
                    MessageBuffer = new ListMessageBuffer();
                    break;
                case MessageBufferPersistencyType.Persistent:
                    try {
                        MessageBuffer = new Db4oMessageBuffer(
                            ProtocolManager.Session.Username,
                            ProtocolManager.Protocol,
                            ProtocolManager.NetworkID,
                            ID
                        );
                    } catch (Exception ex) {
#if LOG4NET
                        _Logger.Error(
                            "InitMessageBuffer(): Db4oMessageBuffer() threw " +
                            "exception, falling back to memory backend!", ex
                        );
#endif
                        MessageBuffer = new ListMessageBuffer();
                    }
                    break;
            }
        }
    }
}
