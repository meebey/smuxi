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

using System.Text;

namespace stringprep.steps
{
    /// <summary>
    /// Base class for steps in a stringprep profile.
    /// </summary>
    public abstract class ProfileStep
    {
        private string m_name;

        /// <summary>
        /// Create a named profile step, with no flags.
        /// </summary>
        /// <param name="name">The profile name</param>
        protected ProfileStep(string name)
        {
            m_name = name;
        }

        /// <summary>
        /// The name of the step.
        /// </summary>
        public virtual string Name
        {
            get { return m_name; }
        }

        /// <summary>
        /// This is the workhorse function, to be implemented in each subclass.
        /// </summary>
        /// <param name="result">Result will be modified in place</param>
        public abstract void Prepare(StringBuilder result);
    }
}
