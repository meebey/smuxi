/*
 * Copyright (c) 2008 Carlos Martín Nieto
 * 
 * This file is released under the terms of the GNU GPLv2 or later.
 */

using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.XPath;

namespace Smuxi.Common
{
    /*
     * This class serves as a way to use both RSS10Feed and RSS20Feed
     * in the same way so the program doesn't need to care.
     */
    public class RSSFeed {
        
        public string version = null;
        public RSSChannel[] Channel = null;
        
        public static RSSFeed Load(StringReader sr)
        {
            string f_c = sr.ReadToEnd();
            string type = feed_type(new StringReader(f_c));
            RSSFeed feed = new RSSFeed();
            if(type == "rss10"){
                feed.PopulateFromRSS10(new StringReader(f_c));
            } else if(type == "rss20"){
                feed.PopulateFromRSS20(new StringReader(f_c));
            } else {
                throw new NotSupportedException("Feed type not supported");
            }
            
            return feed;
        }
        
        public void PopulateFromRSS10(StringReader sr)
        {
            RSS10Feed feed = RSS10Feed.Load(sr);

            version = "1.0";
            
            /* FIXME: I think it's possible to have multiple channels. Try to
             * figure it out and implement it.
             */
            Channel = new RSSChannel[feed.Channel.Length]; // Always one.

            Channel[0] = new RSSChannel();
            Channel[0].Title = feed.Channel[0].Title;
            Channel[0].Description = feed.Channel[0].Description;
            Channel[0].Link = feed.Channel[0].Link;

            Channel[0].Item = new RSSItem[feed.Item.Length];

            for(int i = 0; i < feed.Item.Length; ++i){
                Channel[0].Item[i] = new RSSItem();
                Channel[0].Item[i].Title = feed.Item[i].Title;
                Channel[0].Item[i].Link = feed.Item[i].Link;
                Channel[0].Item[i].Description = feed.Item[i].Description;
                Channel[0].Item[i].Content = feed.Item[i].ContEnc;
                Channel[0].Item[i].Date = feed.Item[i].Date;
                Channel[0].Item[i].Author = feed.Item[i].Creator;
                Channel[0].Item[i].Guid = feed.Item[i].Link;
            }
        }
        
        public void PopulateFromRSS20(StringReader sr)
        {
            RSS20Feed feed = RSS20Feed.Load(sr);

            string lang = null;

            version = "2.0";
            
            Channel = new RSSChannel[feed.Channel.Length];

            for(int i = 0; i < feed.Channel.Length; ++i){
                Channel[i] = new RSSChannel();
                Channel[i].Title = feed.Channel[i].Title;
                Channel[i].Link = feed.Channel[i].Link;
                Channel[i].Language = feed.Channel[i].Language;
                Channel[i].Description = feed.Channel[i].Description;
                
                Channel[i].Item = new RSSItem[feed.Channel[i].Item.Length];
                
                for(int j= 0; j < feed.Channel[i].Item.Length; ++j){
                    Channel[i].Item[j] = new RSSItem();
                    Channel[i].Item[j].Title = feed.Channel[i].Item[j].Title;
                    Channel[i].Item[j].Guid = feed.Channel[i].Item[j].Guid;
                    Channel[i].Item[j].Link = feed.Channel[i].Item[j].Link;
                    Channel[i].Item[j].Description = feed.Channel[i].Item[j].Description;
                    Channel[i].Item[j].Content = feed.Channel[i].Item[j].ContEnc;

                    if(feed.Channel[i].Item[j].Author == null){
                        Channel[i].Item[j].Author = feed.Channel[i].Item[j].Creator;
                    } else {
                        Channel[i].Item[j].Author = feed.Channel[i].Item[j].Author;
                    }

                    if(feed.Channel[i].Item[j].PubDate != null){

                        /* Horrible hack, but it works. */
                        if(feed.Channel[i].Item[j].PubDate.EndsWith("UTC")){
                            string s = feed.Channel[i].Item[j].PubDate.Substring(0,
                               feed.Channel[i].Item[j].PubDate.Length - 3);
                            s += "+0000";
                            feed.Channel[i].Item[j].PubDate = s;
                        }
                        if(feed.Channel[i].Item[j].PubDate.EndsWith("CDT")){
                            string s = feed.Channel[i].Item[j].PubDate.Substring(0,
                               feed.Channel[i].Item[j].PubDate.Length - 3);
                            s += "+0000";
                            feed.Channel[i].Item[j].PubDate = s;
                        }
                        if(feed.Channel[i].Item[j].PubDate.EndsWith("GMT")){
                            string s = feed.Channel[i].Item[j].PubDate.Substring(0,
                               feed.Channel[i].Item[j].PubDate.Length - 3);
                            s += "+0000";
                            feed.Channel[i].Item[j].PubDate = s;
                        }

                        if(feed.Channel[i].Language == null){
                            lang = "en-US"; /* Choose a sane default. */
                        } else {
                            lang = feed.Channel[i].Language;
                        }

                        Channel[i].Item[j].Date = DateTime.Parse(feed.Channel[i].Item[j].PubDate,
                                System.Globalization.CultureInfo.CreateSpecificCulture(lang));
                    }
                }
            }
        }
        
        private static string feed_type(StringReader sr)
        {
            XPathDocument doc = new XPathDocument(sr);
            XPathNavigator nav = doc.CreateNavigator();
            XmlNamespaceManager nsm = new XmlNamespaceManager(nav.NameTable);
            XPathExpression expr = nav.Compile("/rss10:RDF|/rss20:rss");
            
            nsm.AddNamespace("rss10", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            nsm.AddNamespace("rss20", "");
            
            expr.SetContext(nsm);
            
            XPathNodeIterator iter = nav.Select(expr);
            iter.MoveNext();
        
            string str = "(none)";
            
            if(iter.Current != null){
                switch(iter.Current.NamespaceURI){
                case "http://www.w3.org/1999/02/22-rdf-syntax-ns#":
                    str = "rss10";
                    break;
                case "":
                    str = "rss20";
                    break;
                default:
                    str = "(unknown)";
                    break;
                }
            }
            
            return str;
        }
    }
    
    public class RSSChannel {
        public string Title = null;
        public string Link = null;
        public string Language = null;
        public string Description = null;
        public string Generator = null;
        public RSSItem[] Item;
    }
    
    public class RSSItem {
        public string Title = null;
        public string Author = null;
        public DateTime Date = DateTime.MinValue;
        public string Description = null;
        public string Content = null;
        public string Guid = null;
        public string Link = null;
    }
    
    [XmlRoot("RDF")]
    public class RSS10Feed {
        [XmlElement("channel", Namespace="http://purl.org/rss/1.0/")] public RSSGenChannel[] Channel = null;
        [XmlElement("item", Namespace="http://purl.org/rss/1.0/" )] public RSSGenItem[] Item = null;
        
    static XmlSerializer ser = new XmlSerializer(typeof(RSS10Feed),
                              "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
//   "http://purl.org/rss/1.0");


 public static RSS10Feed LoadFromXml(string uri)
 {
     try {
     FileStream fs = new FileStream(uri, FileMode.Open);
     return (RSS10Feed)ser.Deserialize(fs);
     }
     catch(FileNotFoundException){
         Console.Error.WriteLine("Can't open the flie");
         return null;
     }

 }

 public static RSS10Feed Load(StringReader sr)
 {
     return (RSS10Feed)ser.Deserialize(sr);
 }
    }
    
    [XmlType("rss")]
    public class RSS20Feed {
 [XmlAttribute("version")] public float version;
 [XmlElement("channel")] public RSSGenChannel[] Channel = null;
 
 static XmlSerializer ser = new XmlSerializer(typeof(RSS20Feed));//,
 //"http://purl.org/dc/elements/1.1/");

 public static RSS20Feed LoadFromXml(string uri)
 {
     try {
     FileStream fs = new FileStream(uri, FileMode.Open);
     return (RSS20Feed)ser.Deserialize(fs);
     }
     catch(FileNotFoundException){
         Console.Error.WriteLine("Can't open the flie");
         return null;
     }

 }

 public static RSS20Feed Load(StringReader sr)
 {
     return (RSS20Feed)ser.Deserialize(sr);
 }
    }

    [XmlType("channel")]
    public class RSSGenChannel {
        [XmlElement("title")] public string Title = null;
        [XmlElement("link")] public string Link = null;
        [XmlElement("language")] public string Language = null;
        [XmlElement("description")] public string Description = null;
        [XmlElement("lastBuildDate")] public string LastBuildDate = null;
        [XmlElement("generator")] public string Generator = null;
        [XmlElement("item")] public RSSGenItem[] Item = null;
    }

    [XmlType("item")]
    public class RSSGenItem {
        [XmlElement("title")] public string Title = null;
        [XmlElement("guid")] public string Guid = null;
        [XmlElement("link")] public string Link = null;
        [XmlElement("description")] public string Description = null;
        [XmlElement("encoded", Namespace="http://purl.org/rss/1.0/modules/content/")] public string ContEnc = null;
        //[XmlElement("pubDate")] public DateTime PubDate = DateTime.MinValue;
        [XmlElement("pubDate")] public string PubDate = null;
        [XmlElement("author")] public string Author = null;
        [XmlElement("creator", Namespace="http://purl.org/dc/elements/1.1/")] public string Creator = null;
        [XmlElement("date", Namespace="http://purl.org/dc/elements/1.1/")] public DateTime Date;
    }

    [XmlType("image")]
    public class RSSImage {
        [XmlElement("url")] public string Url = null;
        [XmlElement("title")] public string Title = null;
        [XmlElement("link")] public string Link = null;
        [XmlElement("width")] public int Width;
        [XmlElement("height")] public int Height;
    }
}

