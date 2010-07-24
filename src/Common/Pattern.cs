// Smuxi - Smart MUltipleXed Irc
// 
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
using System.Text.RegularExpressions;

namespace Smuxi.Common
{
    public static class Pattern
    {
        public static bool IsMatch(string input, string pattern)
        {
            if (input == null) {
                throw new ArgumentNullException("input");
            }
            if (pattern == null) {
                throw new ArgumentNullException("pattern");
            }

            // regex matching
            if (pattern.StartsWith("/") &&
                pattern.EndsWith("/")) {
                var regexPattern = pattern.Substring(1, pattern.Length - 2);
                return Regex.IsMatch(input, regexPattern);
            }

            // globbing
            if (pattern.Length == 0 &&
                input.Length == 0) {
                return true;
            }
            if (pattern == "*") {
                return true;
            }
            if (pattern.StartsWith("*") &&
                pattern.EndsWith("*")) {
                string globPattern = pattern.Substring(1, pattern.Length - 2);
                return input.Contains(globPattern);
            }
            if (pattern.StartsWith("*")) {
                string globPattern = pattern.Substring(1);
                return input.EndsWith(globPattern);
            }
            if (pattern.EndsWith("*")) {
                string globPattern = pattern.Substring(0, pattern.Length - 1);
                return input.StartsWith(globPattern);
            }

            // exact matching
            return input == pattern;
        }

        public static bool ContainsPatternCharacters(string input)
        {
            if (input == null) {
                throw new ArgumentNullException("input");
            }
            if (input.Length == 0) {
                return false;
            }

            return input.StartsWith("*") || input.EndsWith("*") ||
                   (input.Length >= 2 &&
                    input.StartsWith("/") && input.EndsWith("/"));
        }
    }
}
