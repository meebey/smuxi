/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2007, 2010-2011 Mirco Bauer <meebey@meebey.net>
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
using System.Text;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Globalization;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [Serializable]
    public class ContactModel : ITraceable, ISerializable, IComparable<ContactModel>
    {
        private string          _ID;
        private string          _IdentityName;
        private string          _NetworkID;
        private string          _NetworkProtocol;
        private TextMessagePartModel _IdentityNameColored;

        public string ID {
            get {
                return _ID;
            }
        }
        
        public string IdentityName {
            get {
                return _IdentityName;
            }
            set {
                _IdentityName = value;
            }
        }

        public TextMessagePartModel IdentityNameColored {
            get {
                if (_IdentityNameColored == null) {
                    _IdentityNameColored = GetColoredIdentityName(_IdentityName,
                                                                  null);
                }
                return _IdentityNameColored;
            }
            set {
                _IdentityNameColored = value;
            }
        }
        
        public string NetworkID {
            get {
                return _NetworkID;
            }
        }
        
        public string NetworkProtocol {
            get {
                return _NetworkProtocol;
            }
        }
        
        public ContactModel(string id, string identityName,
                            string networkID, string networkProtocol)
        {
            Trace.Call(id, identityName, networkID, networkProtocol);
            
            if (id == null) {
                throw new ArgumentNullException("id");
            }
            if (identityName == null) {
                throw new ArgumentNullException("identityName");
            }
            if (networkID == null) {
                throw new ArgumentNullException("networkID");
            }
            if (networkProtocol == null) {
                throw new ArgumentNullException("networkProtocol");
            }
                
            _ID = id;
            _IdentityName = identityName;
            _NetworkID = networkID;
            _NetworkProtocol = networkProtocol;
        }
        
        protected ContactModel(SerializationInfo info, StreamingContext ctx)
        {
            if (info == null) {
                throw new ArgumentNullException("info");
            }

            SerializationReader sr = SerializationReader.GetReader(info);
            SetObjectData(sr);
        }
        
        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctx) 
        {
            if (info == null) {
                throw new ArgumentNullException("info");
            }

            SerializationWriter sw = SerializationWriter.GetWriter(); 
            GetObjectData(sw);
            sw.AddToInfo(info);
        }
        
        protected virtual void SetObjectData(SerializationReader sr)
        {
            if (sr == null) {
                throw new ArgumentNullException("sr");
            }

            _ID              = sr.ReadString();
            _IdentityName    = sr.ReadString();
            _NetworkID       = sr.ReadString();
            _NetworkProtocol = sr.ReadString();
        }
        
        protected virtual void GetObjectData(SerializationWriter sw)
        {
            if (sw == null) {
                throw new ArgumentNullException("sw");
            }

            sw.Write(_ID);
            sw.Write(_IdentityName);
            sw.Write(_NetworkID);
            sw.Write(_NetworkProtocol);
        }

        protected virtual TextMessagePartModel GetColoredIdentityName(
            string idendityName, string normalized)
        {
            var name =  new TextMessagePartModel(idendityName);
            if (normalized == null) {
                normalized = idendityName;
            }

            var crc = new Crc32();
            crc.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            var hash = crc.CrcValue;
            var upper24 = hash >> 8;
            /*
            var lower24 = hash & 0xFFFFFFU;
            var merged = upper24 ^ lower24;
            var rotated = (hash >> 16) | ((hash & 0xFFFFU) << 16);
            */
            uint flippedHash = (hash >> 16) | (hash << 16);
            var flippedMergedHash = (flippedHash >> 8) ^ (flippedHash & 0xFFFFFFU);
            name.ForegroundColor = new TextColor(upper24);
            name.BackgroundColor = new TextColor(flippedMergedHash);

            /*
            MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider();
            var md5hash = csp.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            var fgHash = BitConverter.ToUInt32(md5hash, 0);
            var bgHash = BitConverter.ToUInt32(md5hash, 4);
            name.ForegroundColor = new TextColor(fgHash >> 8);
            name.BackgroundColor = new TextColor(bgHash >> 8);
            */

            return name;
        }

        public virtual string ToTraceString()
        {
            return _NetworkID + "/" + _IdentityName; 
        }

        public virtual int CompareTo(ContactModel contact)
        {
            if (contact == null) {
                return 1;
            }

            return String.Compare(IdentityName, contact.IdentityName,
                                  true, CultureInfo.InvariantCulture);

        }

        public override bool Equals(object obj)
        {
            var value = obj as ContactModel;
            if (value == null) {
                return false;
            }
            return Equals(value);
        }

        public virtual bool Equals(ContactModel model)
        {
            if (model == null) {
                return false;
            }
            if (ID != model.ID) {
                return false;
            }
            if (NetworkID != model.NetworkID) {
                return false;
            }
            if (NetworkProtocol != model.NetworkProtocol) {
                return false;
            }
            return true;
        }

        public static bool operator ==(ContactModel a, ContactModel b)
        {
            if (System.Object.ReferenceEquals(a, b)) {
                return true;
            }

            if ((object) a == null || (object) b == null) {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(ContactModel a, ContactModel b)
        {
            return !(a == b);
        }
    }
}
