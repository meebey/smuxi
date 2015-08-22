// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2012 Carlos Martín Nieto <cmn@dwim.me>
//
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using ServiceStack.Text;
using ServiceStack.ServiceClient.Web;
using Smuxi.Common;
using Smuxi.Engine.Campfire;

namespace Smuxi.Engine
{
    internal class MessageReceivedEventArgs : EventArgs
    {
        public GroupChatModel Chat { get; private set; }
        public Message Message { get; private set; }

        public MessageReceivedEventArgs(GroupChatModel chat, Message message)
        {
            Chat = chat;
            Message = message;
        }
    }

    internal class ErrorReceivedEventArgs : EventArgs
    {
        public HttpStatusCode StatusCode { get; private set; }
        public string StatusDescription { get; private set; }

        public ErrorReceivedEventArgs(HttpStatusCode code, string description)
        {
            StatusCode = code;
            StatusDescription = description;
        }
    }

    internal class CampfireEventStream : IDisposable
    {
        public EventHandler<MessageReceivedEventArgs> MessageReceived;
        public EventHandler<ErrorReceivedEventArgs> ErrorReceived;

        HttpWebRequest Request { get; set; }
        GroupChatModel Chat { get; set; }
        NetworkCredential Cred { get; set; }
        Thread Thread { get; set; }
        Uri BaseUri { get; set; }
        string Host { get; set; }
        int LastMessage { get; set; }

        public CampfireEventStream(GroupChatModel chat, Uri baseuri, NetworkCredential cred)
        {
            this.Chat = chat;
            this.Cred = cred;
            this.Host = Host;
            this.BaseUri = baseuri;
            this.LastMessage = 0;
        }
        
        public void Start()
        {
            Thread = new Thread(DoWork);
            Thread.Start();
        }

        void FillHole()
        {
            var client = new JsonServiceClient(BaseUri.AbsoluteUri);
            client.Credentials = Cred;
            var messages = client.Get<MessagesResponse>(
                String.Format("/room/{0}.json?since={1}", Chat.ID, LastMessage)).Messages;

            if (messages == null)
                return;

            foreach (var message in messages) {
                if (MessageReceived != null) {
                    var args = new MessageReceivedEventArgs(Chat, message);
                    MessageReceived(this, args);
                }
                LastMessage = message.Id;
            }
        }

        void DoWork()
        {
            while (true) {
                try {
                    // if LastMessage > 0 we're reconnecting, so we need to ask
                    // the server for the messages we've missed
                    if (LastMessage > 0) {
                        FillHole();
                    }
                    ParseStream();
                } catch (TimeoutException) {
                    // Not to worry, let's just connect again
                } catch (WebException e) {
                    if (e.Status == WebExceptionStatus.ProtocolError) {
                        var resp = (HttpWebResponse) e.Response;
                        if (resp.StatusCode == HttpStatusCode.Unauthorized ||
                            resp.StatusCode == HttpStatusCode.Forbidden) {
                            if (ErrorReceived != null) {
                                ErrorReceived(this, new ErrorReceivedEventArgs(resp.StatusCode, resp.StatusDescription));
                            }

                            return;
                        }
                        // it's not such a bad error, sleep for a bit before trying again
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                }
            }
        }

        public void ParseStream()
        {
            Request = HttpWebRequest.Create(
                String.Format("https://streaming.campfirenow.com/room/{0}/live.json", Chat.ID)) as HttpWebRequest;
            Request.Credentials = Cred;
            Request.PreAuthenticate = true;
            var res = Request.GetResponse() as HttpWebResponse;

            using (StreamReader reader = new StreamReader(res.GetResponseStream()))
            {
                StringBuilder bld = new StringBuilder();

                while (!reader.EndOfStream) {
                    var c = reader.Read();

                    /* The server uses CR to indicate when each message ends */
                    if (c != '\r') {
                        bld.Append((char)c);
                        continue;
                    }

                    var str = bld.ToString();
                    bld.Length = 0;

                    var message = JsonSerializer.DeserializeFromString<Message>(str);
                    if (MessageReceived != null) {
                        var args = new MessageReceivedEventArgs(Chat, message);
                        MessageReceived(this, args);
                    }

                    LastMessage = message.Id;
                }

                reader.Close();
            }

            res.Close();
            Request = null;
        }

        public void Dispose()
        {
            Thread.Abort();
        }
    }
}

