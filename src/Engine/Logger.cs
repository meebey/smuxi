/*
 * $Id$
 * $Revision$
 * $Author$
 * $Date$
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

using System;
using System.IO;
using System.Collections;

namespace Meebey.Smuxi.Engine
{
#if LOG4NET
    public enum Category
    {
        Main,
        UI,
        Remoting,
        Session,
        Config,
        Command,
        NickCompletion,
        IrcManager,
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
                FileInfo fi = new FileInfo("smuxi-engine_log.config");
                if (fi.Exists) {
                    log4net.Config.DOMConfigurator.ConfigureAndWatch(fi);
                } else {
                    log4net.Config.BasicConfigurator.Configure();
                }
            }
            
            _LoggerList[Category.Main]  = log4net.LogManager.GetLogger("MAIN");
            _LoggerList[Category.UI] = log4net.LogManager.GetLogger("UI");
            _LoggerList[Category.Remoting] = log4net.LogManager.GetLogger("REMOTING");
            _LoggerList[Category.Session] = log4net.LogManager.GetLogger("SESSION");
            _LoggerList[Category.Config] = log4net.LogManager.GetLogger("CONFIG");
            _LoggerList[Category.Command] = log4net.LogManager.GetLogger("COMMAND");
            _LoggerList[Category.NickCompletion] = log4net.LogManager.GetLogger("NICKCOMPLETION");
            _LoggerList[Category.IrcManager] = log4net.LogManager.GetLogger("IRCMANAGER");
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
        
        public static log4net.ILog Session
        {
            get {
                return (log4net.ILog)_LoggerList[Category.Session];
            }
        }
        
        public static log4net.ILog Config
        {
            get {
                return (log4net.ILog)_LoggerList[Category.Config];
            }
        }

        public static log4net.ILog Command
        {
            get {
                return (log4net.ILog)_LoggerList[Category.Command];
            }
        }

        public static log4net.ILog NickCompletion
        {
            get {
                return (log4net.ILog)_LoggerList[Category.NickCompletion];
            }
        }

        public static log4net.ILog IrcManager
        {
            get {
                return (log4net.ILog)_LoggerList[Category.IrcManager];
            }
        }
    }
#endif
}
