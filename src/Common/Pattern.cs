// This file is part of Smuxi and is licensed under the terms of MIT/X11
// 
// Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
