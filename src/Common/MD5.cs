// This file is part of Smuxi and is licensed under the terms of MIT/X11
//
// Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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
using System.Text;
using System.Security.Cryptography;

namespace Smuxi.Common
{
    public class MD5
    {
        public static string FromString(string cleartext)
        {
            MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider();
            byte[] md5bytes = csp.ComputeHash(Encoding.UTF8.GetBytes(cleartext));
            StringBuilder md5text = new StringBuilder();
            foreach (byte md5byte in md5bytes) {
                md5text.Append(md5byte.ToString("x2").ToLower());
            }
            return md5text.ToString();
        }
    }
}
