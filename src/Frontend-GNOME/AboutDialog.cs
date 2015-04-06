/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2006-2012 Mirco Bauer <meebey@meebey.net>
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
using Smuxi.Common;

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
            ProgramName = Frontend.Name;
            var version = Frontend.Version.ToString();
            var distVersion = Defines.DistVersion;
            if (!String.IsNullOrEmpty(distVersion)) {
                Version = String.Format("\n Vendor: {0}", distVersion);
            }
            Version += "\n Frontend: " + Frontend.UIName + " " + version  +
                       "\n Engine: " + Frontend.EngineVersion;
            Copyright = "Copyright © 2005-2015 Mirco Bauer <meebey@meebey.net> and other contributors";
            Authors = new string[] {
                "Mirco Bauer <meebey@meebey.net>",
                "David Paleino <dapal@debian.org>",
                "Clément Bourgeois <moonpyk@gmail.com>",
                "Chris Le Sueur <c.m.lesueur@gmail.com>",
                "Tuukka Hastrup <Tuukka.Hastrup@iki.fi>",
                "Bianca Mix <heavydemon@freenet.de>",
                "Oliver Schneider <mail@oli-obk.de>",
                "Carlos Martín Nieto <cmn@dwim.me>"
            };
            Artists = new string[] {
                "Jakub Steiner <jimmac@ximian.com>",
                "Rodney Dawes <dobey@novell.com>",
                "Lapo Calamandrei <calamandrei@gmail.com>",
                "Ahmed Abdellah <a3dman1@gmail.com>",
                "George Karavasilev <motorslav@gmail.com>",
                "Joern Konopka <cldx3000@googlemail.com>",
                "Nuno F. Pinheiro <nuno@oxygen-icons.org>"
            };
            TranslatorCredits = _("translator-credits");
            Logo = Frontend.LoadIcon(
                Frontend.IconName, 256, "icon_256x256.png"
            );
            // HACK: shows "not implemented" error on OS X and
            // "No application is registered as handling this file" on Windows.
            // This probably relies on gvfs or similar which isn't available in
            // the GTK{+,#} ports/installers for OS X and Windows. Thus we only
            // show the website URL as label instead.
            if (Frontend.IsMacOSX || Frontend.IsWindows) {
                WebsiteLabel = "http://www.smuxi.org/";
            } else {
                Website = "http://www.smuxi.org/";
                WebsiteLabel = _("Smuxi Website");
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
