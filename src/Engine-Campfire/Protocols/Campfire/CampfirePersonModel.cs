// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012 Carlos Martín Nieto <cmn@dwim.me>

// 
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
using System;
using System.Net;
using System.Runtime.Serialization;
using Smuxi.Common;
using Smuxi.Engine.Campfire;

namespace Smuxi.Engine.Campfire
{
    [Serializable]
    internal class CampfirePersonModel : PersonModel
    {
        public int Uid { get; internal set; }
        public string Ident { get; internal set; }
        public string Host { get; internal set; }
        public string Name {get; internal set; }
        public string Email {get; internal set; }
        public bool Admin {get; internal set; }
        public string AvatarUrl {get; internal set; }

        internal protected CampfirePersonModel(User user, string network, IProtocolManager pm)
            : base(user.Id.ToString(), user.Name, network, "Campfire", pm)
        {
            Uid = user.Id;
            Name = user.Name;
            Email = user.Email_Address;
            Admin = user.Admin;
            AvatarUrl = user.Avatar_Url;
            Host = network;
            Ident = Name;
        }

        internal protected CampfirePersonModel(SerializationInfo info,
                                          StreamingContext ctx) :
                                     base(info, ctx)
        {
        }

    }
}

