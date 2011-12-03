/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
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
using System.Linq;

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
            Copyright = "Copyright © 2005-2010 Mirco Bauer <meebey@meebey.net>";
            Authors = new string[] {
                "Mirco Bauer <meebey@meebey.net>",
                "David Paleino <dapal@debian.org>",
                "Clément Bourgeois <moonpyk@gmail.com>",
                "Chris Le Sueur <c.m.lesueur@gmail.com>",
                "Tuukka Hastrup <Tuukka.Hastrup@iki.fi>"
            };
            Artists = new string[] {
                "Jakub Steiner <jimmac@ximian.com>",
                "Rodney Dawes <dobey@novell.com>",
                "Lapo Calamandrei <calamandrei@gmail.com>",
                "Ahmed Abdellah <a3dman1@gmail.com>"
            };
            TranslatorCredits = _("translator-credits");
            if (Frontend.HasSystemIconTheme) {
                LogoIconName = Frontend.IconName;
            } else {
                Logo = Frontend.LoadIcon(
                    Frontend.IconName, 256, "icon_256x256.png"
                );
            }
            Website = "http://www.smuxi.org/";
            WebsiteLabel = _("Smuxi Website");
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
