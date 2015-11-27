/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006, 2010, 2015 Mirco Bauer <meebey@meebey.net>
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
    public class FilterListController
    {
        UserConfig f_UserConfig;

        public FilterListController(UserConfig userConfig)
        {
            if (userConfig == null) {
                throw new ArgumentNullException("userConfig");
            }

            f_UserConfig = userConfig;
        }

        public IDictionary<int, FilterModel> GetFilterList()
        {
            string[] filterKeys = (string[]) f_UserConfig["Filters/Filters"];
            var filters = new Dictionary<int, FilterModel>();
            if (filterKeys == null) {
                return filters;
            }
            foreach (string filterKey in filterKeys) {
                int key = Int32.Parse(filterKey);
                var filter = GetFilter(key);
                if (filter == null) {
                    continue;
                }
                filters.Add(key, filter);
            }
            return filters;
        }

        public FilterModel GetFilter(int key)
        {
            Trace.Call(key);

            string prefix = "Filters/" + key + "/";
            if (f_UserConfig[prefix + "MessagePattern"] == null) {
                // filter does not exist
                return null;
            }
            FilterModel filter = new FilterModel();
            filter.Protocol = (string) f_UserConfig[prefix + "Protocol"];
            filter.NetworkID = (string) f_UserConfig[prefix + "NetworkID"];
            var chatType = (string) f_UserConfig[prefix + "ChatType"];
            if (!String.IsNullOrEmpty(chatType)) {
                filter.ChatType = (ChatType) Enum.Parse(
                    typeof(ChatType),
                    chatType
                );
            }
            filter.ChatID = (string) f_UserConfig[prefix + "ChatID"];
            var msgType = (string) f_UserConfig[prefix + "MessageType"];
            if (!String.IsNullOrEmpty(msgType)) {
                filter.MessageType = (MessageType) Enum.Parse(
                    typeof(MessageType),
                    msgType
                );
            }
            filter.MessagePattern = (string) f_UserConfig[prefix + "MessagePattern"];
            return filter;
        }

        public int AddFilter(FilterModel filter)
        {
            Trace.Call(filter);

            if (filter == null) {
                throw new ArgumentNullException("filter");
            }

            string[] filterKeys = (string[]) f_UserConfig["Filters/Filters"];
            if (filterKeys == null) {
                filterKeys = new string[] {};
            }
            int highestKey = 0;
            foreach (string filterKey in filterKeys) {
                int key = Int32.Parse(filterKey);
                if (key > highestKey) {
                    highestKey = key;
                }
            }
            int newKey = ++highestKey;

            string prefix = "Filters/" + newKey + "/";
            f_UserConfig[prefix + "Protocol"] = filter.Protocol;
            f_UserConfig[prefix + "NetworkID"] = filter.NetworkID;
            if (filter.ChatType == null) {
                f_UserConfig[prefix + "ChatType"] = String.Empty;
            } else {
                f_UserConfig[prefix + "ChatType"] = filter.ChatType.ToString();
            }
            f_UserConfig[prefix + "ChatID"] = filter.ChatID;
            if (filter.MessageType == null) {
                f_UserConfig[prefix + "MessageType"] = String.Empty;
            } else {
                f_UserConfig[prefix + "MessageType"] = filter.MessageType.ToString();
            }
            f_UserConfig[prefix + "MessagePattern"] = filter.MessagePattern;

            List<string> filterKeyList = new List<string>(filterKeys);
            filterKeyList.Add(newKey.ToString());
            f_UserConfig["Filters/Filters"] = filterKeyList.ToArray();
            return newKey;
        }

        public void SetFilter(int key, FilterModel filter)
        {
            Trace.Call(key, filter);

            if (filter == null) {
                throw new ArgumentNullException("filter");
            }

            string prefix = "Filters/" + key + "/";
            f_UserConfig[prefix + "Protocol"] = filter.Protocol;
            f_UserConfig[prefix + "NetworkID"] = filter.NetworkID;
            if (filter.ChatType == null) {
                f_UserConfig[prefix + "ChatType"] = String.Empty;
            } else {
                f_UserConfig[prefix + "ChatType"] = filter.ChatType.ToString();
            }
            f_UserConfig[prefix + "ChatID"] = filter.ChatID;
            if (filter.MessageType == null) {
                f_UserConfig[prefix + "MessageType"] = String.Empty;
            } else {
                f_UserConfig[prefix + "MessageType"] = filter.MessageType.ToString();
            }
            f_UserConfig[prefix + "MessagePattern"] = filter.MessagePattern;
        }

        public void RemoveFilter(int key)
        {
            Trace.Call(key);

            string filterSection = "Filters/" + key + "/";
            string[] filterKeys = (string[]) f_UserConfig["Filters/Filters"];
            if (filterKeys == null) {
                filterKeys = new string[] {};
            }
            List<string> filterKeyList = new List<string>(filterKeys);
            int idx = filterKeyList.IndexOf(key.ToString());
            if (idx == -1) {
                // key not found
                return;
            }
            filterKeyList.RemoveAt(idx);
            f_UserConfig.Remove(filterSection);
            f_UserConfig["Filters/Filters"] = filterKeyList.ToArray();
        }

        public void Save()
        {
            Trace.Call();

            f_UserConfig.Save();
        }
    }
}
