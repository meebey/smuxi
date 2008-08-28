using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Smuxi.Engine;

namespace Smuxi.Frontend.Wpf
{
	public class ChatViewManager : ChatViewManagerBase
	{
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private TabControl _Notebook;

        public ChatViewManager(TabControl notebook)
        {
            _Notebook = notebook;
        }

        public override IChatView ActiveChat
        {
            get { throw new NotImplementedException(); }
        }

        public override void AddChat(Smuxi.Engine.ChatModel chat)
        {
            throw new NotImplementedException();
        }

        public override void RemoveChat(Smuxi.Engine.ChatModel chat)
        {
            throw new NotImplementedException();
        }

        public override void EnableChat(Smuxi.Engine.ChatModel chat)
        {
            throw new NotImplementedException();
        }

        public override void DisableChat(Smuxi.Engine.ChatModel chat)
        {
            throw new NotImplementedException();
        }
        public ChatView GetChat(ChatModel epage)
        {
            return null;
        }
    }
}
