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
    public class ChatView : IChatView
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

        public ChatModel ChatModel {
            get {
                return f_ChatModel;
            }
        }

        public bool IsVisible {
            get {
                return f_MainWindow[f_WidgetName + "_display"] == "1";
            }
            set {
                /*
                f_MainWindow.Modify(f_WidgetName, "replace",
                                   ".display:" + (value ?  "1" : "0"));
                */
                //f_MainWindow[f_WidgetName + ":.display"] = value ?  "1" : "0";
                f_MainWindow[f_WidgetID + "d"] = value ?  "1" : "0";
           }
        }

        public string WidgetName {
            get {
                return f_WidgetName;
            }
        }
        
        public int Offset {
            get {
                int offset;
                string offsetStr = f_MainWindow[f_WidgetID + "os"];
                if (!Int32.TryParse(offsetStr, out offset)) {
#if LOG4NET
                    _Logger.Error("Offset(): Int32.Parse(\"" + offsetStr + "\")  failed!");
#endif
                    return 0;
                }
                return offset;
            }
            set {
                f_MainWindow[f_WidgetID + "os"] = value.ToString();
            }
        }
        
        public int Heigth {
            get {
                // force height refresh
                f_MainWindow.Run(-3);
                //string heigthStr = f_MainWindow[String.Format("{0}:h", f_WidgetName)];
                string heigthStr = f_MainWindow["output_vbox:h"];
                int heigth;
                if (!Int32.TryParse(heigthStr, out heigth)) {
#if LOG4NET
                    _Logger.Error("get_Heigth(): Int32.Parse(\"" + heigthStr + "\") failed!");
#endif
                    return 0;
                }
                return heigth;
            }
        }

        public int Width {
            get {
                // force height refresh
                f_MainWindow.Run(-3);
                //string widthStr = f_MainWindow[String.Format("{0}:w", f_WidgetName)];
                string widthStr = f_MainWindow["output_vbox:w"];
                int width;
                if (!Int32.TryParse(widthStr, out width)) {
#if LOG4NET
                    _Logger.Error("get_Width(): Int32.Parse(\"" + widthStr + "\") failed!");
#endif
                    return 0;
                }
                return width;
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

            // FIXME: move me to the caller scope
            //f_MainWindow.Modify("output_vbox", "append", "{textview[ " + f_WidgetName + "] .expand:vh .display:0 offset:0}");
            f_MainWindow.Modify("output_vbox", "append",
                "{" +
                    "textview[" + f_WidgetName + "] " +
                        ".expand:vh " +
                        ".display[" + f_WidgetID + "d]:0 " +
                        "offset[" + f_WidgetID + "os]:0 " +
                        "style_end:\"\" " +
                        "richtext:1 " +
                        "style_red_normal:fg=red " +
                        "style_u_normal:attr=underline " +
                        "style_b_normal:attr=bold " +
                        "style_i_normal:attr=standout " +
                "}"
            );
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
        
        public void AddMessage(MessageModel msg)
        {
            // OPT: typical message length
            var line = new StringBuilder(512);
            int msgLength = 0;
            foreach (MessagePartModel msgPart in msg.MessageParts) {
                // TODO: implement other types
                if (msgPart is TextMessagePartModel) {
                    var txtPart = (TextMessagePartModel) msgPart;
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

                    if (tags.Count > 0) {
                        tags.Reverse();
                        string markup = txtPart.Text;
                        foreach (string tag in tags) {
                            markup = String.Format("<{0}>{1}</{2}>",
                                                   tag, markup, tag);
                        }
                        line.Append(markup);
                    } else {
                        line.Append(txtPart.Text);
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

            // TODO: implement line wrap and re-wrap when console size changes
            f_MainWindow.Modify(f_WidgetName, "append", "{listitem text:" + StflApi.stfl_quote(finalMsg) + "}");

            ScrollToEnd();
        }
        
        public void ScrollUp()
        {
            Trace.Call();
            
            Scroll(-0.9);
        }
        
        public void ScrollDown()
        {
            Trace.Call();
            
            Scroll(0.9);
        }

        protected void Scroll(double scrollFactor)
        {
            int currentOffset = Offset;
            int newOffset = (int) (currentOffset + (Heigth * scrollFactor));
#if LOG4NET
            _Logger.Debug("Scroll(" + scrollFactor + "):" + 
                          " chat: " + ChatModel.ID +
                          " old offset: " + currentOffset +
                          " new offset: " + newOffset);
#endif
            if (newOffset < 0) {
                newOffset = 0;
            } else if (newOffset > f_ChatModel.Messages.Count) {
                newOffset = f_ChatModel.Messages.Count;
            }
            Offset = newOffset;
        }

        public void ScrollToStart()
        {
            Trace.Call();
            
        }
        
        public void ScrollToEnd()
        {
            Trace.Call();
            
#if LOG4NET
            //_Logger.Debug("output_textview_offset: " + f_MainWindow["output_textview_offset"]);
            //_Logger.Debug("output_textview_pos: " + f_MainWindow["output_textview_pos"]);
            //_Logger.Debug("dump: " + _MainWindow.Dump("output_textview", "", 0));
#endif
            //f_MainWindow["output_textview_offset"] = (f_ChatModel.Messages.Count - 1).ToString();
        }
    }
}
