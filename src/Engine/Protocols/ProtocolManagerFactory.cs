/*
 * $Id: IrcProtocolManager.cs 149 2007-04-11 16:47:52Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/IrcProtocolManager.cs $
 * $Rev: 149 $
 * $Author: meebey $
 * $Date: 2007-04-11 18:47:52 +0200 (Wed, 11 Apr 2007) $
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
using System.Reflection;
using System.Collections.Generic;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class ProtocolManagerFactory
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private IDictionary<ProtocolManagerInfoModel, Type> _ProtocolManagerTypes = new Dictionary<ProtocolManagerInfoModel, Type>();

        public IList<ProtocolManagerInfoModel> ProtocolManagerInfos {
            get {
                return new List<ProtocolManagerInfoModel>(_ProtocolManagerTypes.Keys);
            }
        }
        
        public ProtocolManagerFactory()
        {
        }
        
        public void LoadProtocolManager(string filename)
        {
            Trace.Call(filename);
            
            Assembly asm = Assembly.LoadFile(filename);
            Type[] types;
            try {
                types = asm.GetTypes();
            } catch (ReflectionTypeLoadException ex) {
#if LOG4NET
                _Logger.ErrorFormat(
                    "LoadProtocolManager(): GetTypes() on {0} threw exceptions",
                    filename
                );
                foreach (var loaderEx in ex.LoaderExceptions) {
                    _Logger.Error(
                        "LoadProtocolManager(): LoaderException: ",
                        loaderEx
                    );
                    _Logger.Error(
                        "LoadProtocolManager(): LoaderException.InnerException: ",
                        loaderEx.InnerException
                    );
                }
#endif
                throw;
            }
            
            foreach (Type type in types) {
                if (type.IsAbstract) {
                    continue;
                }
                
                Type foundType = null;
                Type[] interfaceTypes = type.GetInterfaces();
                foreach (Type interfaceType in interfaceTypes) {
                    if (interfaceType == typeof(IProtocolManager)) {
#if LOG4NET
                        _Logger.Debug("LoadProtocolManager(): found " + type);
#endif
                        foundType = type;
                        break;
                    }
                }
                
                if (foundType == null) {
                    continue;
                }
                
                // let's get the info attribute
                object[] attrs = foundType.GetCustomAttributes(typeof(ProtocolManagerInfoAttribute), true);
                if (attrs == null || attrs.Length == 0) {
                    throw new ArgumentException("Assembly contains IProtocolManager but misses ProtocolManagerInfoAttribute", "filename");
                    //continue;
                }
                
                ProtocolManagerInfoAttribute attr = (ProtocolManagerInfoAttribute) attrs[0];
                ProtocolManagerInfoModel info = new ProtocolManagerInfoModel(attr.Name, attr.Description, attr.Alias);
                
                _ProtocolManagerTypes.Add(info, foundType);
            }
        }

        public void LoadAllProtocolManagers(string path)
        {
            Trace.Call(path);
            
            string[] filenames = Directory.GetFiles(path, "smuxi-engine-*.dll");
            foreach (string filename in filenames) {
                LoadProtocolManager(filename);
            }
        }
        
        public ProtocolManagerInfoModel GetProtocolManagerInfoByAlias(string alias)
        {
            foreach (ProtocolManagerInfoModel info in _ProtocolManagerTypes.Keys) {
                if (info.Alias.Equals(alias, StringComparison.InvariantCultureIgnoreCase)) {
                    return info;
                }
            }
            
            return null;
        }
        
        public IList<string> GetProtocols()
        {
            IList<string> protocols = new List<string>();
            foreach (ProtocolManagerInfoModel info in _ProtocolManagerTypes.Keys) {
                if (!protocols.Contains(info.Name)) {
                    protocols.Add(info.Name);
                }
            }
            return protocols;
        }
        
        public IProtocolManager CreateProtocolManager(ProtocolManagerInfoModel info, Session session)
        {
            if (info == null) {
                throw new ArgumentNullException("info");
            }
            if (session == null) {
                throw new ArgumentNullException("session");
            }
            
            Type type = _ProtocolManagerTypes[info];
            return (IProtocolManager) Activator.CreateInstance(type, session);
        }
    }
}
