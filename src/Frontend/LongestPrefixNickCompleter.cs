/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2013 Mirco Bauer <meebey@meebey.net>
 * Copyright (c) 2013 Ondra Hosek <ondra.hosek@gmail.com>
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
using System.Linq;
using System.Collections.Generic;
using Smuxi.Engine;

namespace Smuxi.Frontend
{
    /// <summary>
    /// Longest Common Prefix (bash-style) nick completer.
    /// </summary>
    /// <description>
    /// When triggered, the nickname list is searched for all matching nicknames.
    /// When only one nickname is found, it is fully completed. When more than one
    /// nickname is found, the longest prefix common to all these nicknames is
    /// completed, and a list of the nicknames is output into the chat view.
    /// The user may then input additional characters to narrow down the search,
    /// then trigger completion anew.
    /// </description>
    public class LongestPrefixNickCompleter : NickCompleter
    {
        protected static string LongestCommonPrefix(IList<string> nicks)
        {
            string ret = null;

            if (nicks.Count == 0) {
                return ret;
            }

            foreach (string nick in nicks) {
                if (ret == null) {
                    ret = nick;
                } else {
                    while (!nick.StartsWith(ret, StringComparison.OrdinalIgnoreCase)) {
                        // cut off one character at the end
                        ret = ret.Substring(0, ret.Length - 1);
                    }
                }
            }

            return ret;
        }

        public override void Complete(ref string entryLine, ref int cursorPosition, IChatView currentChatView)
        {
            // isolate the nick to complete
            int matchPosition;
            bool appendSpace, leadingAt;
            string matchMe = IsolateNickToComplete(entryLine, cursorPosition, out matchPosition, out appendSpace, out leadingAt);

            bool appendCompletionChar = (matchPosition == 0);
            int additionalSteps = 0;

            // find the matching nicknames
            var nicks = NicksMatchingPrefix(currentChatView.Participants, matchMe);

            if (nicks.Count == 0) {
                // no matches; do nothing
                return;
            } else if (nicks.Count == 1) {
                // bingo!
                string nick = nicks [0];

                // suppress the completion character if we had an @
                if (leadingAt) {
                    appendCompletionChar = false;
                }

                // find the beginning and end of the string
                string prefix = entryLine.Substring(0, matchPosition);
                string suffix = entryLine.Substring(matchPosition + matchMe.Length);

                // append the completion character and a space, if requested
                if (appendSpace) {
                    suffix = ' ' + suffix;
                    ++additionalSteps;
                }
                if (appendCompletionChar) {
                    suffix = CompletionChar + suffix;
                    ++additionalSteps;
                }

                // assemble the line and move the cursor
                entryLine = prefix + nick + suffix;
                cursorPosition = matchPosition + nick.Length + additionalSteps;
            } else {
                // find the longest common prefix
                string lcp = LongestCommonPrefix(nicks);

                // assemble nickname string
                string nickString = string.Join(" ", nicks.ToArray());

                // output the matched prefixes
                currentChatView.AddMessage(
                    new MessageModel(String.Format("-!- {0}", nickString))
                );

                // extend to the longest match
                string prefix = entryLine.Substring(0, matchPosition);
                string suffix = entryLine.Substring(matchPosition + matchMe.Length);

                // assemble the line and move the cursor
                entryLine = prefix + lcp + suffix;
                cursorPosition = matchPosition + lcp.Length;
            }
        }
    }
}

