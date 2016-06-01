// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2016 Mirco Bauer <meebey@meebey.net>
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
using System.IO;
using System.Reflection;
#if INTERNAL_GTK_BUILDER
namespace Smuxi.Frontend.Gnome
{
    public class GtkBuilder : Gtk.Builder
    {
        public GtkBuilder(Assembly assembly, string resource_name, string translation_domain)
        {
            if (resource_name == null) {
                throw new ArgumentNullException("resource_name");
            }
            if (assembly == null) {
                assembly = Assembly.GetExecutingAssembly();
            }
            if (!String.IsNullOrEmpty(translation_domain)) {
                TranslationDomain = translation_domain;
            }

            using (var stream = assembly.GetManifestResourceStream(resource_name))
            using (var reader = new StreamReader(stream))
            {
                var ui = reader.ReadToEnd();
                AddFromString(ui);
            }
        }

        public class ObjectAttribute : Attribute
        {
            public string Name { get; private set; }
            public string Specified { get; private set; }

            public ObjectAttribute(string name)
            {
                Name = name;
            }
        }
    }
}
#endif