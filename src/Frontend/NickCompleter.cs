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
using Smuxi.Engine;

namespace Smuxi.Frontend
{
    /// <summary>
    /// Automatically completes nicknames (e.g. when the user presses the Tab key).
    /// </summary>
    public abstract class NickCompleter
    {
        public string CompletionChar = ":";

        /// <summary>
        /// Isolates the nickname that should be completed.
        /// </summary>
        /// <returns>The isolated nickname.</returns>
        /// <param name="entryLine">The text currently typed into the input text box.</param>
        /// <param name="cursorPosition">The current location of the cursor in the input text box.</param>
        /// <param name="nickBeginning">Stores where the isolated nickname begins in the entered text.</param>
        /// <param name="appendSpace">Whether to append a space when the nickname is completed.</param>
        /// <param name="leadingAt">Whether the nickname started with a leading @ (which was stripped away).</param>
        protected static string IsolateNickToComplete(string entryLine, int cursorPosition, out int nickBeginning, out bool appendSpace, out bool leadingAt)
        {
            string ret;
            int prev_space = entryLine.Substring(0, cursorPosition).LastIndexOf(' ');
            int next_space = entryLine.IndexOf(' ', cursorPosition);
            appendSpace = false;

            if (prev_space == -1 && next_space == -1) {
                // no spaces (the nick is the only thing)
                nickBeginning = 0;
                appendSpace = true;
                ret = entryLine;
            } else if (prev_space == -1) {
                nickBeginning = 0;
                ret = entryLine.Substring(0, next_space);
            } else if (next_space == -1) {
                nickBeginning = prev_space + 1;
                appendSpace = true;
                ret = entryLine.Substring(nickBeginning);
            } else {
                nickBeginning = prev_space + 1;
                ret = entryLine.Substring(prev_space + 1, next_space - prev_space - 1);
            }

            leadingAt = false;
            if (ret.StartsWith("@")) {
                leadingAt = true;
                ++nickBeginning;
                ret = ret.Substring(1);
            }

            return ret;
        }

        /// <summary>
        /// Returns a list containing only the nicknames matching the given prefix.
        /// </summary>
        /// <returns>
        /// The list of nicknames matching the given prefix.
        /// </returns>
        /// <param name="persons">
        /// List of people to enumerate. The ordering will be taken over verbatim.
        /// </param>
        /// <param name="prefix">Prefix of nicknames to return.</param>
        protected static IList<string> NicksMatchingPrefix(IList<PersonModel> persons, string prefix)
        {
            var ret = new List<string>();
            string lowerPfx = prefix.ToLower();
            foreach (PersonModel person in persons) {
                string nick = person.IdentityName;
                if (nick.ToLower().StartsWith(lowerPfx)) {
                    ret.Add(nick);
                }
            }
            return ret;
        }

        /// <summary>
        /// Performs nickname tab completion on the specified input.
        /// </summary>
        /// <param name="entryLine">The text currently typed into the input text box.</param>
        /// <param name="cursorPosition">
        /// The current location of the cursor in the input text box. Equal to the index of the
        /// character after the current cursor position.
        /// </param>
        /// <param name="currentChatView">
        /// The current chat view. The list of participants is fetched from it; the completer may
        /// also append messages to the chat to provide further information.
        /// </param>
        abstract public void Complete(ref string entryLine, ref int cursorPosition, IChatView currentChatView);
    }
}
