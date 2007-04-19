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

using System.Resources;
using System.Reflection;

namespace stringprep.unicode
{
    class ResourceLoader
    {
        private const string UNICODE = "stringprep.unicode.Unicode";
        private static ResourceManager m_uni_res = null;

        private static ResourceManager Resources
        {
            get
            {
                if (m_uni_res == null)
                {
                    lock (UNICODE)
                    {
                        if (m_uni_res == null)
                            m_uni_res = new ResourceManager(UNICODE, Assembly.GetExecutingAssembly());
                    }
                }
                return m_uni_res;
            }
        }

        public static object LoadRes(string name)
        {
            return Resources.GetObject(name);
        }
    }
}
