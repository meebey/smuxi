// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2010-2014 Mirco Bauer <meebey@meebey.net>
// Copyright (c) 2013 Oliver Schneider <mail@oli-obk.de>
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Web;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class MessageBuilder
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        static readonly string LibraryTextDomain = "smuxi-engine";
        MessageModel Message { get; set; }
        public PersonModel Me { get; set; }
        public MessageBuilderSettings Settings { get; set; }

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

        public string ID {
            get {
                return Message.ID;
            }
            set {
                Message.ID = value;
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
            Settings = new MessageBuilderSettings();
        }

        public MessageModel ToMessage()
        {
            Message.Compact();
            return Message;
        }

        public virtual void ApplyConfig(UserConfig userConfig)
        {
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }

            Settings.ApplyConfig(userConfig);
        }

        public virtual MessageBuilder Append(MessagePartModel msgPart)
        {
            if (msgPart == null) {
                throw new ArgumentNullException("msgPart");
            }

            Message.MessageParts.Add(msgPart);
            return this;
        }

        public virtual MessageBuilder Append(IEnumerable<MessagePartModel> msgParts)
        {
            if (msgParts == null) {
                throw new ArgumentNullException("msgParts");
            }

            foreach (var part in msgParts) {
                Append(part);
            }
            return this;
        }

        public virtual MessageBuilder Append(MessageModel msg)
        {
            if (msg == null) {
                throw new ArgumentNullException("msg");
            }

            foreach (var part in msg.MessageParts) {
                Append(part);
            }
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
            return Append(ParsePatterns(CreateText(msg)));
        }

        public  MessageBuilder AppendMessage(ContactModel sender, string msg)
        {
            if (sender != null) {
                AppendSenderPrefix(sender);
            }
            return AppendMessage(msg);
        }

        public virtual MessageBuilder AppendWarningText(string errorText,
                                                        params object[] args)
        {
            var text = CreateText(errorText, args);
            text.Bold = true;
            return AppendText(text);
        }

        public virtual MessageBuilder AppendErrorText(string errorText,
                                                      params object[] args)
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

            if (!Settings.NickColors) {
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
            if (Settings.NickColors) {
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
            foreach (string highLightWord in Settings.HighlightWords) {
                if (String.IsNullOrEmpty(highLightWord)) {
                    continue;
                }

                if (highLightWord.StartsWith("/") && highLightWord.EndsWith("/")) {
                    // This is a regex, so just build a regex out of the string.
                    regex = new Regex(
                        highLightWord.Substring(1, highLightWord.Length - 2),
                        RegexOptions.IgnoreCase
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

            MarkAsHighlight();
        }

        public virtual void MarkAsHighlight()
        {
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
                textMsg.ForegroundColor = Settings.HighlightColor;
            }
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
        
        void ParseStyle(XmlNode style, TextMessagePartModel submodel)
        {
            if (style == null) return;
            var properties = style.InnerText.Split(';');
            foreach (string property in properties) {
                var colonpos = property.IndexOf(':');
                if (colonpos == -1) continue;
                string name = property.Substring(0, colonpos).Trim();
                string value = property.Substring(colonpos+1).Trim();
                switch (name) {
                    case "background":
                        value = value.Split(' ')[0];
                        submodel.BackgroundColor = TextColor.Parse(value);
                        break;
                    case "background-color":
                        submodel.BackgroundColor = TextColor.Parse(value);
                        break;
                    case "color":
                        submodel.ForegroundColor = TextColor.Parse(value);
                        break;
                    case "font-style":
                        if (value == "normal") {
                            submodel.Italic = false;
                        } else if (value == "inherit") {
                        } else {
                            submodel.Italic = true;
                        }
                        break;
                    case "font-weight":
                        if (value == "normal") {
                            submodel.Bold = false;
                        } else if (value == "inherit") {
                        } else {
                            submodel.Bold = true;
                        }
                        break;
                    case "text-decoration":
                    {
                        foreach (string val in value.Split(' ')) {
                            if (val == "underline") {
                                submodel.Underline = true;
                            }
                        }
                    }
                        break;
                    case "font-family":
                    case "font-size":
                    case "text-align":
                    case "margin-left":
                    case "margin-right":
                    default:
                        // ignore formatting
                        break;
                }
            }
        }
        
        void ParseHtml(XmlNode node, TextMessagePartModel model)
        {
            TextMessagePartModel submodel;
            string nodetype = node.Name.ToLower();
            if (model is UrlMessagePartModel) {
                submodel = new UrlMessagePartModel(model);
            } else if (nodetype == "a") {
                submodel = new UrlMessagePartModel(model);
                (submodel as UrlMessagePartModel).Url = node.Attributes.GetNamedItem("href").Value;
            } else {
                submodel = new TextMessagePartModel(model);
            }
            switch (nodetype) {
                case "b":
                case "strong":
                    submodel.Bold = true;
                    break;
                case "i":
                case "em":
                    submodel.Italic = true;
                    break;
                case "u":
                    submodel.Underline = true;
                    break;
                default:
                    break;
            }
            if (node.Attributes != null) {
                ParseStyle(node.Attributes.GetNamedItem("style"), submodel);
            }
            if (node.HasChildNodes) {
                foreach (XmlNode child in node.ChildNodes) {
                    // clone this model
                    TextMessagePartModel nextmodel;
                    if (submodel is UrlMessagePartModel) {
                        nextmodel = new UrlMessagePartModel(submodel);
                    } else {
                        nextmodel = new TextMessagePartModel(submodel);
                    }
                    ParseHtml(child, nextmodel);
                }
            } else {
                // final node
                if (nodetype == "br") {
                    AppendText("\n");
                } else if (nodetype == "img") {
                    AppendUrl(node.Attributes.GetNamedItem("src").Value, "[image placeholder - UNIMPLEMENTED]");
                } else {
                    model.Text = HttpUtility.HtmlDecode(node.Value);
                    AppendText(model);
                }
            }
        }

        public virtual MessageBuilder AppendHtmlMessage(string html)
        {
            html = NormalizeNewlines(html);
            XmlDocument doc = new XmlDocument();
            try {
                // wrap in div to prevent messages beginning with text from failing "to be xml"
                doc.Load(new StringReader("<html>"+html+"</html>"));
            } catch (XmlException ex) {
#if LOG4NET
                f_Logger.Error("AppendHtmlMessage(): error parsing html: " + html, ex);
#endif
                AppendText(html);
                return this;
            }
            ParseHtml(doc, new TextMessagePartModel());
            return this;
        }

        public virtual IList<MessagePartModel> CreateFormat(string format, params object[] objs)
        {
            if (format == null) {
                throw new ArgumentNullException("format");
            }
            if (objs == null) {
                throw new ArgumentNullException("objs");
            }

            var parts = new List<MessagePartModel>();
            var assembling = new StringBuilder(format.Length);
            var inPlaceholder = false;

            for (int i = 0; i < format.Length; ++i) {
                char c = format[i];
                char peek = (i < format.Length-1) ? format[i+1] : '\0';

                if (c == '{') {
                    if (peek == '{') {
                        // escaped brace
                        assembling.Append('{');

                        // skip the second brace too
                        ++i;
                    } else if (!inPlaceholder) {
                        // we're parsing a placeholder here

                        // first, append the currently assembled string
                        parts.Add(CreateText(assembling.ToString()));

                        // we will now assemble the placeholder text
                        assembling.Length = 0;
                        inPlaceholder = true;
                    } else {
                        // nested formatting?!
                        throw new System.FormatException("nested formatting is forbidden");
                    }
                } else if (c == '}') {
                    if (peek == '}') {
                        // escaped brace
                        assembling.Append('}');

                        // skip the second brace too
                        ++i;
                    } else if (inPlaceholder) {
                        // substitute the placeholder

                        var placeholderText = assembling.ToString();
                        uint placeholderInt;

                        if (!uint.TryParse(placeholderText, out placeholderInt)) {
                            // that's not even an integer...
                            throw new System.FormatException("format placeholder must be an integer >= 0 in braces");
                        }
                        if (placeholderInt >= objs.Length) {
                            // placeholder out of bounds
                            throw new System.FormatException("format placeholder number is greater than the array");
                        }

                        var placeMe = objs[placeholderInt];
                        if (placeMe == null) {
                            throw new System.FormatException("null object in objs array");
                        } else if (placeMe is String) {
                            // append strings as-is
                            parts.Add(CreateText((String) placeMe));
                        } else if (placeMe is ContactModel) {
                            // append contacts as their identity names
                            parts.Add(CreateIdendityName((ContactModel) placeMe));
                        } else if (placeMe is MessagePartModel) {
                            // append the part verbatim
                            parts.Add((MessagePartModel) placeMe);
                        } else if (placeMe is MessageModel) {
                            // append all parts of the message
                            foreach (var part in ((MessageModel) placeMe).MessageParts) {
                                parts.Add(part);
                            }
                        } else if (placeMe.GetType().IsValueType) {
                            parts.Add(CreateText(placeMe.ToString()));
                        } else {
                            // no idea how to format this
                            throw new System.FormatException("unknown object type to format: " + placeMe.GetType().ToString());
                        }

                        // we are done with this placeholder
                        assembling.Length = 0;
                        inPlaceholder = false;
                    } else {
                        // closing brace without opening brace
                        throw new System.FormatException("format placeholder closing brace without corresponding opening brace");
                    }
                } else {
                    // simply append
                    assembling.Append(c);
                }
            }

            // done parsing

            if (inPlaceholder) {
                // unterminated brace
                throw new System.FormatException("format placeholder opening brace without corresponding closing brace");
            }

            if (assembling.Length > 0) {
                // bit of text at the end
                parts.Add(CreateText(assembling.ToString()));
            }

            return parts;
        }

        public virtual MessageBuilder AppendFormat(string format, params object[] objs)
        {
            foreach (var part in CreateFormat(format, objs)) {
                Append(part);
            }
            return this;
        }

        public virtual MessageBuilder AppendChatState(ContactModel contact, MessageType state)
        {
            switch (state) {
                case MessageType.ChatStateComposing:
                    if (Message.IsEmpty) {
                        AppendActionPrefix();
                        AppendFormat(_("{0} is typing..."), contact);
                    }
                    break;
                case MessageType.ChatStatePaused:
                    if (Message.IsEmpty) {
                        AppendActionPrefix();
                        AppendFormat(_("{0} has stopped typing..."), contact);
                    }
                    break;
                case MessageType.ChatStateReset:
                    break;
                default:
                    throw new ArgumentException("state is not a ChatState", "state");
            }
            MessageType = state;
            return this;
        }

        protected static string NormalizeNewlines(string text)
        {
            if (text == null) {
                throw new ArgumentNullException("text");
            }
            if (!text.Contains("\n")) {
                // nothing to normalize
                return text;
            }

            var normalized = new StringBuilder(text.Length);
            text = text.Replace("\r\n", "\n");
            foreach (var textPart in text.Split('\n')) {
                var trimmed = textPart.TrimEnd(' ');
                if (trimmed.Length == 0) {
                    // skip empty lines
                    continue;
                }
                normalized.AppendFormat("{0} ", trimmed);
            }
            // remove trailing space
            if (normalized.Length > 0) {
                normalized.Length--;
            }
            return normalized.ToString();
        }

        public virtual MessageBuilder AppendPresenceState(ContactModel contact, MessageType state)
        {
            switch (state) {
                case MessageType.PresenceStateAway:
                    if (Message.IsEmpty) {
                        AppendActionPrefix();
                        AppendFormat(_("{0} is away"), contact);
                    }
                    break;
                case MessageType.PresenceStateOffline:
                    if (Message.IsEmpty) {
                        AppendActionPrefix();
                        AppendFormat(_("{0} is offline"), contact);
                    }
                    break;
                case MessageType.PresenceStateOnline:
                    if (Message.IsEmpty) {
                        AppendActionPrefix();
                        AppendFormat(_("{0} is online"), contact);
                    }
                    break;
                default:
                    throw new ArgumentException("state is not a PresenceState", "state");
            }
            MessageType = state;
            return this;
        }

        static string _(string msg)
        {
            return LibraryCatalog.GetString(msg, LibraryTextDomain);
        }

        public static IList<MessagePartModel> ParsePatterns(TextMessagePartModel textPart,
                                                            List<MessagePatternModel> patterns)
        {
            if (textPart == null) {
                throw new ArgumentNullException("textPart");
            }
            if (patterns == null) {
                throw new ArgumentNullException("patterns");
            }

            var msgParts = new List<MessagePartModel>();
            if (patterns.Count == 0) {
                // all patterns have been tried -> this text is PURE text
                msgParts.Add(textPart);
                return msgParts;
            }

            var remainingPatterns = new List<MessagePatternModel>(patterns);
            var pattern = remainingPatterns.First();
            remainingPatterns.Remove(pattern);
            
            var match = pattern.MessagePartPattern.Match(textPart.Text);
            if (!match.Success) {
                // no matches in this MessagePart, try other smartlinks
                return ParsePatterns(textPart, remainingPatterns);
            }
            
            int lastindex = 0;
            do {
                var groupValues = new string[match.Groups.Count];
                int i = 0;
                foreach (Group @group in match.Groups) {
                    groupValues[i++] = @group.Value;
                }
                
                string url;
                if (String.IsNullOrEmpty(pattern.LinkFormat)) {
                    url = match.Value;
                } else {
                    url = String.Format(pattern.LinkFormat, groupValues);
                }
                string text;
                if (String.IsNullOrEmpty(pattern.TextFormat)) {
                    text = match.Value;
                } else {
                    text = String.Format(pattern.TextFormat, groupValues);
                }

                if (lastindex != match.Index) {
                    // there were some non-matching-chars before the match
                    // copy that to a TextMessagePartModel
                    var notMatchPart = new TextMessagePartModel(textPart);
                    // only take the proper chunk of text
                    notMatchPart.Text = textPart.Text.Substring(lastindex, match.Index - lastindex);
                    // and try other patterns on this part
                    var parts = ParsePatterns(notMatchPart, remainingPatterns);
                    foreach (var part in parts) {
                        msgParts.Add(part);
                    }
                }
                
                MessagePartModel msgPart;
                if (pattern.MessagePartType == typeof(UrlMessagePartModel)) {
                    // no need to set URL and text if they are the same
                    text = text == url ? null : text;
                    msgPart = new UrlMessagePartModel(url, text);
                } else if (pattern.MessagePartType == typeof(ImageMessagePartModel)) {
                    msgPart = new ImageMessagePartModel(url, text);
                } else {
                    msgPart = new TextMessagePartModel(text);
                }
                msgParts.Add(msgPart);
                lastindex = match.Index + match.Length;
                match = match.NextMatch();
            } while (match.Success);
            
            if (lastindex != textPart.Text.Length) {
                // there were some non-url-chars before this url
                // copy TextMessagePartModel
                var notMatchPart = new TextMessagePartModel(textPart);
                // only take the proper chunk of text
                notMatchPart.Text = textPart.Text.Substring(lastindex);
                // and try other smartlinks on this part
                var parts = ParsePatterns(notMatchPart, remainingPatterns);
                foreach (var part in parts) {
                    msgParts.Add(part);
                }
            }
            return msgParts;
        }
        
        public IEnumerable<MessagePartModel> ParsePatterns(TextMessagePartModel part)
        {
            return ParsePatterns(part, Settings.Patterns);
        }
    }
}
