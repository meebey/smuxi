// This file is part of Smuxi and is licensed under the terms of MIT/X11
//
// Copyright (c) 2012 Mirco Bauer <meebey@meebey.net>
// Copyright (c) 2015 Carlos Martín Nieto <cmn@dwim.me>
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Smuxi.Common;

namespace Smuxi.Common
{
    public class IconCache
    {
        #if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endif

        public IWebProxy Proxy { get; set; }

        readonly string f_CachePath = Platform.CachePath;
        readonly string f_IconsPath;

        /// <summary>
        /// Try to get a cached icon
        /// </summary>
        /// <returns><c>true</c>, if get icon is cached, <c>false</c> otherwise.</returns>
        /// <param name="protocol">The protocol of the channel or server</param>
        /// <param name="iconName">Name of the icon, including file extension</param>
        /// <param name="value">Path to the cached icon, if cached</param>
        /// <remarks>
        /// This method is thread safe
        /// </remarks>
        public bool TryGetIcon(string protocol, string iconName, out string value)
        {
            var iconPath = Path.Combine(f_IconsPath, protocol, iconName);
            var iconFile = new FileInfo(iconPath);
            if (iconFile.Exists && iconFile.Length > 0) {
                value = iconPath;
                return true;
            }

            value = null;
            return false;
        }

        void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Download an image file into the cache
        /// </summary>
        /// <param name="protocol">The protocol of the channel or server</param>
        /// <param name="iconName">Name of the image, including extension</param>
        /// <param name="fileUrl">The url from which to download the image</param>
        /// <param name="onSuccess">Function to call after downloading the image</param>
        /// <param name="onError">Function to call if an error happens (optional)</param>
        /// <remarks>
        /// This method is thread safe
        /// </remarks>
        public void BeginDownloadFile(string protocol, string iconName, string fileUrl,
            Action<string> onSuccess, Action<Exception> onError)
        {
            EnqueueDownload(protocol, iconName, fileUrl, DownloadFileWorker, onSuccess, onError);
        }

        void DownloadFileWorker(string fileUrl, FileInfo imageFile)
        {
            Trace.Call(fileUrl, imageFile);

            // download to a randomly-named file so we don't conflict with a concurrent download
            var tempFile = new FileInfo(Path.Combine(imageFile.DirectoryName, Path.GetRandomFileName()));
            DownloadFileFromUrl(fileUrl, tempFile);

            // finally, rename atomically, giving up if someone beat us to downloading
            // this file
            try {
                tempFile.MoveTo(imageFile.FullName);
            } catch (IOException) {
                // someone beat us to downloading the image, simply remove the temp file
                tempFile.Delete();
            }
        }

        void EnqueueDownload(string protocol, string iconName, string url,
            Action<string, FileInfo> action, Action<string> onSuccess, Action<Exception> onError)
        {
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    var protocolPath = Path.Combine(f_IconsPath, protocol);
                    EnsureDirectoryExists(protocolPath);
                    var iconPath = Path.Combine(protocolPath, iconName);
                    var iconFile = new FileInfo(iconPath);
                    action(url, iconFile);
                    iconFile.Refresh();
                    if (!iconFile.Exists || iconFile.Length == 0) {
                        return;
                    }

                    onSuccess(iconPath);
                } catch (Exception ex) {
#if LOG4NET
                    f_Logger.Error("IconCache: Exception", ex);
#endif
                    if (onError != null) {
                        onError(ex);
                    }
                }
            });
        }

        void DownloadFileFromUrl(string url, FileInfo file)
        {
            var request = WebRequest.Create(url);
            request.Proxy = Proxy;

            if (request is HttpWebRequest) {
                var iconHttpRequest = (HttpWebRequest) request;
                if (file.Exists) {
                    iconHttpRequest.IfModifiedSince = file.LastWriteTime;
                }
            }

            WebResponse response;
            try {
                response = request.GetResponse();
            } catch (WebException ex) {
                if (ex.Response is HttpWebResponse) {
                    var iconHttpResponse = (HttpWebResponse) ex.Response;
                    if (iconHttpResponse.StatusCode == HttpStatusCode.NotModified) {
                        // icon hasn't changed, nothing to do
                        return;
                    }
                }
                throw;
            }

            using (var fileStream = file.OpenWrite())
            using (var httpStream = response.GetResponseStream()) {
                byte[] buffer = new byte[4096];
                int read;
                while ((read = httpStream.Read(buffer, 0, buffer.Length)) > 0) {
                    fileStream.Write(buffer, 0, read);
                }
            }
        }

        /// <summary>
        /// Download an icon into the cache.
        ///
        /// The download will happen in the background and onSuccess will be called
        /// when done. If there is an error during the download, an error will be
        /// logged and onSuccess will not be called.
        ///
        /// The callback will be called in the background thread. If you want to
        /// update the GUI, make sure you schedule code to run in the main thread.
        /// </summary>
        /// <param name="protocol">The protocol of the channel or server</param>
        /// <param name="iconName">Name of the icon, including extension</param>
        /// <param name="websiteUrl">The url from which to download the icon</param>
        /// <param name="onSuccess">Function to call after downloading the icon</param>
        /// <param name="onError">Function to call if an error happens (optinal)</param>
        /// <remarks>
        /// This method is thread safe
        /// </remarks>
        public void BeginDownloadIcon(string protocol, string iconName, string websiteUrl,
            Action<string> onSuccess, Action<Exception> onError)
        {
            EnqueueDownload(protocol, iconName, websiteUrl, DownloadServerIcon, onSuccess, onError);
        }

        void DownloadServerIcon(string websiteUrl, FileInfo iconFile)
        {
            Trace.Call(websiteUrl, iconFile);

            var webClient = new WebClient();
            webClient.Proxy = Proxy;

            var content = webClient.DownloadString(websiteUrl);
            var links = new List<Dictionary<string, string>>();
            foreach (Match linkMatch in Regex.Matches(content, @"<link[\s]+([^>]*?)/?>")) {
                var attributes = new Dictionary<string, string>();
                foreach (Match attrMatch in Regex.Matches(linkMatch.Value, @"([\w]+)[\s]*=[\s]*[""']([^""']*)[""'][\s]*")) {
                    var key = attrMatch.Groups[1].Value;
                    var value = attrMatch.Groups[2].Value;
                    attributes.Add(key, value);
                }
                links.Add(attributes);
            }
            string faviconRel = null;
            foreach (var link in links) {
                var iconLink = false;
                foreach (var attribute in link) {
                    if (attribute.Key != "rel" ||
                        !attribute.Value.Split(' ').Contains("icon")) {
                        continue;
                    }
                    iconLink = true;
                    break;
                }
                if (!iconLink) {
                    continue;
                }
                foreach (var attribute in link) {
                    if (attribute.Key != "href") {
                        continue;
                    }
                    // yay, we have found the favicon in all this junk
                    faviconRel = attribute.Value;
                    break;
                }
            }
            string faviconUrl = null;
            if (String.IsNullOrEmpty(faviconRel)) {
                faviconRel = "/favicon.ico";
            }
            faviconUrl = new Uri(new Uri(websiteUrl), faviconRel).ToString();
            #if LOG4NET
            f_Logger.DebugFormat("DownloadServerIcon(): favicon URL: {0}",
                faviconUrl);
            #endif


            // save new or modified icon file
            DownloadFileFromUrl(faviconUrl, iconFile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Smuxi.Frontend.Gnome.IconCache"/> class.
        /// </summary>
        /// <param name="kind">The kind of icons to cache (e.g. "server-icons", "emoji")</param>
        public IconCache(string kind)
        {
            f_IconsPath = Path.Combine(f_CachePath, kind);
            EnsureDirectoryExists(f_IconsPath);
        }
    }
}

