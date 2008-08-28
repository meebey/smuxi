using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Smuxi.Engine;

namespace Smuxi.Frontend.Wpf
{
	public class ChatView : TabControl, IChatView
	{
        #region IChatView Members

        public Smuxi.Engine.ChatModel ChatModel
        {
            get { throw new NotImplementedException(); }
        }

        public void Enable()
        {
            throw new NotImplementedException();
        }

        public void Disable()
        {
            throw new NotImplementedException();
        }

        public void ScrollUp()
        {
            throw new NotImplementedException();
        }

        public void ScrollDown()
        {
            throw new NotImplementedException();
        }

        public void ScrollToStart()
        {
            throw new NotImplementedException();
        }

        public void ScrollToEnd()
        {
            throw new NotImplementedException();
        }

        #endregion

        public void AddMessage(MessageModel msg)
        {
        }

        public void Sync()
        {
        }
    }
}
