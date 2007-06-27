/*
 * $Id: TestUI.cs 179 2007-04-21 15:01:29Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-Test/TestUI.cs $
 * $Rev: 179 $
 * $Author: meebey $
 * $Date: 2007-04-21 17:01:29 +0200 (Sat, 21 Apr 2007) $
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
using System.IO;
using System.Reflection;
//using Stfl;
using Smuxi.Common;

namespace Smuxi.Frontend.Stfl
{
    public class Form : IDisposable
    {
        private stfl_form _StflForm;
        
        public event KeyPressedEventHandler KeyPressed;
        
        public string this[string name] {
            get {
                return _StflForm.get(name);
            }
            set {
                _StflForm.set(name, value);
            }
        }
        
        public Form(string text)
        {
            _CreateForm(text);
        }
        
        public Form(Assembly assembly, string resourceName)
        {
            if (assembly == null) {
                assembly = Assembly.GetCallingAssembly();
            }

            using (Stream str = assembly.GetManifestResourceStream(resourceName)) {
                if (str == null) {
                    throw new ArgumentException(resourceName + " could not be found in assembly", "resourceName");
                }
                StreamReader reader = new StreamReader(str);
                string text = reader.ReadToEnd();
                reader.Dispose();
                _CreateForm(text);
            }
        }
        
        private void _CreateForm(string text)
        {
            _StflForm = new stfl_form(text);
        }

        private void _DestroyForm()
        {
            _StflForm.Dispose();
        }
        
        public virtual void Dispose()
        {
            _DestroyForm();
        }
        
        public virtual void Run(int timeout)
        {
            string @event = _StflForm.run(timeout);
            ProcessEvent(@event);
        }

        public void Run()
        {
            Run(0);
        }
        
        public void Modify(string name, string mode, string text)
        {
            _StflForm.modify(name, mode, text);
        }
        
        protected virtual void ProcessEvent(string key)
        {
            if (key != null && key != "TIMEOUT") {
                ProcessKey(key);
            }
        }
        
        protected virtual void ProcessKey(string key)
        {
            Trace.Call(key);
            
            string focus = _StflForm.get_focus();
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
