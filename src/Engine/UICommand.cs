/**
 * $Id: AssemblyInfo.cs 34 2004-09-05 14:46:59Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/Gnosmirc/trunk/src/AssemblyInfo.cs $
 * $Rev: 34 $
 * $Author: meebey $
 * $Date: 2004-09-05 16:46:59 +0200 (Sun, 05 Sep 2004) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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

namespace Meebey.Smuxi.Engine
{
    public enum UICommand
    {
        AddPage,
        AddTextToPage,
        RemovePage,
        AddUserToChannel,
        UpdateUserInChannel,
        UpdateTopicInChannel,
        RemoveUserFromChannel,
        SetNetworkStatus,
        SetStatus,
    }
}
