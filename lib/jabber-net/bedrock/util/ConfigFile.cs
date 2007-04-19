/* --------------------------------------------------------------------------
 * Copyrights
 *
 * Portions created by or assigned to Cursive Systems, Inc. are
 * Copyright (c) 2002-2007 Cursive Systems, Inc.  All Rights Reserved.  Contact
 * information for Cursive Systems, Inc. is available at
 * http://www.cursive.net/.
 *
 * License
 *
 * Jabber-Net can be used under either JOSL or the GPL.
 * See LICENSE.txt for details.
 * --------------------------------------------------------------------------*/
using System;

using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Collections;
using bedrock.util;
namespace bedrock.util
{
    /// <summary>
    /// XML configuration file manager.
    /// </summary>
    [SVN(@"$Id: ConfigFile.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ConfigFile
    {
        private string m_file;
        private XmlDocument m_doc;
        private static Hashtable s_instances = new Hashtable();
        /// <summary>
        /// Singleton factory
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ConfigFile GetInstance(string name)
        {
            ConfigFile inst = (ConfigFile) s_instances[name];
            if (inst == null)
            {
                lock (s_instances.SyncRoot)
                {
                    if (inst == null)
                    {
                        inst = new ConfigFile(name);
                        s_instances[name] = inst;
                    }
                }
            }
            return inst;
        }
        private ConfigFile(string name)
        {
            // Don't call Tracer from here!
            m_doc = new XmlDocument();
            string d = Path.GetDirectoryName(System.Environment.GetCommandLineArgs()[0]);
            DirectoryInfo p;
            while (d != null)
            {
                FileInfo fi = new FileInfo(Path.Combine(d, name));
                if (fi.Exists)
                {
                    m_file = fi.FullName;
                    m_doc.Load(m_file);
                    return;
                }
                p = fi.Directory.Parent;
                if (p == null)
                    break;
                d = p.FullName;
            }

            throw new FileNotFoundException(name);
        }

        /// <summary>
        /// The full path of the filename being used.
        /// </summary>
        public string Filename
        {
            get { return m_file; }
        }

        /// <summary>
        /// Get the configuration file XML node associated
        /// with a given XPath query.
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public XmlNode GetNode(string xpath)
        {
            return m_doc.SelectSingleNode(xpath);
            //ConfigFile f;
        }
        /// <summary>
        /// Get the configuration file XML nodes associated with a give XPath query
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public XmlNodeList GetNodes(string xpath)
        {
            return m_doc.SelectNodes(xpath);
        }
        /// <summary>
        /// Get the configuration file string associated
        /// with a given XPath query, or null if not found.
        /// </summary>
        public string this[string xpath]
        {
            get
            {
                return this[xpath, null];
            }
        }
        /// <summary>
        /// Get the configuration file string associated
        /// with a given XPath query, or defaultValue if not found.
        /// </summary>
        public string this[string xpath, string defaultValue]
        {
            get
            {
                string val;
                XmlNode n = m_doc.SelectSingleNode(xpath);
                if (n != null)
                {
                    val = n.InnerText;
                }
                else
                {
                    val = defaultValue;
                }
                return val;
            }
        }
    }
}
