/*
 * $Id: PreferencesDialog.cs 283 2008-07-16 21:26:07Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/Preferences/PreferencesDialog.cs $
 * $Rev: 283 $
 * $Author: meebey $
 * $Date: 2008-07-16 23:26:07 +0200 (Wed, 16 Jul 2008) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
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

namespace Smuxi.Frontend.Gnome
{
    public partial class SteticPreferencesDialog : Gtk.Dialog
    {
        public SteticPreferencesDialog()
        {
            Build();
        }
        
        protected virtual void _OnChanged(object sender, System.EventArgs e)
        {
        }
    }
}
