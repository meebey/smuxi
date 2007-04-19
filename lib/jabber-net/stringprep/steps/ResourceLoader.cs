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

namespace stringprep.steps
{
    class ResourceLoader
    {
        private const string RFC3454 = "stringprep.steps.rfc3454";
        private static ResourceManager m_rfc_res = null;

        private static ResourceManager Resources
        {
            get
            {
                if (m_rfc_res == null)
                {
                    lock (RFC3454)
                    {
                        if (m_rfc_res == null)
                            m_rfc_res = new ResourceManager(RFC3454, Assembly.GetExecutingAssembly());
                    }
                }
                return m_rfc_res;
            }
        }

        public static object LoadRes(string name)
        {
            return Resources.GetObject(name);
        }
    }
}
