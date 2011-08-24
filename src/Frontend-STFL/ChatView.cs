/*
 * $Id: ChatView.cs 200 2007-06-25 01:12:33Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/ChatView.cs $
 * $Rev: 200 $
 * $Author: meebey $
 * $Date: 2007-06-25 03:12:33 +0200 (Mon, 25 Jun 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
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
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using Smuxi.Common;
using Smuxi.Engine;
using Smuxi.Frontend;
using Stfl;

namespace Smuxi.Frontend.Stfl
{
    [ChatViewInfo(ChatType = ChatType.Session)]
    [ChatViewInfo(ChatType = ChatType.Protocol)]
    [ChatViewInfo(ChatType = ChatType.Person)]
    [ChatViewInfo(ChatType = ChatType.Group)]
    public class ChatView : IChatView, IDisposable
    {
#if LOG4NET
        static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        // HACK: STFL crashes if we use 0 in a widget name
        static int f_NextID = 1;
        int        f_WidgetID;
        string     f_WidgetName;
        ChatModel  f_ChatModel;
        MainWindow f_MainWindow;
        TextView   MessageTextView { get; set; }

        public ChatModel ChatModel {
            get {
                return f_ChatModel;
            }
        }

        public string ID {
            get {
                return ChatModel.ID;
            }
        }

        public int Position {
            get {
                return ChatModel.Position;
            }
        }

        public bool IsVisible {
            get {
                return f_MainWindow[f_WidgetID + "d"] == "1";
            }
            set {
                f_MainWindow[f_WidgetID + "d"] = value ?  "1" : "0";
           }
        }

        public string WidgetName {
            get {
                return f_WidgetName;
            }
        }

        public ChatView(ChatModel chat, MainWindow window)
        {
            Trace.Call(chat, window);

            if (chat == null) {
                throw new ArgumentNullException("chat");
            }
            if (window == null) {
                throw new ArgumentNullException("window");
            }

            f_ChatModel = chat;
            f_MainWindow = window;
            f_WidgetID = f_NextID++;
            f_WidgetName = "output_textview_" + f_WidgetID;

            f_MainWindow.Modify("output_vbox", "append",
                "{" +
                    "textview[" + f_WidgetName + "] " +
                        ".expand:vh " +
                        ".display[" + f_WidgetID + "d]:0 " +
                        "offset[" + f_WidgetID + "os]:0 " +
                        "richtext:1 " +
                        "style_red_normal:fg=red " +
                        "style_url_normal:attr=underline " +
                        "style_u_normal:attr=underline " +
                        "style_b_normal:attr=bold " +
                        "style_i_normal:attr=standout " +
                "}"
            );
            MessageTextView = new TextView(f_MainWindow, f_WidgetName);
            MessageTextView.OffsetVariableName = f_WidgetID + "os";
            // HACK: as the chat is not always visible we can't extract the
            // heigth and width information from the textview because it simply
            // returns 0 when invisible, thus we need to abuse output_vbox
            MessageTextView.HeigthVariableName = "output_vbox:h";
            MessageTextView.WidthVariableName = "output_vbox:w";
            MessageTextView.AutoLineWrap = true;
        }
        
        ~ChatView()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            f_MainWindow.Modify(f_WidgetName, "delete", null);
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
            f_MainWindow.Modify("output_textview", "replace_inner", "");
            
            IList<MessageModel> messages = f_ChatModel.Messages;
            if (messages.Count > 0) {
                foreach (MessageModel msg in messages) {
                    AddMessage(msg);
                }
            }
        }
        
        public virtual void Populate()
        {
        }

        public void AddMessage(MessageModel msg)
        {
            // OPT: typical message length
            var line = new StringBuilder(512);
            int msgLength = 0;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
                // TODO: implement other types
                if (msgPart is UrlMessagePartModel) {
                    var urlPart = (UrlMessagePartModel) msgPart;
                    var escapedUrl = StflApi.EscapeRichText(urlPart.Url);
                    line.Append(String.Format("<url>{0}</url>", escapedUrl));
                    msgLength += urlPart.Url.Length;
                } else if (msgPart is TextMessagePartModel) {
                    var txtPart = (TextMessagePartModel) msgPart;
                    if (String.IsNullOrEmpty(txtPart.Text)) {
                        continue;
                    }

                    var tags = new List<string>();
                    if (txtPart.ForegroundColor != TextColor.None) {
                        // TODO: implement color mapping, see:
                        // http://www.calmar.ws/vim/256-xterm-24bit-rgb-color-chart.html
                        //tags.Add("red");
                    }
                    if (txtPart.Underline) {
                        tags.Add("u");
                    }
                    if (txtPart.Bold) {
                        tags.Add("b");
                    }
                    if (txtPart.Italic) {
                        tags.Add("i");
                    }

                    string escapedText = StflApi.EscapeRichText(txtPart.Text);
                    if (tags.Count > 0) {
                        tags.Reverse();
                        string markup = escapedText;
                        foreach (string tag in tags) {
                            markup = String.Format("<{0}>{1}</{2}>",
                                                   tag, markup, tag);
                        }
                        line.Append(markup);
                    } else {
                        line.Append(escapedText);
                    }
                    msgLength += txtPart.Text.Length;
                }
            }

            string timestamp;
            try {
                timestamp = msg.TimeStamp.ToLocalTime().ToString((string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"]);
            } catch (FormatException e) {
                timestamp = "Timestamp Format ERROR: " + e.Message;
            }
            var finalMsg = String.Format("{0} {1}", timestamp, line.ToString());
            MessageTextView.AppendLine(finalMsg);

            ScrollToEnd();
        }

        public void ScrollUp()
        {
            Trace.Call();

            MessageTextView.ScrollUp();
        }

        public void ScrollDown()
        {
            Trace.Call();

            MessageTextView.ScrollDown();
        }

        public void ScrollToStart()
        {
            Trace.Call();

            MessageTextView.ScrollToStart();
        }

        public void ScrollToEnd()
        {
            Trace.Call();

            MessageTextView.ScrollToEnd();
        }
    }
}
