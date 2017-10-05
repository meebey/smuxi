// This file is part of Smuxi and is licensed under the terms of MIT/X11
//
// Copyright (c) 2015 Carlos Martín Nieto
// Copyright (c) 2017 Mirco Bauer <meebey@meebey.net>
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

namespace Smuxi.Common
{
    public partial class Emojione
    {
        readonly static string BaseUri = "http://cdnjs.cloudflare.com/ajax/libs/emojione/2.2.7/assets/png/";

        public static Dictionary<string, string> ShortnameToUnicodeMap {
            get {
                return map;
            }
        }

        public static string ShortnameToUnicode(string shortName)
        {
            string val;
            if (map.TryGetValue(shortName, out val)) {
                return val;
            }

            return null;
        }

        public static string  UnicodeToUrl(string unicode)
        {
            // the filename has to be lower case, otherwise it will HTTP 404
            unicode = unicode.ToLowerInvariant();
            return String.Format("{0}{1}.png", BaseUri, unicode);
        }

        public static string ShortnameToUri(string shortName)
        {
            var unicode = ShortnameToUnicode(shortName);
            if (String.IsNullOrEmpty(unicode)) {
                return null;
            }

            return UnicodeToUrl(unicode);
        }
    }
}

