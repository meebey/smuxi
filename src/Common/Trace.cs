/*
 * $Id: Page.cs 111 2006-02-20 23:10:45Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GtkGnome/Page.cs $
 * $Rev: 111 $
 * $Author: meebey $
 * $Date: 2006-02-21 00:10:45 +0100 (Tue, 21 Feb 2006) $
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
using System.Reflection;
using System.Diagnostics;
using SysTrace = System.Diagnostics.Trace;

namespace Meebey.Smuxi.Common
{
	public sealed class Trace
	{
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger("TRACE");
#else
	    static Trace()
		{
            TextWriterTraceListener myWriter = new TextWriterTraceListener(Console.Out);
            SysTrace.Listeners.Add(myWriter); 
		}
#endif

        [Conditional("TRACE")]
        public static void CallFull(params object[] args)
        {
            string line = Environment.NewLine;
            StackTrace st = new StackTrace();
            for (int i = st.FrameCount - 1; i >= 1 ; i--) {
                StackFrame sf = new StackFrame();
                sf = st.GetFrame(i);
                MethodBase method = sf.GetMethod();
                string methodname = method.DeclaringType + "." + method.Name;
                line += methodname + "(" + _Parameterize(args) + ")" + Environment.NewLine;
            }
#if LOG4NET
            _Logger.Debug(line);
#else
            SysTrace.Write(line);
#endif
        }
        
        public static MethodBase GetMethodBase()
        {
            MethodBase mb = new StackTrace().GetFrame(1).GetMethod();
            return mb;
        }
        
        [Conditional("TRACE")]
        public static void Call(params object[] args)
        {
            MethodBase mb = new StackTrace().GetFrame(1).GetMethod();
            Call(mb, args);
        }
        
        [Conditional("TRACE")]
        public static void Call(MethodBase mb, params object[] args)
        {
            string methodname = mb.DeclaringType.Name + "." + mb.Name;
            string line;
            line = methodname + "(" + _Parameterize(args) + ")";
#if LOG4NET
            _Logger.Debug(line);
#else
            SysTrace.WriteLine(line);
#endif
        }
        
        private static string _Parameterize(object[] parameters)
        {
            string res = null;
            if (parameters.Length > 0) {
                res += _ParameterizeQuote(parameters[0]);
                for (int i = 1; i < parameters.Length; i++) {
                    res += ", ";
                    res += _ParameterizeQuote(parameters[i]);
                }
            } else {
                res = String.Empty;
            }
            
            return res;
        }
        
        private static string _ParameterizeQuote(object obj)
        {
            if (obj is string) {
                return "'" + (obj == null ? "(null)" : obj) + "'";
            }
            
            return (obj == null ? "(null)" : obj.ToString());
        }
	}
}
