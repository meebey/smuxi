/*
 * $Id: TestUI.cs 179 2007-04-21 15:01:29Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-Test/TestUI.cs $
 * $Rev: 179 $
 * $Author: meebey $
 * $Date: 2007-04-21 17:01:29 +0200 (Sat, 21 Apr 2007) $
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
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Stfl
{
    public class Form : IDisposable
    {
        IntPtr f_Handle;
        
        public event KeyPressedEventHandler KeyPressed;
        public event EventHandler<EventReceivedEventArgs> EventReceived;
        
        public string this[string name] {
            get {
                return StflApi.stfl_get(f_Handle, name);
            }
            set {
                StflApi.stfl_set(f_Handle, name, value);
            }
        }

        public Form(string text)
        {
            f_Handle = StflApi.stfl_create(text);

            // initialize ncurses
            StflApi.stfl_run(f_Handle, -3);
            //StflApi.raw();
            NcursesApi.nocbreak();
        }

        public Form(Assembly assembly, string resourceName)
        {
            if (assembly == null) {
                assembly = Assembly.GetCallingAssembly();
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream)) {
                if (stream == null) {
                    throw new ArgumentException(resourceName + " could not be found in assembly", "resourceName");
                }
                string text = reader.ReadToEnd();
                if (String.IsNullOrEmpty(text)) {
                    throw new ArgumentException(resourceName + " in assembly is missing or empty.", "resourceName");
                }
                f_Handle = StflApi.stfl_create(text);
            }
        }

        ~Form()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (f_Handle != IntPtr.Zero) {
                StflApi.stfl_free(f_Handle);
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Run(int timeout)
        {
            string @event = StflApi.stfl_run(f_Handle, timeout);
            ProcessEvent(@event);
        }

        public void Run()
        {
            Run(0);
        }

        public void Modify(string name, string mode, string text)
        {
            StflApi.stfl_modify(f_Handle, name, mode, text);
        }

        public string Dump(string name, string prefix, int focus)
        {
            return StflApi.stfl_dump(f_Handle, name, prefix, focus);
        }

        protected virtual void ProcessEvent(string @event)
        {
            OnEventReceived(new EventReceivedEventArgs(@event));
            if (@event != null && @event != "TIMEOUT") {
                ProcessKey(@event);
            }
        }

        protected virtual void OnEventReceived(EventReceivedEventArgs e)
        {
            if (EventReceived != null) {
                EventReceived(this, e);
            }
        }

        protected virtual void ProcessKey(string key)
        {
            string focus = StflApi.stfl_get_focus(f_Handle);
            OnKeyPressed(new KeyPressedEventArgs(key, focus));
        }

        protected virtual void OnKeyPressed(KeyPressedEventArgs e)
        {
            if (KeyPressed != null) {
                KeyPressed(this, e);
            }
        }
    }
}
