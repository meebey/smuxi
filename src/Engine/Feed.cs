// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2012 oliver
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
using System.Collections.Generic;
using Smuxi.Common;

using System.Xml.XPath;
using System.Xml;
using System.Web;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Smuxi.Engine
{
    public class FeedEntry
    {
        public DateTime Timestamp { get; protected set; }
        public String Title { get; protected set; }
        public String Description { get; protected set; }
        public String Link { get; protected set; }
        public String NewsFeedTitle { get; protected set; }
        public String Content { get; protected set; }

        public FeedEntry(AtomEntry entry, AtomFeed feed)
        {
            NewsFeedTitle = feed.Title.Text;
            Timestamp = entry.Published;
            if (entry.Title != null) {
                Title = entry.Title.Text;
            }
            if (entry.Content != null && entry.Content.Length > 0) {
                Content = entry.Content[0].Text;
            }
            if (entry.Summary != null) {
                Description = entry.Summary.Text;
            }
            if (entry.Link.Length > 0) {
                Link = entry.Link[0].Url;
            }
        }

        public FeedEntry(RSSItem item, RSSChannel channel)
        {
            NewsFeedTitle = channel.Title;
            Timestamp = item.Date;
            Title = item.Title;
            Description = item.Description;
            Link = item.Link;
            Content = item.Content;
        }
    }

    public class Feed
    {
#if LOG4NET
        static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif

        ProxySettings _ProxySettings;

        DateTime LastModified { get; set; }
        DateTime NextCheckForUpdates { get; set; }
        List<string> SeenFeedEntryIds { get; set; }

        public TimeSpan UpdateDelay { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public Uri Url {get; private set; }

        public ProxySettings ProxySettings {
            get {
                return _ProxySettings;
            }
            set {
                _ProxySettings = value;
                if (value == null) {
                    Request.Proxy = null;
                } else {
                    Request.Proxy = _ProxySettings.GetWebProxy(Url);
                }
            }
        }

        HttpWebRequest Request { get; set; }

        public Feed(Uri url)
            :this(url, true)
        {
        }

        public Feed(Uri url, bool use_encryption)
        {
            if (url == null) {
                throw new ArgumentNullException("url");
            }
            Url = url;

            if (!use_encryption) {
                var whitelist = Session.CertificateValidator.HostnameWhitelist;
                lock (whitelist) {
                    if (!whitelist.Contains(Url.Host)) {
                        whitelist.Add(Url.Host);
                    }
                }
            }

            var req = WebRequest.Create(Url);
            if (!(req is HttpWebRequest)) {
                throw new ArgumentException("url is not a http url");
            }
            Request = (HttpWebRequest) req;
//            string mozilla_crt_dir = "/usr/share/ca-certificates/mozilla/";
//            string[] files = System.IO.Directory.GetFiles(mozilla_crt_dir,"*.crt");
//            foreach (string file in files) {
//                X509Certificate cert = X509Certificate2.CreateFromCertFile(file);
//                Request.ClientCertificates.Add(cert);
//#if LOG4NET
//                f_Logger.Info("Connect(): importing cert from: " + file);
//#endif
//            }

            LastModified = DateTime.MinValue;
            UpdateDelay = TimeSpan.FromMinutes(30);
            RetryDelay = TimeSpan.FromMinutes(5);
            NextCheckForUpdates = DateTime.Now;
            SeenFeedEntryIds = new List<string>();
        }

        static string feed_type(string feed)
        {
            string type = null;
            try {
                XPathDocument doc = new XPathDocument(new System.IO.StringReader(feed));
                XPathNavigator nav = doc.CreateNavigator();
                XmlNamespaceManager nsm = new XmlNamespaceManager(nav.NameTable);
                XPathExpression expr = nav.Compile("/atom03:feed|/atom10:feed|/rss10:RDF|/rss20:rss");
    
                nsm.AddNamespace("atom10", "http://www.w3.org/2005/Atom");
                nsm.AddNamespace("atom03", "http://purl.org/atom/ns#");
                nsm.AddNamespace("rss10",  "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                nsm.AddNamespace("rss20", "");
                expr.SetContext(nsm);
        
                XPathNodeIterator iter = nav.Select(expr);
                iter.MoveNext();
        
                if (iter.Current != null){
                    switch (iter.Current.NamespaceURI){
                        case "http://www.w3.org/2005/Atom":
                        case "http://purl.org/atom/ns#":
                            type = "Atom";
                        break;
                        case "http://www.w3.org/1999/02/22-rdf-syntax-ns#":
                        case "":
                            type = "RSS";
                        break;
                        default:
                            type = "unknown";
                        break;
                    }
                } else {
                    type = "unknown";
                }
            } catch (Exception e){
                    Console.Error.WriteLine("Error determining feed type: {0}", e.Message);
            }
    
            return type;
        }

        void ResetRequest()
        {
            var req = (HttpWebRequest)WebRequest.Create(Url);
            req.ClientCertificates.AddRange(Request.ClientCertificates);
            req.Proxy = Request.Proxy;
            Request = req;
        }

        public List<FeedEntry> GetNewItems()
        {
            if (NextCheckForUpdates > DateTime.Now) {
#if LOG4NET
                f_Logger.Info("GetNewItems(): got to wait another " + (NextCheckForUpdates-DateTime.Now).TotalMinutes + " Minutes");
#endif
                // we don't want to overload the rss server
                return null;
            }
            var newlastmodified = LastModified;
            List<string> seen = new List<string>();
            List<FeedEntry> list = new List<FeedEntry>();
            Request.UserAgent = Engine.VersionString;
            if (LastModified != DateTime.MinValue) {
                Request.IfModifiedSince = LastModified;
            }
            var res = (HttpWebResponse)Request.GetResponse();
            try {
                // check whether anything changed
                if (res.StatusCode == HttpStatusCode.NotModified) {
                    NextCheckForUpdates = DateTime.Now.Add(UpdateDelay);
                    return null;
                }
                newlastmodified = res.LastModified;

                string stream;
                {
                    var resstream = res.GetResponseStream();
                    var streamreader = new System.IO.StreamReader(resstream);
                    stream = streamreader.ReadToEnd();
                    ResetRequest();
                }
                string type = feed_type(stream);

                if (type == "RSS") {
                    RSSFeed feed = RSSFeed.Load(new System.IO.StringReader(stream));
#if LOG4NET
                    f_Logger.Debug("GetNewItems(): Found " + feed.Channel.Length + " RSS Channels");
#endif
                    foreach (RSSChannel channel in feed.Channel) {
#if LOG4NET
                        f_Logger.Debug("GetNewItems(): Found " + channel.Item.Length + " RSS Feed Items");
#endif
                        foreach (var item in channel.Item) {
                            if (SeenFeedEntryIds.Contains(item.Guid)) {
                                continue;
                            }
                            if (seen.Contains(item.Guid)) {
                                continue;
                            }
#if LOG4NET
                            f_Logger.Debug("GetNewItems(): Adding RSS Feed Entry: " + item.Guid);
#endif
                            seen.Add(item.Guid);
                            list.Add(new FeedEntry(item, channel));
                        }
                    }
                } else if (type == "Atom") {
                    AtomFeed feed = AtomFeed.Load(new System.IO.StringReader(stream));
#if LOG4NET
                    f_Logger.Debug("GetNewItems(): Found " + feed.Entry.Length + " Atom FeedEntries");
#endif
                    foreach (var entry in feed.Entry) {
                        if (SeenFeedEntryIds.Contains(entry.Id)) {
                            continue;
                        }
                        if (seen.Contains(entry.Id)) {
                            continue;
                        }
#if LOG4NET
                        f_Logger.Debug("GetNewItems(): Adding Atom Feed Entry: " + entry.Id);
#endif
                        seen.Add(entry.Id);
                        list.Add(new FeedEntry(entry, feed));
                    }
                } else {
#if LOG4NET
                    f_Logger.Debug("GetNewItems(): Unknown Feed type: " + type);
#endif
                }
            } catch (Exception e) {
                ResetRequest();
                NextCheckForUpdates = DateTime.Now.Add(RetryDelay);
                // rethrow, let the outside handle this, we can't do anything anyway
                throw e;
            }
            // change NewsFeed object here (strong exception guarantee)
            SeenFeedEntryIds.AddRange(seen);
            LastModified = newlastmodified;
            NextCheckForUpdates = DateTime.Now.Add(UpdateDelay);
#if LOG4NET
            f_Logger.Debug("GetNewItems(): Done, got " + list.Count + " new items");
#endif
            return list;
        }
    }
}

