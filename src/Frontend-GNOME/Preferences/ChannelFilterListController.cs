/*
 * $Id: PreferencesDialog.cs 142 2007-01-02 22:19:08Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/PreferencesDialog.cs $
 * $Rev: 142 $
 * $Author: meebey $
 * $Date: 2007-01-02 23:19:08 +0100 (Tue, 02 Jan 2007) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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
using Smuxi;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class ChannelFiltersController
    {
        private UserConfig _UserConfig;
        
        public ChannelFiltersController(UserConfig userConfig)
        {
            _UserConfig = userConfig;
        }
        
        public IList<ChannelFilterModel> GetFilterList()
        {
            string[] channels = (string[]) _UserConfig["Filters/Channel/Patterns"];
            IList<ChannelFilterModel> filters = new List<ChannelFilterModel>();
            foreach (string channel in channels) {
                filters.Add(GetFilter(channel));
            }
            return filters;
        }
        
        public ChannelFilterModel GetFilter(string pattern)
        {
            string prefix = "Filters/Channel/" + pattern + "/";
            ChannelFilterModel filter = new ChannelFilterModel();
            filter.Pattern     = (string) _UserConfig[prefix + "Pattern"];
            filter.FilterJoins = (bool) _UserConfig[prefix + "FilterJoins"];
            filter.FilterParts = (bool) _UserConfig[prefix + "FilterParts"];
            filter.FilterQuits = (bool) _UserConfig[prefix + "FilterQuits"];
            return filter;
        }
        
        public void AddFilter(ChannelFilterModel filter)
        {
            string prefix = "Filters/Channel/" + filter.Pattern + "/";
            _UserConfig[prefix + "Pattern"] = filter.Pattern;
            _UserConfig[prefix + "FilterJoins"] = filter.FilterJoins;
            _UserConfig[prefix + "FilterParts"] = filter.FilterParts;
            _UserConfig[prefix + "FilterQuits"] = filter.FilterQuits;
            
            string[] channels = (string[]) _UserConfig["Filters/Channel/Patterns"];
            List<string> channelList = new List<string>(channels);
            channelList.Add(filter.Pattern);
            _UserConfig["Filters/Channel/Patterns"] = channelList.ToArray();
        }

        public void RemoveFilter(string pattern)
        {
        }
    }
}
