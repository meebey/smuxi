// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2011, 2013 Mirco Bauer
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Stfl
{
    public class TextView : Widget
    {
#if LOG4NET
        static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        public string OffsetVariableName { get; set; }
        public bool AutoLineWrap { get; set; }
        List<string> Lines { get; set; }
        int WrappedLineCount { get; set; }
        static Regex StyleTagRegex = new Regex("<([^>]+)>");

        public int Offset {
            get {
                var offset = Form[OffsetVariableName];
                if (String.IsNullOrEmpty(offset)) {
                    return -1;
                }
                return Int32.Parse(offset);
            }
            set {
                var minOffset = OffsetStart;
                var maxOffset = OffsetEnd;
                if (value == -1) {
                    value = maxOffset;
                 } else if (value > maxOffset) {
                    value = maxOffset;
                } else if (value < minOffset) {
                    value = minOffset;
                }
                Form[OffsetVariableName] = value.ToString();
            }
        }

        public int OffsetStart {
            get {
                return 0;
            }
        }

        public int OffsetEnd {
            get {
                int heigth = Heigth;
                if (WrappedLineCount <= heigth) {
                    return 0;
                }
                return WrappedLineCount - heigth;
            }
        }

        public TextView(Form form, string widgetId) :
                   base(form, widgetId)
        {
            Lines = new List<string>();
            Form.EventReceived += OnEventReceived;
        }

        public void AppendWrappedLine(string line)
        {
            WrappedLineCount++;
            Form.Modify(
                WidgetName,
                "append",
                String.Format("{{listitem text:{0}}}",
                              StflApi.stfl_quote(line))
            );
        }

        public void AppendWrappedLines(IEnumerable<string> lines)
        {
            foreach (var line in lines) {
                AppendWrappedLine(line);
            }
        }

        public void AppendLine(string line)
        {
            var width = Width;
            if (!AutoLineWrap || width <= 0) {
                // we don't know our width for whatever reason thus we can't
                // apply any line wrapping
                Lines.Add(line);
                AppendWrappedLine(line);
                return;
            }

            Lines.Add(line);
            AppendWrappedLines(WrapLine(line, width));
        }

        public void AppendLines(IEnumerable<string> lines)
        {
            var width = Width;
            if (!AutoLineWrap || width <= 0) {
                // we don't know our width for whatever reason thus we can't
                // apply any line wrapping
                Lines.AddRange(lines);
                AppendWrappedLines(lines);
                return;
            }

            Lines.AddRange(lines);
            var wrappedLines = new List<string>(lines);
            foreach (var line in lines) {
                wrappedLines.AddRange(WrapLine(line, width));
            }
            AppendWrappedLines(wrappedLines);
        }

        public void ScrollUp()
        {
            Scroll(-0.9);
        }

        public void ScrollDown()
        {
            Scroll(0.9);
        }

        protected void Scroll(double scrollFactor)
        {
            int currentOffset = Offset;
            try {
                int newOffset = (int) (currentOffset + (Heigth * scrollFactor));
                if (newOffset < 0) {
                    newOffset = 0;
                } else if (newOffset > OffsetEnd) {
                    newOffset = OffsetEnd;
                }
                Offset = newOffset;
            } catch (FormatException ex) {
#if LOG4NET
                Logger.ErrorFormat(
                    "Scroll({0}): FormatException, ignoring...", ex
                );
#endif
            }
        }

        public void ScrollToStart()
        {
            Offset = OffsetStart;
        }

        public void ScrollToEnd()
        {
            Offset = OffsetEnd;
        }

        public void Clear()
        {
            Lines.Clear();
            WrappedLineCount = 0;
            Form.Modify(WidgetName, "replace_inner", "{list}");
            ScrollToStart();
        }

        /// <summary>
        /// Splits a line into characters, keeping style tags intact and
        /// attached to the character following them, and not breaking apart
        /// escapes of <c>&lt;</c>.
        /// </summary>
        private static IList<string> SplitStyledLineIntoCharacters(string line)
        {
            var chars = new List<string>();
            string assembleStyle = null;
            bool tagging = false;

            for (int i = 0; i < line.Length; ++i) {
                char c = line [i];
                if (c == '<') {
                    if (i < line.Length - 1 && line [i+1] == '>') {
                        // this is <> which is an escape of <
                        if (assembleStyle != null) {
                            chars.Add('<' + assembleStyle + "><>");
                        } else {
                            chars.Add("<>");
                        }
                        // no style anymore
                        assembleStyle = null;
                        // skip the > too
                        ++i;
                    } else {
                        // style begins
                        assembleStyle = String.Empty;
                        tagging = true;
                    }
                } else if (c == '>') {
                    // style ended
                    tagging = false;
                } else if (tagging) {
                    // add to style
                    assembleStyle += c;
                } else {
                    // normal character
                    if (assembleStyle != null) {
                        // we have a style too
                        chars.Add('<' + assembleStyle + '>' + c);
                    } else {
                        chars.Add(c.ToString());
                    }
                    // no style anymore
                    assembleStyle = null;
                }
            }

            return chars;
        }

        /// <summary>
        /// Returns the length of the given line in characters that will
        /// actually be displayed.
        /// </summary>
        private static int LengthWithoutStyle(string line)
        {
            var untaggedString = StyleTagRegex.Replace(line, "");
            var unescapedString = untaggedString.Replace("<>", "<");
            return unescapedString.Length;
        }

        public static List<string> WrapLine(string line, int wrapWidth)
        {
            if (line == null) {
                throw new ArgumentNullException("line");
            }
            if (wrapWidth <= 0) {
                throw new ArgumentException("Wrap width must bigger than 0",
                                            "wrapWidth");
            }

            // split the line on spaces
            IList<string> splitOnSpaces = line.Split(' ').ToList();
            var wrappedLine = new List<string>();
            var freshestStyle = "";

            // as long as there is anything left to wrap
            while (splitOnSpaces.Count > 0) {
                var joinedUp = splitOnSpaces [0];
                int currentLengthWithoutStyle = LengthWithoutStyle(joinedUp);

                // take one word
                if (currentLengthWithoutStyle > wrapWidth) {
                    // uh-oh, cannot grab first word whole; must split it
                    var chars = SplitStyledLineIntoCharacters(joinedUp);
                    joinedUp = String.Join("", chars.Take(wrapWidth).ToArray());
                    currentLengthWithoutStyle = wrapWidth;

                    // process the remaining characters next time
                    var rest = splitOnSpaces [0].Substring(joinedUp.Length);
                    splitOnSpaces.RemoveAt(0);
                    splitOnSpaces.Insert(0, rest);
                } else {
                    // that worked
                    splitOnSpaces.RemoveAt(0);

                    // try taking more words
                    var joinedUpBuilder = new StringBuilder(joinedUp, wrapWidth*2);
                    while (splitOnSpaces.Count > 0) {
                        // + 1 accounts for the joining space
                        var newLengthWithoutStyle = currentLengthWithoutStyle + 1 + LengthWithoutStyle(splitOnSpaces [0]);
                        if (newLengthWithoutStyle > wrapWidth) {
                            // that won't work anymore
                            break;
                        }

                        joinedUpBuilder.Append(' ');
                        joinedUpBuilder.Append(splitOnSpaces [0]);
                        currentLengthWithoutStyle = newLengthWithoutStyle;
                        splitOnSpaces.RemoveAt(0);
                    }
                    joinedUp = joinedUpBuilder.ToString();
                }

                // prepend the currently freshest style unless the line starts with a style
                if (!joinedUp.StartsWith("<")) {
                    joinedUp = freshestStyle + joinedUp;
                }

                // find out the now-freshest style
                var styleTags = StyleTagRegex.Matches(joinedUp);
                if (styleTags.Count > 0) {
                    var lastTagName = styleTags[styleTags.Count-1].Groups[1].Value;
                    if (lastTagName.IndexOf('/') != -1) {
                        // closing tag -- no more style
                        freshestStyle = "";
                    } else {
                        // we have a new style
                        freshestStyle = '<' + lastTagName + '>';
                        // make sure to terminate our string
                        joinedUp += "</>";
                    }
                }

                // add the joined-up, style-terminated line to the list
                wrappedLine.Add(joinedUp);
            }

            return wrappedLine;
        }

        void Resize()
        {
            var width = Width;
            if (!AutoLineWrap || width <= 0) {
                // nothing to do
                return;
            }

            var estimatedLines = Math.Max(WrappedLineCount, Lines.Count);
            // see items.AppendFormat() below
            var lineStyleOverhead = 18;
            var listStyleOverhead = 6;
            var estimatedLength = listStyleOverhead +
                (estimatedLines * (width + lineStyleOverhead));
            estimatedLength = (int) (estimatedLength * 1.2);

            // re-wrap all lines and re-apply offset
            WrappedLineCount = 0;
            var offset = Offset;
            var items = new StringBuilder("{list", estimatedLength);
            foreach (var line in Lines) {
                foreach (var wrappedLine in WrapLine(line, width)) {
                    WrappedLineCount++;
                    items.AppendFormat("{{listitem text:{0}}}",
                                       StflApi.stfl_quote(wrappedLine));
                }
            }
            items.Append("}");
            Form.Modify(WidgetName, "replace_inner", items.ToString());
            Offset = offset;
        }

        void OnEventReceived(object sender, EventReceivedEventArgs e)
        {
            if (e.Event == "RESIZE") {
                DateTime start, stop;
                start = DateTime.UtcNow;
                Resize();
                stop = DateTime.UtcNow;
#if LOG4NET
                Logger.DebugFormat(
                    "OnEventReceived(): Resize() took: {0:0.00} ms " +
                    "lines: {1} wrapped lines: {2} width: {3}",
                    (stop - start).TotalMilliseconds,
                    Lines.Count,
                    WrappedLineCount,
                    Width
                );
#endif
            }
        }
    }
}
