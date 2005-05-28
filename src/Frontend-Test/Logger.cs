/**
 * $Id: Logger.cs,v 1.5 2004/07/15 20:51:03 meebey Exp $
 * $Revision: 1.5 $
 * $Author: meebey $
 * $Date: 2004/07/15 20:51:03 $
 *
 * Copyright (c) 2003 Mirco 'meebey' Bauer <mail@meebey.net> <http://www.meebey.net>
 * 
 * Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System.IO;
using System.Collections;

namespace Meebey.Smuxi.FrontendTest
{
#if LOG4NET
    public enum Category
    {
        Main,
        UI,
        Remoting
    }
    
    public class Logger
    {
        private static SortedList _LoggerList = new SortedList();
        private static bool       _Init = false;
        private Logger()
        {
        }
            
        public static void Init()
        {
            if (_Init) {
                return;
            }
                
            _Init = true;
                
            if (log4net.LogManager.GetCurrentLoggers().Length == 0) {
                FileInfo fi = new FileInfo("smuxi-test_log.config");
                if (fi.Exists) {
                    log4net.Config.DOMConfigurator.ConfigureAndWatch(fi);
                } else {
                    log4net.Config.BasicConfigurator.Configure();
                }
            }
    
            _LoggerList[Category.Main]     = log4net.LogManager.GetLogger("MAIN");
            _LoggerList[Category.UI]       = log4net.LogManager.GetLogger("UI");
            _LoggerList[Category.Remoting] = log4net.LogManager.GetLogger("REMOTING");
        }
    
        public static log4net.ILog Main
        {
            get {
                return (log4net.ILog)_LoggerList[Category.Main];
            }
        }
    
        public static log4net.ILog UI
        {
            get {
                return (log4net.ILog)_LoggerList[Category.UI];
            }
        }
        
        public static log4net.ILog Remoting
        {
            get {
                return (log4net.ILog)_LoggerList[Category.Remoting];
            }
        }
    }
#endif
}
