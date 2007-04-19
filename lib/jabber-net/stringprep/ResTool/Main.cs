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

namespace ResTool
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    class MainApp
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ResTool <assembly> <resxfile>");
                Environment.Exit(64);
            }
            Assembly asy = Assembly.LoadFrom(args[0]);
            ResXResourceWriter resx = new ResXResourceWriter(args[1]);
            foreach (Type t in asy.GetTypes())
            {
                foreach (FieldInfo fi in t.GetFields())
                {
                    if (!fi.IsStatic)
                        continue;

                    string n = t.Name + "." + fi.Name;
                    Console.WriteLine(n);

                    resx.AddResource(n, fi.GetValue(null));
                }
            }
            resx.Close();
        }
    }
}
