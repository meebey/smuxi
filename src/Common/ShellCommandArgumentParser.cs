//
// ShellCommandArgumentParser.cs
//
// Author:
//       Chris Howie <me@chrishowie.com>
//
// Copyright (c) 2010 Chris Howie
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Smuxi
{
    public sealed class ShellCommandArgumentParser
    {
        public static readonly ShellCommandArgumentParser Instance = new ShellCommandArgumentParser();

        private ShellCommandArgumentParser() { }

        #region ICommandArgumentParser Members

        public IList<string> ParseArguments(string arguments, int maxArguments)
        {
            return DoParse(arguments, maxArguments).ToList();
        }

        private IEnumerable<string> DoParse(string arguments, int maxArguments)
        {
            StringBuilder argBuilder = new StringBuilder();
            char quote = '\0';
            bool escaped = false;
            bool sawQuote = false;

            bool hadMaxArgumentsSpace = false;

            maxArguments--;

            int argumentsSeen = 0;

            foreach (char c in arguments)
            {
                if (escaped)
                {
                    switch (c)
                    {
                        case 'n':
                            case 'r':
                            argBuilder.Append('\n');
                            break;

                            case '\'':
                            case '"':
                            argBuilder.Append(c);
                            break;

                            case '\\':
                            argBuilder.Append('\\');
                            break;

                            case ' ':
                            argBuilder.Append(' ');
                            break;

                            default:
                            argBuilder.Append('\\');
                            argBuilder.Append(c);
                            break;
                    }

                    escaped = false;
                }
                else if (c == ' ')
                {
                    if (quote != '\0')
                    {
                        argBuilder.Append(' ');
                    }
                    else if (argumentsSeen >= maxArguments)
                    {
                        if (!hadMaxArgumentsSpace)
                        {
                            argBuilder.Append(' ');
                            hadMaxArgumentsSpace = true;
                        }
                    }
                    else if (argBuilder.Length != 0 || sawQuote)
                    {
                        yield return argBuilder.ToString();
                        argBuilder.Length = 0;
                        sawQuote = false;

                        argumentsSeen++;
                    }
                }
                else
                {
                    hadMaxArgumentsSpace = false;

                    if (quote != '\0' && c == quote)
                    {
                        quote = '\0';
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '\'' || c == '"')
                    {
                        if (quote != '\0') {
                            argBuilder.Append(c);
                        } else {
                            quote = c;
                            sawQuote = true;
                        }
                    }
                    else
                    {
                        argBuilder.Append(c);
                    }
                }
            }

            if (quote != '\0')
                throw new InvalidDataException("Unable to parse arguments: unterminated " + quote);

            if (argBuilder.Length != 0 || sawQuote)
            {
                if (hadMaxArgumentsSpace)
                    argBuilder.Length--;

                yield return argBuilder.ToString();
            }
        }

        #endregion
    }
}