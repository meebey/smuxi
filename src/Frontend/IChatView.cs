/*
 * $Id: AboutDialog.cs 122 2006-04-26 19:31:42Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/AboutDialog.cs $
 * $Rev: 122 $
 * $Author: meebey $
 * $Date: 2006-04-26 21:31:42 +0200 (Wed, 26 Apr 2006) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2007 Mirco Bauer <meebey@meebey.net>
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
using Smuxi.Engine;

namespace Smuxi.Frontend
{
    public interface IChatView
    {
        ChatModel ChatModel { get; }
        string    ID { get; }
        int       Position { get; }

        void Enable();
        void Disable();
        void Sync();
        void Populate();

        void ScrollUp();
        void ScrollDown();
        void ScrollToStart();
        void ScrollToEnd();
    }
}
