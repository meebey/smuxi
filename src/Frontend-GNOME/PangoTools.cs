// $Id$
//
// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2010 David Paleino <dapal@debian.org>
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
using System.Text;
using Smuxi.Engine;

namespace Smuxi.Frontend.Gnome
{
    public class PangoTools
    {
        public static string ToMarkup(MessageModel Model)
        {
            /* Pango Markup doesn't support hyperlinks:
             *     (smuxi-frontend-gnome:9824): Gtk-WARNING **: Failed to set
             *     text from markup due to error parsing markup: Unknown tag
             *     'a' on line 1 char 59
             *
             * For this reason, for UrlMessagePartModels, we'll render them as
             * plaintext.
             *
             * Here we loop over the MessageModel to build up a proper Pango
             * Markup.
             *
             * The colour codes/values have been taken from BuildTagTable(), in
             * MessageTextView.cs.
             *
             * Documentation for Pango Markup is located at:
             *    http://library.gnome.org/devel/pango/unstable/PangoMarkupFormat.html
             */
            StringBuilder markup = new StringBuilder ();
            foreach (MessagePartModel msgPart in Model.MessageParts) {
                if (msgPart is UrlMessagePartModel) {
                    UrlMessagePartModel url = (UrlMessagePartModel) msgPart;

                    string str = GLib.Markup.EscapeText(url.Text);
                    str = String.Format("<span color='{0}'><u>{1}</u></span>",
                                        "darkblue",
                                        str);
                    markup.Append(str);
                } else if (msgPart is TextMessagePartModel) {
                    TextMessagePartModel text = (TextMessagePartModel) msgPart;
                    List<string> tags = new List<string>();

                    string str = GLib.Markup.EscapeText(text.Text);
                    if (text.ForegroundColor != TextColor.None) {
                        tags.Add(String.Format("span color='#{0}'",
                                                text.ForegroundColor.HexCode));
                    }
                    // TODO: do contrast checks here like we do in MessageTextView?
                    if (text.Underline) {
                        tags.Add("u");
                    }
                    if (text.Bold) {
                        tags.Add("b");
                    }
                    if (text.Italic) {
                        tags.Add("i");
                    }

                    if (tags.Count > 0) {
                        foreach (string tag in tags) {
                            str = String.Format("{0}{1}{2}",
                                "<"+tag+">", str, "</"+tag.Split(' ')[0]+">");
                        }
                    }

                    markup.Append(str);
                }
            }

            return markup.ToString();
        }
    }
}
