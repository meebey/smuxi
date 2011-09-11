// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2011 Mirco Bauer <meebey@meebey.net>
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

namespace Smuxi.Engine
{
    public class EntrySettings
    {
        public string CommandCharacter { get; set; }
        public string CompletionCharacter { get; set; }
        public bool BashStyleCompletion { get; set; }
        public int CommandHistorySize { get; set; }

        public EntrySettings()
        {
            // internal defaults
            CommandCharacter = "/";
            CompletionCharacter = ":";
            BashStyleCompletion = false;
            CommandHistorySize = 30;
        }

        public void ApplyConfig(UserConfig config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }

            CommandCharacter = (string)
                config["Interface/Entry/CommandCharacter"];
            CompletionCharacter = (string)
                config["Interface/Entry/CompletionCharacter"];
            BashStyleCompletion = (bool)
                config["Interface/Entry/BashStyleCompletion"];
            CommandHistorySize = (int)
                config["Interface/Entry/CommandHistorySize"];
        }
    }
}
