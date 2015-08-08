// This file is part of Smuxi and is licensed under the terms of MIT/X11
//
// Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
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
using Mono.Unix;

namespace Smuxi.Common
{
    public class LibraryCatalog
    {
        private static Object _SyncRoot = new Object();
        private static string _TextDomain;
        private static string _LocaleDirectory;
        private static bool   _IsInitialized;
        
        private LibraryCatalog()
        {
        }
        
        public static void Init(string textDomain, string localeDirectory)
        {
            _TextDomain = textDomain;
            _LocaleDirectory = localeDirectory;
            Catalog.Init(textDomain, localeDirectory);
            _IsInitialized = true;
        }
        
        public static string GetString(string s, string textDomain)
        {
            lock (_SyncRoot) {
                if (_IsInitialized) {
                    Catalog.Init(textDomain, _LocaleDirectory);
                    string msg = Catalog.GetString(s);
                    Catalog.Init(_TextDomain, _LocaleDirectory);
                    return msg;
                }
                return s;
            }
        }
        
        public static string GetString(string s)
        {
            // TODO: use text-domain registry for each calling assembly or class
            throw new NotImplementedException();
        }
    }
}
