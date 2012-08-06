// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2010 David Paleino <dapal@debian.org>
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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
        public static string ToMarkup(MessageModel msg)
        {
            return ToMarkup(msg, null);
        }

        public static string ToMarkup(MessageModel msg, Gdk.Color? bgColor)
        {
            if (msg == null) {
                return String.Empty;
            }

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
            foreach (MessagePartModel msgPart in msg.MessageParts) {
                if (msgPart is UrlMessagePartModel) {
                    UrlMessagePartModel url = (UrlMessagePartModel) msgPart;

                    string str = GLib.Markup.EscapeText(url.Url);
                    
                    Gdk.Color gdkColor = Gdk.Color.Zero;
                    Gdk.Color.Parse("darkblue", ref gdkColor);
                    TextColor urlColor = ColorConverter.GetTextColor(gdkColor);
                    if (bgColor != null) {
                        // we have a bg color so lets try to get a url color
                        // with a good contrast
                        urlColor = TextColorTools.GetBestTextColor(
                            urlColor, ColorConverter.GetTextColor(bgColor.Value)
                        );
                    }

                    str = String.Format("<span color='#{0}'><u>{1}</u></span>",
                                        urlColor.HexCode,
                                        str);
                    markup.Append(str);
                } else if (msgPart is TextMessagePartModel) {
                    TextMessagePartModel text = (TextMessagePartModel) msgPart;
                    List<string> tags = new List<string>();

                    string str = GLib.Markup.EscapeText(text.Text);
                    if (text.ForegroundColor != TextColor.None) {
                        TextColor fgColor;
                        if (bgColor == null) {
                            fgColor = text.ForegroundColor;
                        } else {
                            var bgTextColor = ColorConverter.GetTextColor(
                                bgColor.Value
                            );
                            fgColor = TextColorTools.GetBestTextColor(
                                text.ForegroundColor, bgTextColor
                            );
                        }
                        tags.Add(String.Format("span color='#{0}'",
                                                fgColor.HexCode));
                    }
                    
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
                        tags.Reverse();
                        foreach (string tag in tags) {
                            string endTag;
                            if (tag.Contains(" ")) {
                                // tag contains attributes, only get tag name
                                endTag = tag.Split(' ')[0];
                            } else {
                                endTag = tag;
                            }
                            str = String.Format("<{0}>{1}</{2}>",
                                                tag, str, endTag);
                        }
                    }

                    markup.Append(str);
                }
            }

            return markup.ToString();
        }
    }
}
