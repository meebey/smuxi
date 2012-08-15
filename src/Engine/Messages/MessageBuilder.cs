// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Smuxi.Engine
{
    public class MessageBuilder
    {
        MessageModel Message { get; set; }
        public bool NickColors { get; set; }
        public bool StripFormattings { get; set; }
        public bool StripColors { get; set; }
        public TextColor HighlightColor { get; set; }
        public List<string> HighlightWords { get; set; }
        public PersonModel Me { get; set; }

        public MessageType MessageType {
            get {
                return Message.MessageType;
            }
            set {
                Message.MessageType = value;
            }
        }
        
        public DateTime TimeStamp {
            get {
                return Message.TimeStamp;
            }
            set {
                Message.TimeStamp = value;
            }
        }

        public bool IsEmpty {
            get {
                return Message.IsEmpty;
            }
        }

        public MessageBuilder()
        {
            Message = new MessageModel();
            NickColors = true;
        }

        public MessageModel ToMessage()
        {
            //MessageParser.ParseSmileys
            MessageParser.ParseUrls(Message);
            Message.Compact();
            return Message;
        }

        public virtual void ApplyConfig(UserConfig userConfig)
        {
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }

            NickColors = (bool) userConfig["Interface/Notebook/Channel/NickColors"];
            StripColors = (bool) userConfig["Interface/Notebook/StripColors"];
            StripFormattings = (bool) userConfig["Interface/Notebook/StripFormattings"];
            HighlightColor = TextColor.Parse(
                (string) userConfig["Interface/Notebook/Tab/HighlightColor"]
            );
            HighlightWords = new List<string>(
                (string[]) userConfig["Interface/Chat/HighlightWords"]
            );
        }

        public virtual MessageBuilder Append(MessagePartModel msgPart)
        {
            if (msgPart == null) {
                throw new ArgumentNullException("msgPart");
            }

            Message.MessageParts.Add(msgPart);
            return this;
        }

        public virtual TextMessagePartModel CreateText(TextMessagePartModel text)
        {
            if (text == null) {
                throw new ArgumentNullException("text");
            }

            return new TextMessagePartModel(text);
        }

        public virtual TextMessagePartModel CreateText(string text,
                                                       params object[] args)
        {
            if (text == null) {
                throw new ArgumentNullException("text");
            }

            if (args != null && args.Length > 0) {
                text = String.Format(text, args);
            }
            return new TextMessagePartModel(text);
        }

        public virtual TextMessagePartModel CreateText()
        {
            return new TextMessagePartModel();
        }

        public virtual MessageBuilder AppendText(TextMessagePartModel textPart)
        {
            return Append(textPart);
        }

        public MessageBuilder AppendText(IEnumerable<TextMessagePartModel> text)
        {
            if (text == null) {
                throw new ArgumentNullException("text");
            }

            foreach (var textPart in text) {
                AppendText(textPart);
            }
            return this;
        }

        public virtual MessageBuilder AppendText(string text,
                                                 params object[] args)
        {
            text = text ?? String.Empty;

            if (text.Length == 0) {
                return this;
            }

            var textPart = CreateText(text, args);
            return AppendText(textPart);
        }

        public virtual TextMessagePartModel CreateSpace()
        {
            return CreateText(" ");
        }

        public MessageBuilder AppendSpace()
        {
            return AppendText(CreateSpace());
        }

        public virtual TextMessagePartModel CreateEventPrefix()
        {
            return CreateText("-!- ");
        }

        public virtual MessageBuilder AppendEventPrefix()
        {
            MessageType = MessageType.Event;
            return AppendText(CreateEventPrefix());
        }

        public virtual TextMessagePartModel CreateActionPrefix()
        {
            return CreateText(" * ");
        }

        public virtual MessageBuilder AppendActionPrefix()
        {
            return AppendText(CreateActionPrefix());
        }

        public virtual UrlMessagePartModel CreateUrl(string url, string text)
        {
            if (url == null) {
                throw new ArgumentNullException("url");
            }

            return new UrlMessagePartModel(url, text);
        }

        public UrlMessagePartModel CreateUrl(string url)
        {
            return CreateUrl(url, null);
        }

        public virtual MessageBuilder AppendUrl(string url, string text)
        {
            return Append(CreateUrl(url, text));
        }

        public MessageBuilder AppendUrl(string url)
        {
            return AppendUrl(url, null);
        }

        public virtual IList<TextMessagePartModel> CreateHeader(string text,
                                                                params object[] args)
        {
            if (text == null) {
                throw new ArgumentNullException("text");
            }

            var prefix = CreateText("[");
            var suffix = CreateText("]");
            var headerText = CreateText(text, args);
            headerText.Bold = true;

            var header = new List<TextMessagePartModel>(3);
            header.Add(prefix);
            header.Add(headerText);
            header.Add(suffix);
            return header;
        }

        public virtual MessageBuilder AppendHeader(string text,
                                                   params object[] args)
        {
            text = text ?? String.Empty;

            return AppendText(CreateHeader(text, args));
        }

        public virtual MessageBuilder AppendMessage(string msg)
        {
            return AppendText(msg);
        }

        public  MessageBuilder AppendMessage(ContactModel sender, string msg)
        {
            if (sender != null) {
                AppendSenderPrefix(sender);
            }
            return AppendMessage(msg);
        }

        public virtual MessageBuilder AppendWarningText(string errorText,
                                                        params string[] args)
        {
            var text = CreateText(errorText, args);
            text.Bold = true;
            return AppendText(text);
        }

        public virtual MessageBuilder AppendErrorText(string errorText,
                                                      params string[] args)
        {
            var text = CreateText(errorText, args);
            text.ForegroundColor = new TextColor(255, 0, 0);
            text.Bold = true;
            text.IsHighlight = true;
            return AppendText(text);
        }

        public virtual TextMessagePartModel CreateIdendityName(ContactModel identity)
        {
            if (identity == null) {
                throw new ArgumentNullException("identity");
            }

            if (!NickColors) {
                return CreateText(identity.IdentityName);
            }

            var identityName = CreateText(identity.IdentityNameColored);
            // don't clutter with the bg color
            identityName.BackgroundColor = TextColor.None;
            return identityName;
        }

        public virtual MessageBuilder AppendIdendityName(ContactModel identity,
                                                         bool isHighlight)
        {
            if (identity == null) {
                throw new ArgumentNullException("identity");
            }

            var identityName = CreateIdendityName(identity);
            identityName.IsHighlight = isHighlight;
            return AppendText(identityName);
        }

        public MessageBuilder AppendIdendityName(ContactModel identity)
        {
            return AppendIdendityName(identity, false);
        }

        public virtual IList<TextMessagePartModel> CreateNick(ContactModel contact)
        {
            if (contact == null) {
                throw new ArgumentNullException("contact");
            }

            var prefix = CreateText("<");
            var suffix = CreateText(">");
            var nick = CreateIdendityName(contact);
            if (NickColors) {
                // using bg colors for the nick texts are too intrusive, thus
                // map the bg color to the fg color of the surrounding tags
                var senderBgColor = contact.IdentityNameColored.BackgroundColor;
                if (senderBgColor != TextColor.None) {
                    prefix.ForegroundColor = senderBgColor;
                    suffix.ForegroundColor = senderBgColor;
                    nick.BackgroundColor = TextColor.None;
                }
            }

            var senderMsg = new List<TextMessagePartModel>(3);
            senderMsg.Add(prefix);
            senderMsg.Add(nick);
            senderMsg.Add(suffix);
            return senderMsg;
        }

        public virtual MessageBuilder AppendNick(ContactModel contact)
        {
            if (contact == null) {
                throw new ArgumentNullException("contact");
            }

            return AppendText(CreateNick(contact));
        }

        public virtual IList<TextMessagePartModel> CreateSenderPrefix(ContactModel contact)
        {
            if (contact == null) {
                throw new ArgumentNullException("contact");
            }

            var sender = CreateNick(contact);
            sender.Add(CreateSpace());
            return sender;
        }

        public virtual MessageBuilder AppendSenderPrefix(ContactModel contact,
                                                         bool isHighlight)
        {
            if (contact == null) {
                throw new ArgumentNullException("sender");
            }

            var senderMsg = CreateSenderPrefix(contact);
            /*
            // 1st element is prefix
            // 3rdt element is prefix
            if (isHighlight) {
                // HACK: reset fg color of prefix and suffix so highlight color
                // will be applied instead
                senderMsg[0].ForegroundColor = TextColor.None;
                senderMsg[2].ForegroundColor = TextColor.None;
            }
            */
            // 2nd element is the nick
            senderMsg[1].IsHighlight = isHighlight;
            foreach (var senderPart in senderMsg) {
                AppendText(senderPart);
            }
            return this;
        }

        public MessageBuilder AppendSenderPrefix(ContactModel sender)
        {
            return AppendSenderPrefix(sender, false);
        }

        public bool ContainsHighlight()
        {
            return ContainsHighlight(Message.ToString());
        }

        public virtual bool ContainsHighlight(string text)
        {
            Regex regex;
            if (Me != null) {
                // First check to see if our current nick is in there.
                regex = new Regex(
                    String.Format(
                        "(^|\\W){0}($|\\W)",
                        Regex.Escape(Me.IdentityName)
                    ),
                    RegexOptions.IgnoreCase
                );
                if (regex.Match(text).Success) {
                    return true;
                }
            }

            // go through the user's custom highlight words and check for them.
            foreach (string highLightWord in HighlightWords) {
                if (String.IsNullOrEmpty(highLightWord)) {
                    continue;
                }

                if (highLightWord.StartsWith("/") && highLightWord.EndsWith("/")) {
                    // This is a regex, so just build a regex out of the string.
                    regex = new Regex(
                        highLightWord.Substring(1, highLightWord.Length - 2)
                    );
                } else {
                    // Plain text - make a regex that matches the word as long as it's separated properly.
                    string regex_string = String.Format(
                        "(^|\\W){0}($|\\W)",
                        Regex.Escape(highLightWord)
                    );
                    regex = new Regex(regex_string, RegexOptions.IgnoreCase);
                }

                if (regex.Match(text).Success) {
                    return true;
                }
            }

            return false;
        }

        public virtual void MarkHighlights()
        {
            bool containsHighlight = false;
            foreach (var part in Message.MessageParts) {
                if (!(part is TextMessagePartModel)) {
                    continue;
                }

                var textPart = (TextMessagePartModel) part;
                if (String.IsNullOrEmpty(textPart.Text)) {
                    // URLs without a link name don't have text
                    continue;
                }
                if (ContainsHighlight(textPart.Text)) {
                    containsHighlight = true;
                }
            }

            if (!containsHighlight) {
                // nothing to do
                return;
            }

            // colorize the whole message
            foreach (MessagePartModel msgPart in Message.MessageParts) {
                if (!(msgPart is TextMessagePartModel)) {
                    continue;
                }

                TextMessagePartModel textMsg = (TextMessagePartModel) msgPart;
                if (textMsg.ForegroundColor != null &&
                    textMsg.ForegroundColor != TextColor.None) {
                    // HACK: don't overwrite colors as that would replace
                    // nick-colors for example
                    continue;
                }
                // HACK: we have to mark all parts as highlight else
                // ClearHighlights() has no chance to properly undo all
                // highlights
                textMsg.IsHighlight = true;
                textMsg.ForegroundColor = HighlightColor;
            }
            return;
        }

        public virtual void ClearHighlights()
        {
            foreach (var msgPart in Message.MessageParts) {
                if (!msgPart.IsHighlight || !(msgPart is TextMessagePartModel)) {
                    continue;
                }

                var textMsg = (TextMessagePartModel) msgPart;
                textMsg.IsHighlight = false;
                textMsg.ForegroundColor = null;
            }
            return;
        }
    }
}
