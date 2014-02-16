// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014 Mirco Bauer <meebey@meebey.net>
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

namespace Smuxi.Engine
{
    public class MessagePatternModel
    {
        public int? ID { get; set; }
        public Regex MessagePartPattern { get; set; }
        public Type MessagePartType { get; set; }
        // what is linked to
        public string LinkFormat { get; set; }
        // what is displayed
        public string TextFormat { get; set; }

        protected string ConfigKeyPrefix {
            get {
                if (ID == null) {
                    throw new ArgumentNullException("ID");
                }
                return "MessagePatterns/" + ID + "/";
            }
        }

        public MessagePatternModel(Regex pattern)
        {
            if (pattern == null) {
                throw new ArgumentNullException("pattern");
            }
            MessagePartPattern = pattern;
            MessagePartType = typeof(UrlMessagePartModel);
        }

        public MessagePatternModel(int id)
        {
            ID = id;
        }

        public void Load(UserConfig config)
        {
            if (ID == null) {
                throw new InvalidOperationException("ID must not be null.");
            }

            Load(config, ID.Value);
        }

        public virtual void Load(UserConfig config, int id)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            // don't use ConfigKeyPrefix, so exception guarantees can be kept
            string prefix = "MessagePatterns/" + id + "/";
            if (config[prefix + "MessagePartPattern"] == null) {
                // SmartLink does not exist
                throw new ArgumentException("MessagePattern ID not found in config", "id");
            }

            ID = id;
            // now we have a valid ID, ConfigKeyPrefix works
            var messagePartPattern = (string) config[ConfigKeyPrefix + "MessagePartPattern"];
            if (messagePartPattern.StartsWith("/") && messagePartPattern.EndsWith("/i")) {
                var regexPattern = messagePartPattern.Substring(1, messagePartPattern.Length - 3);
                MessagePartPattern = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            } else {
                MessagePartPattern = new Regex(messagePartPattern, RegexOptions.Compiled);
            }
            var messagePartType = (string) config[ConfigKeyPrefix + "MessagePartType"];
            switch (messagePartType.ToLower()) {
                case "url":
                    MessagePartType = typeof(UrlMessagePartModel);
                    break;
                case "image":
                    MessagePartType = typeof(ImageMessagePartModel);
                    break;
            }
            LinkFormat = (string) config[ConfigKeyPrefix + "LinkFormat"];
            TextFormat = (string) config[ConfigKeyPrefix + "TextFormat"];
        }

        public virtual void Save(UserConfig config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            if (MessagePartPattern == null) {
                config[ConfigKeyPrefix + "MessagePartPattern"] = String.Empty;
            } else {
                config[ConfigKeyPrefix + "MessagePartPattern"] = MessagePartPattern.ToString();
            }
            if (MessagePartType == typeof(ImageMessagePartModel)) {
                config[ConfigKeyPrefix + "MessagePartType"] = "Image";
            } else if (MessagePartType == typeof(UrlMessagePartModel)) {
                config[ConfigKeyPrefix + "MessagePartType"] = "Url";
            } else {
                config[ConfigKeyPrefix + "MessagePartType"] = String.Empty;
            }
            config[ConfigKeyPrefix + "LinkFormat"] = LinkFormat ?? String.Empty;
            config[ConfigKeyPrefix + "TextFormat"] = TextFormat ?? String.Empty;
        }

        public override string ToString()
        {
            return String.Format("<{0}>", ToTraceString());
        }

        public string ToTraceString()
        {
            return String.Format("{0}", ID);
        }
    }
}
