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
    public static class EmoticonStore
    {
        static  Dictionary<string, string> emoticonStore = new Dictionary<string, string>();

        public static Dictionary<string, string> InitStore()
        {
            emoticonStore.Add("o:)", "angel");
            emoticonStore.Add(">:o", "angry");
            emoticonStore.Add(":/", "confused");
            emoticonStore.Add("B|", "cool");
            emoticonStore.Add(":'(", "crying");
            emoticonStore.Add("3:)", "devilish");
            emoticonStore.Add(">:(", "embarrassed");
            emoticonStore.Add(":*", "kiss");
            emoticonStore.Add("^_^", "laugh");
            emoticonStore.Add(":8)", "monkey");
            emoticonStore.Add(":|", "plain");
            emoticonStore.Add(":P", "raspberry");
            emoticonStore.Add(":(", "sad");
            emoticonStore.Add(":x", "shutmouth");
            emoticonStore.Add(":$", "sick");
            emoticonStore.Add(":D", "smile-big");
            emoticonStore.Add(":)", "smile");
            emoticonStore.Add(":?", "smirk");
            emoticonStore.Add(":o", "surprised");
            emoticonStore.Add("-_-", "tired");
            emoticonStore.Add(":<", "uncertain");
            emoticonStore.Add(";)", "wink");
            emoticonStore.Add(":<<", "worried");
            emoticonStore.Add(":O", "yawn");
            return emoticonStore;
        }

        public static string GetEmoticonFile(string emoticon)
        {
            return "face-" + emoticonStore[emoticon] + "-symbolic";
        }

        public static void ClearStore()
        {
            emoticonStore.Clear();
        }
    }
}
