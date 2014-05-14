/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2014 Mirco Bauer <meebey@meebey.net>
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
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Engine
{
    public class MessagePatternListController
    {
        UserConfig UserConfig { get; set; }

        protected string[] PatternIDs {
            get {
                return (string[]) UserConfig["MessagePatterns/MessagePatterns"];
            }
            set {
                UserConfig["MessagePatterns/MessagePatterns"] = value;
            }
        }

        public MessagePatternListController(UserConfig userConfig)
        {
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }

            UserConfig = userConfig;
        }

        public List<MessagePatternModel> GetList()
        {
            var keys = PatternIDs;
            var list = new List<MessagePatternModel>(keys.Length);
            if (keys == null) {
                return list;
            }
            foreach (var key in keys) {
                int parsedKey = Int32.Parse(key);
                var link = Get(parsedKey);
                if (link == null) {
                    continue;
                }
                list.Add(link);
            }
            return list;
        }

        public MessagePatternModel Get(int id)
        {
            Trace.Call(id);

            string prefix = "MessagePatterns/" + id + "/";
            if (UserConfig[prefix + "MessagePartPattern"] == null) {
                // link does not exist
                return null;
            }
            var link = new MessagePatternModel(id);
            link.Load(UserConfig);
            return link;
        }

        public int Add(MessagePatternModel link)
        {
            return Add(link, -1);
        }

        public int Add(MessagePatternModel link, int id)
        {
            Trace.Call(link, id);

            if (link == null) {
                throw new ArgumentNullException("link");
            }

            string[] keys = PatternIDs;
            if (keys == null) {
                keys = new string[] {};
            }
            int highestKey = 0;
            int newKey = id;
            if (id == -1) {
                foreach (string key in keys) {
                    int parsedKey = Int32.Parse(key);
                    if (parsedKey > highestKey) {
                        highestKey = parsedKey;
                    }
                }
                newKey = ++highestKey;
            }

            link.ID = newKey;
            link.Save(UserConfig);

            var keyList = new List<string>(keys);
            keyList.Add(link.ID.ToString());
            PatternIDs = keyList.ToArray();
            return newKey;
        }

        public void Set(MessagePatternModel link)
        {
            Trace.Call(link);

            if (link == null) {
                throw new ArgumentNullException("link");
            }

            link.Save(UserConfig);
        }

        public void Remove(int key)
        {
            Trace.Call(key);

            string section = "MessagePatterns/" + key + "/";
            string[] keys = PatternIDs;
            if (keys == null) {
                keys = new string[] {};
            }
            var keyList = new List<string>(keys);
            int idx = keyList.IndexOf(key.ToString());
            if (idx == -1) {
                // key not found
                return;
            }
            keyList.RemoveAt(idx);
            UserConfig.Remove(section);
            PatternIDs = keyList.ToArray();
        }

        public void Save()
        {
            Trace.Call();

            UserConfig.Save();
        }
    }
}
