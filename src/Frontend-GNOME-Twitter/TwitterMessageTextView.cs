// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2015 Andrés G. Aragoneses <knocte@gmail.com>
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

using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class TwitterMessageTextView : MessageTextView
    {
        protected override void InsertTimeStamp(Gtk.TextBuffer buffer, ref Gtk.TextIter iter,
                                                string timestamp, MessageModel msg)
        {
            if (String.IsNullOrWhiteSpace(msg.ID)) {
                buffer.Insert(ref iter, timestamp);
            } else {
                var uri = new Uri(String.Format("https://twitter.com/{0}/status/{1}", msg.GetNick(), msg.ID));

                var tags = new List<Gtk.TextTag>();
                // link URI tag
                var linkTag = new LinkTag(uri);
                linkTag.TextEvent += OnLinkTagTextEvent;
                _MessageTextTagTable.Add(linkTag);
                tags.Add(linkTag);

                // link style tag
                tags.Add(LinkTag);

                buffer.InsertWithTags(ref iter, timestamp, tags.ToArray());
            }

            buffer.Insert(ref iter, " ");
        }
    }
}

