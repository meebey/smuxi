// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2014 jamesaxl
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
using System.Collections.Specialized;
using System.Text;

namespace Smuxi.SmuxiPasteBin
{
	public class Pastebin
	{
		public string ResponseString { get; set;}
		public Pastebin (NameValueCollection values)
		{
			using (var client = new WebClient())
			{
				var response = client.UploadValues("http://pastebin.com/api/api_post.php", values);
				ResponseString = Encoding.Default.GetString(response);
			}
		}
	}
}

