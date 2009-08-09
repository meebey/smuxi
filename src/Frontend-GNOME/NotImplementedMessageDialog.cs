/*
 * $Id: PreferencesDialog.cs 73 2005-06-27 12:42:06Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GtkGnome/PreferencesDialog.cs $
 * $Rev: 73 $
 * $Author: meebey $
 * $Date: 2005-06-27 14:42:06 +0200 (Mon, 27 Jun 2005) $
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
using Mono.Unix;
using Smuxi;

namespace Smuxi.Frontend.Gnome
{
    public class NotImplementedMessageDialog : Gtk.MessageDialog
    {
        public NotImplementedMessageDialog(Gtk.Window parent) :
                                      base(parent, Gtk.DialogFlags.Modal,
                                           Gtk.MessageType.Info, Gtk.ButtonsType.Close,
                                           _("Sorry, not implemented yet!"))
        {
        }
        
        public static void Show(Gtk.Window parent)
        {
            NotImplementedMessageDialog nimd = new NotImplementedMessageDialog(parent);
            nimd.Run();
            nimd.Destroy();
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }    
    }
}
