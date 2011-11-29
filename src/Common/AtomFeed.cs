/*
 * Copyright (c) 2007 Carlos Martín Nieto <carlos@cmartin.tk>
 *
 * This file is released under the terms of the GNU GPLv2 or later
 */

using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Smuxi.Common
{
    [XmlType("feed")]
    public class AtomFeed
    {
        [XmlElement("link")] public AtomLink[] Link = null;
        
        [XmlElement("updated")] public DateTime UpdateTime = DateTime.MinValue;
        [XmlElement("modified")] public DateTime ModifyTime = DateTime.MinValue;
        [XmlElement("title")] public AtomText Title = null;
        [XmlElement("subtitle")] public string Subtitle = null;
        [XmlElement("author")] public AtomAuthor Author = null;
        
        [XmlElement("entry")] public AtomEntry[] Entry;
        
        private static XmlSerializer ser = new XmlSerializer(typeof(AtomFeed),
                                                             "http://www.w3.org/2005/Atom");

        public static AtomFeed LoadFromXml(string file)
        {
            try {
                FileStream fs = new FileStream(file, FileMode.Open);
                return (AtomFeed)ser.Deserialize(fs);
            } catch(FileNotFoundException){
                Console.Error.WriteLine("Unable to open file");
                return null;
            }
        }

        public static AtomFeed Load(StringReader sr)
        {
            return (AtomFeed)ser.Deserialize(sr);
        }

        public static AtomFeed Load(Stream stream)
        {
            return (AtomFeed) ser.Deserialize(stream);
        }

        public DateTime Modified {
            get {
                if(UpdateTime != DateTime.MinValue){
                    return UpdateTime;
                } else {
                    return ModifyTime;
                }
            }
        }
        
        public DateTime Updated {
            get {
                return Modified;
            }
        }

        public AtomLink LinkByType(string type)
        {
            foreach(AtomLink link in Link){
                if(link.Type == type){
                    return link;
                }
            }

            return null;
        }
    }

    [XmlType("author")]
    public class AtomAuthor
    {
        [XmlElement("name")] public string Name;
        [XmlElement("email")] public string Email;
    }

    [XmlType("link")]
    public class AtomLink
    {
        [XmlAttribute("href")] public string Url = null;
        [XmlAttribute("rel")] public string Rel = null;
        [XmlAttribute("type")] public string Type = null;
    }

    [XmlType("entry")]
    public class AtomEntry
    {
        [XmlElement("link")] public AtomLink[] Link = null;
        [XmlElement("published")] public DateTime Published;
        [XmlElement("updated")] public DateTime UpdateTime = DateTime.MinValue;
        [XmlElement("modified")] public DateTime ModifyTime = DateTime.MinValue;
        [XmlElement("title")] public AtomText Title;
        [XmlElement("author")] public AtomAuthor Author = null;
        [XmlElement("id")] public string Id;

        [XmlElement("content")] public AtomText[] Content;
        [XmlElement("summary")] public AtomText Summary;

        public DateTime Modified {
            get {
                if(UpdateTime != DateTime.MinValue){
                    return UpdateTime;
                } else {
                    return ModifyTime;
                }
            }
        }
        
        public DateTime Updated {
            get {
                return Modified;
            }
        }

        public AtomLink LinkByType(string type)
        {
            foreach(AtomLink link in Link){
                if(link.Type == type){
                        return link;
                }
            }
            return null;
        }

        public AtomText ContentByType(string type)
        {
            foreach(AtomText text in Content){
                if(text.Type == type){
                    return text;
                }
            }

            return null;
        }
    }

    public class AtomText
    {
        [XmlText] public string Text = null;
        [XmlAttribute("type")] public string Type = null;
    }
}
