/*
 * $Id: Main.cs 183 2007-04-21 15:14:23Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/Main.cs $
 * $Rev: 183 $
 * $Author: meebey $
 * $Date: 2007-04-21 17:14:23 +0200 (Sat, 21 Apr 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007 Mirco Bauer <meebey@meebey.net>
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

namespace Smuxi.Frontend.Curses
{ 
    public class MainClass
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif

        public static void Main(string[] args)
        {
#if LOG4NET
            //log4net.Config.BasicConfigurator.Configure();
#endif
            try {
                Frontend.Init(args);
            } catch (Exception e) {
#if LOG4NET
                _Logger.Fatal(e);
#endif
                throw;
            }
        }
    }
}
