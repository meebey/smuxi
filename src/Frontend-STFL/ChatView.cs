/*
 * $Id: ChatView.cs 200 2007-06-25 01:12:33Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/ChatView.cs $
 * $Rev: 200 $
 * $Author: meebey $
 * $Date: 2007-06-25 03:12:33 +0200 (Mon, 25 Jun 2007) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007 Mirco Bauer <meebey@meebey.net>
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
using System.Globalization;
using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;
using STFL = Stfl;

namespace Smuxi.Frontend.Stfl
{
    [ChatViewInfo(ChatType = ChatType.Network)]
    [ChatViewInfo(ChatType = ChatType.Person)]
    [ChatViewInfo(ChatType = ChatType.Group)]
    public class ChatView : IChatView
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private ChatModel  _ChatModel;
        private MainWindow _MainWindow;
        
        public ChatModel ChatModel {
            get {
                return _ChatModel;
            }
        }
        
        public MainWindow MainWindow {
            get {
                return _MainWindow;
            }
            set {
                _MainWindow = value;
            }
        }
        
        public ChatView(ChatModel chat)
        {
            _ChatModel = chat;
        }
        
        public virtual void Enable()
        {
            Trace.Call();
        }
        
        public virtual void Disable()
        {
            Trace.Call();
        }
        
        public virtual void Sync()
        {
#if LOG4NET
            _Logger.Debug("Sync() syncing messages");
#endif
            // sync messages
            // cleanup, be sure the output is empty
            _MainWindow.Modify("output_textview", "replace_inner", "");
            
            IList<MessageModel> messages = _ChatModel.Messages;
            if (messages.Count > 0) {
                foreach (MessageModel msg in messages) {
                    AddMessage(msg);
                }
            }
        }
        
		public void AddMessage(MessageModel msg)
		{
            string finalMsg = String.Empty;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
                // TODO: implement other types
                if (msgPart is TextMessagePartModel) {
                    TextMessagePartModel fmsgti = (TextMessagePartModel) msgPart;
                    finalMsg += fmsgti.Text;
                } 
            }
            
            string timestamp;
            try {
                timestamp = msg.TimeStamp.ToLocalTime().ToString((string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"]);
            } catch (FormatException e) {
                timestamp = "Timestamp Format ERROR: " + e.Message;
            }
            finalMsg = timestamp + " " + _ChatModel.Name + " " + finalMsg;
            
            _MainWindow.Modify("output_textview", "append", "{listitem text:" + STFL.quote(finalMsg) + "}"); 
            
            //ScrollToEnd();
		}
		
		public void ScrollUp()
        {
            Trace.Call();
        }
        
		public void ScrollDown()
        {
            Trace.Call();
        }
        
		public void ScrollToStart()
        {
            Trace.Call();
        }
        
		public void ScrollToEnd()
        {
            Trace.Call();
            
            // let height refresh
            //_MainWindow.Run(-1);
            //_MainWindow.Modify("output_textview", "replace", "offset:-1");
            //_MainWindow["output_textview_offset"] = (_ChatModel.Messages.Count - 1).ToString();
        }     
    }
}
