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

namespace Smuxi.Frontend.Gnome
{
    public class EmoticonStore : Dictionary<string, string>
    {
        public EmoticonStore()
        {
            Add("o:)", "angel");
            Add(">:o", "angry");
            Add(":/", "confused");
            Add("B|", "cool");
            Add(":'(", "crying");
            Add("3:)", "devilish");
            Add(">:(", "embarrassed");
            Add(":*", "kiss");
            Add("^_^", "laugh");
            Add(":8)", "monkey");
            Add(":|", "plain");
            Add(":P", "raspberry");
            Add(":p", "raspberry");
            Add(":(", "sad");
            Add(":x", "shutmouth");
            Add(":$", "sick");
            Add(":D", "smile-big");
            Add(":)", "smile");
            Add(":?", "smirk");
            Add(":o", "surprised");
            Add("-_-", "tired");
            Add(":<", "uncertain");
            Add(";)", "wink");
            Add(":<<", "worried");
            Add(":O", "yawn");
        }

        public Gdk.Pixbuf GetEmoticonImage(string emoticonName)
        {
            var emoticonImage = Frontend.LoadIcon("face- " + emoticonName + "-symbolic", 16, "face-" + emoticonName + "-symbolic.png");
            return emoticonImage;
        }

        public Gdk.Pixbuf GetEmoticonSymbol(string emoticonName)
        {
            var emoticonImage = Frontend.LoadIcon("face- " + emoticonName , 16, "face-" + emoticonName + ".png");
            return emoticonImage;
        }

        public bool TryGetImage(string emoticonSymbol)
        {
            if (this.ContainsKey(emoticonSymbol))
                return true;
            return false;
        }
    }
}
