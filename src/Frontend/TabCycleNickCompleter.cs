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
using System.Collections.Generic;

namespace Smuxi.Frontend
{
    /// <summary>
    /// Tab Cycle (irssi-style) nick completer.
    /// </summary>
    /// <description>
    /// When triggered, the first nickname matching the input characters is
    /// completed. If triggered again at the same position, the match is
    /// replaced by the next nickname.
    /// </description>
    public class TabCycleNickCompleter : NickCompleter
    {
        IList<string> PreviousNicks { get; set; }
        int PreviousNickIndex { get; set; }
        int PreviousMatchPos { get; set; }
        int PreviousMatchLength { get; set; }
        int PreviousMatchCursorOffset { get; set; } // offset from match pos + match len
        IChatView PreviousChatView { get; set; }

        public TabCycleNickCompleter()
        {
            PreviousNicks = null;
            PreviousNickIndex = -1;
            PreviousMatchPos = -1;
            PreviousMatchLength = -1;
            PreviousMatchCursorOffset = 0;
            PreviousChatView = null;
        }

        public override void Complete(ref string entryLine, ref int cursorPosition, IChatView currentChatView)
        {
            // isolate the nick to complete
            int matchPosition;
            bool appendSpace, leadingAt;
            string matchMe = IsolateNickToComplete(entryLine, cursorPosition, out matchPosition, out appendSpace, out leadingAt);

            int rematchCursorPosition = PreviousMatchPos + PreviousMatchLength + PreviousMatchCursorOffset;
            if (PreviousNickIndex != -1 && currentChatView == PreviousChatView && cursorPosition == rematchCursorPosition) {
                // re-match
                PreviousNickIndex = (PreviousNickIndex + 1) % PreviousNicks.Count;

                string nick = PreviousNicks [PreviousNickIndex];
                string prefix = entryLine.Substring(0, PreviousMatchPos);
                string suffix = entryLine.Substring(PreviousMatchPos + PreviousMatchLength);

                PreviousMatchLength = nick.Length;
                entryLine = prefix + nick + suffix;
                cursorPosition = PreviousMatchPos + PreviousMatchLength + PreviousMatchCursorOffset;

                return;
            }

            // don't re-match even if the user moves the cursor back to the "correct" position
            PreviousNickIndex = -1;

            // don't complete empty strings
            if (matchMe.Length == 0) {
                return;
            }

            bool appendCompletionChar = (matchPosition == 0);
            int additionalSteps = 0;

            // find the matching nicknames
            IList<string> nicks = NicksMatchingPrefix(currentChatView.Participants, matchMe);

            if (nicks.Count == 0) {
                // no matches; do nothing
                return;
            } else {
                // bingo!
                string nick = nicks [0];

                // store the new values for the next completion
                PreviousNicks = nicks;
                PreviousNickIndex = 0;
                PreviousMatchPos = matchPosition;
                PreviousMatchLength = nick.Length;
                PreviousChatView = currentChatView;

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
                PreviousMatchCursorOffset = additionalSteps;
            }
        }
    }
}

