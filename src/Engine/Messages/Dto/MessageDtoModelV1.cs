// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012, 2014 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;

namespace Smuxi.Engine.Dto
{
    public class MessageDtoModelV1
    {
        public DateTime TimeStamp { get; set; }
        public List<MessagePartDtoModelV1> MessageParts { get; set; }
        public MessageType MessageType { get; set; }

        public MessageDtoModelV1()
        {
            MessageParts = new List<MessagePartDtoModelV1>();
        }

        public MessageDtoModelV1(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            TimeStamp = msg.TimeStamp;
            MessageType = msg.MessageType;
            MessageParts = new List<MessagePartDtoModelV1>(msg.MessageParts.Count);
            foreach (var msgPart in msg.MessageParts) {
                var dtoPart = new MessagePartDtoModelV1(msgPart);
                MessageParts.Add(dtoPart);
            }
        }

        public virtual MessageModel ToMessage()
        {
            var msg = new MessageModel() {
                MessageType = this.MessageType,
                TimeStamp = this.TimeStamp
            };
            foreach (var msgPart in MessageParts) {
                MessagePartModel part = null;
                switch (msgPart.Type) {
                    case "Text":
                        var textPart = new TextMessagePartModel() {
                            ForegroundColor = msgPart.ForegroundColor,
                            BackgroundColor = msgPart.BackgroundColor,
                            Underline = msgPart.Underline,
                            Bold = msgPart.Bold,
                            Italic = msgPart.Italic,
                            Text = msgPart.Text
                        };
                        part = textPart;
                        break;
                    case "URL":
                        var urlPart = new UrlMessagePartModel() {
                            Url = msgPart.Url,
                            Text = msgPart.Text
                        };
                        part = urlPart;
                        break;
                    case "Image":
                        var imagePart = new ImageMessagePartModel() {
                            ImageFileName = msgPart.ImageFileName,
                            AlternativeText = msgPart.AlternativeText
                        };
                        part = imagePart;
                        break;
                }
                if (part == null) {
                    continue;
                }
                part.IsHighlight = msgPart.IsHighlight;
                msg.MessageParts.Add(part);
            }
            return msg;
        }
    }

    public class MessagePartDtoModelV1
    {
        // MessagePartModel
        public string Type { get; set; }
        public bool IsHighlight { get; set; }
        // TextMessagePartModel
        public TextColor ForegroundColor { get; set; }
        public TextColor BackgroundColor { get; set; }
        public bool Underline { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public string Text { get; set; }
        // UrlMessagePartModel
        public string Url { get; set; }
        public UrlProtocol? Protocol { get; set; }
        // ImageMessagePartModel
        public string ImageFileName { get; set; }
        public string AlternativeText { get; set; }

        public MessagePartDtoModelV1()
        {
        }

        public MessagePartDtoModelV1(MessagePartModel part)
        {
            if (part == null) {
                throw new ArgumentNullException("part");
            }

            IsHighlight = part.IsHighlight;
            if (part is TextMessagePartModel) {
                var textPart = (TextMessagePartModel) part;
                Type = "Text";
                ForegroundColor = textPart.ForegroundColor;
                BackgroundColor = textPart.BackgroundColor;
                Underline = textPart.Underline;
                Bold = textPart.Bold;
                Italic = textPart.Italic;
                Text = textPart.Text;
            }
            if (part is UrlMessagePartModel) {
                var urlPart = (UrlMessagePartModel) part;
                Type = "URL";
                Url = urlPart.Url;
                Text = urlPart.Text;
            }
            if (part is ImageMessagePartModel) {
                var imagePart = (ImageMessagePartModel) part;
                Type = "Image";
                ImageFileName = imagePart.ImageFileName;
                AlternativeText = imagePart.AlternativeText;
            }
        }
    }
}
