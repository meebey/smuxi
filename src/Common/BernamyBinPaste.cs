// This file is part of Smuxi and is licensed under the terms of MIT/X11
//
// Copyright (c) 2015 jamesaxl
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
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace Smuxi.Common
{
    public class BernamyBinPaste
    {
        private readonly WebClient Session = new WebClient();

        private IWebProxy Proxy { get; set;}
        public string GetUrl { get; private set; }

        public BernamyBinPaste()
        {
            Session.Proxy = Proxy;
        }

        public void DebianPast(string content, string nick , string language, string expiry)
        {
            if (String.IsNullOrEmpty(content))
                throw new ArgumentNullException("Parameter cannot be null or empty", "content");
            if (String.IsNullOrEmpty (nick))
                nick = "Anonymous";
            if (String.IsNullOrEmpty (language))
                language = "-1";
            if (String.IsNullOrEmpty(expiry))
                expiry = "3600";

            var servicePoint = ServicePointManager.FindServicePoint("http://paste.debian.net/./", Session.Proxy);
            servicePoint.Expect100Continue = false;

            var values = new NameValueCollection();
            values["code"] = content;
            values["poster"] = nick;
            values["lang"] = language;
            values["expire"] = expiry;
            Session.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            var message = Session.UploadValues("http://paste.debian.net/./", "POST", values);
            var url = ASCIIEncoding.Default.GetString(message);
            var match = Regex.Match(url, @"<a href='//(paste.debian.net/[0-9a-zA-Z]+)'>", RegexOptions.IgnoreCase);
            if (match.Success) {
                url = match.Groups [1].Value;
                GetUrl = "http://" + url;
            } else {
                throw new ArgumentException("Check URL");
            }
        }

        public void Fpaste(string content, string nick, string language ,string expiry) {
            if (String.IsNullOrEmpty(content))
                throw new System.ArgumentNullException("Parameter cannot be null or empty", "content");
            if (String.IsNullOrEmpty (nick))
                nick = "Anonymous";
            if (String.IsNullOrEmpty (language))
                language = "text";
            if (String.IsNullOrEmpty(expiry))
                expiry = "1800";

            var servicePoint = ServicePointManager.FindServicePoint("http://fpaste.org/", Session.Proxy);
            servicePoint.Expect100Continue = false;

            var values = new NameValueCollection();
            values["paste_data"] = content;
            values["paste_user"] = nick;
            values["paste_lang"] = language;
            values["api_submit"] = "true";
            values["mode"] = "json";
            Session.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            var message = Session.UploadValues("http://fpaste.org/", "POST", values);
            var url = ASCIIEncoding.Default.GetString(message);
            var match = Regex.Match(url, @"""id"": ""([0-9a-zA-Z]+)""",RegexOptions.IgnoreCase);
            if (match.Success) {
                url = match.Groups [1].Value;
                GetUrl = "http://fpaste.org/" + url;
            } else {
                throw new ArgumentException("Check URL");
            }
        }

        public void CtrlV(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                throw new System.ArgumentNullException("Parameter cannot be null or empty", "filePath");

            var message = Session.UploadFile ("http://ctrlv.in//do/upload_from_file.php", "POST", filePath);
            var url = ASCIIEncoding.Default.GetString(message);
            var match = Regex.Match(url, @"\[url=(http://ctrlv.in/[0-9a-zA-Z]+)\]",RegexOptions.IgnoreCase);
            if (match.Success) {
                url = match.Groups [1].Value;
                GetUrl = url;
            } else {
                GetUrl = ("Check URL");
            }
        }
    }
}

