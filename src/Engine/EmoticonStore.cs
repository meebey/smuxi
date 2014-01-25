// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014 jamesaxl
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

namespace Smuxi.Engine
{
    public class EmoticonStore : Dictionary<string, string>
    {
        public EmoticonStore()
        {
            this.Add("o:)", "angel");
            this.Add(">:o", "angry");
            this.Add(":/", "confused");
            this.Add("B|", "cool");
            this.Add(":'(", "crying");
            this.Add("3:)", "devilish");
            this.Add(">:(", "embarrassed");
            this.Add(":*", "kiss");
            this.Add("^_^", "laugh");
            this.Add(":8)", "monkey");
            this.Add(":|", "plain");
            this.Add(":P", "raspberry");
            this.Add(":(", "sad");
            this.Add(":x", "shutmouth");
            this.Add(":$", "sick");
            this.Add(":D", "smile-big");
            this.Add(":)", "smile");
            this.Add(":?", "smirk");
            this.Add(":o", "surprised");
            this.Add("-_-", "tired");
            this.Add(":<", "uncertain");
            this.Add(";)", "wink");
            this.Add(":<<", "worried");
            this.Add(":O", "yawn");
        }

        public string GetEmoticonFile(string emoticon)
        {
            return "face-" + this[emoticon] + "-symbolic";
        }
    }
}

