/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using System;
using Smuxi;

namespace Smuxi.Frontend.Gnome
{
    public class AboutDialog : Gtk.AboutDialog
    {
        public AboutDialog(Gtk.Window parent)
        {
            if (parent == null) {
                throw new ArgumentNullException("parent");
            }
            
            TransientFor = parent;
            Name = Frontend.Name;
            Version = "\n Frontend: " + Frontend.UIName + " " + Frontend.Version +
                      "\n Engine: " + Frontend.EngineVersion;
            Copyright = "Copyright © 2005-2008 Mirco Bauer <meebey@meebey.net>";
            Authors = new string[] {"Mirco Bauer <meebey@meebey.net>"};
            TranslatorCredits = _("German") + " - Mirco Bauer <meebey@meebey.net>\n" +
                                _("Spanish") + " - Juan Miguel Carrero <streinleght@gmail.com>\n" +
                                _("British English") + " - Ryan Smith-Evans <Kimera.Kimera@gmail.com>\n" +
                                _("French") + " - Clement BOURGEOIS <moonpyk@gmail.com>\n" +
                                _("Italian") + " - David Paleino <d.paleino@gmail.com>";
            Logo = new Gdk.Pixbuf(null, "about.png");
            Website = "http://www.smuxi.org/";
            WebsiteLabel = _("Smuxi Website");
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
