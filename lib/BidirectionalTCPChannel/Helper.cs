// Copyright (c) 2002 Ingo Rammer
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.

/*
 * And now for a little bit of advertisment:
 * 
 *    I'm consultant, author, speaker, trainer and developer - mainly for distributed 
 *	  .NET applications. I'm available for technical and architectural consulting,  
 *    on-site training and development throughout Europe. 
 * 
 *	  If you currently look at developing a distributed .NET application, 
 *    think about designing/implementing an application framework, or just like
 *    what you see here and think that you need a hardcore .NET person on your 
 *    project, please don't hesitate to contact me at rammer@sycom.at.
 *
 *    My services include: custom training, design, prototyping & architecural review.
 *    I'm not normally available for long term project development. But if you are
 *    working on something exceptionally interesting - who knows? ;-)
 * 
 *												Ingo Rammer
 *												rammer@sycom.at
 *												http://www.dotnetremoting.cc
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;

namespace DotNetRemotingCC.Channels.BidirectionalTCP
{
	public class Helper
	{
		private static String _hostname;
		private static String _IPAddress;
		private static String _machineName;

		private static Guid _myGuid = Guid.Empty;

		public const String TCPGUID_PREFIX = "bidirtcpguid://";
		public const String TCP_PREFIX = "bidirtcp://";

		/// <summary>
		/// GUID which will be used for addressing replies. Is only valid for a single session.
		/// </summary>
		public static Guid GetMyGUID() 
		{
			if (_myGuid == Guid.Empty) 
			{
				_myGuid = Guid.NewGuid();
			}
			return _myGuid;
		}

		internal static String ParseURL(String url, out String objectURI)
        {    
            objectURI = null;
            int pos;

            if (url.ToLower().StartsWith(TCP_PREFIX))
            {
                pos = TCP_PREFIX.Length; // 11
            }
            else
            {	
				return ParseGuidURL(url,out objectURI);
			}

            pos = url.IndexOf('/', pos);
            if (pos == -1)
            {
                return url; // no ObjectURI specified, only direct URL
            }

            String baseURL = url.Substring(0, pos);
            objectURI = url.Substring(pos); 

            return baseURL;
        }

		internal static String ParseGuidURL(String url, out String objectURI)
		{
			
			objectURI = null;
			int pos;

			if (url.ToLower().StartsWith(TCPGUID_PREFIX))
			{
				pos = TCPGUID_PREFIX.Length; 
			}
			else
			{	
				return null;
			}

			pos = url.IndexOf('/', pos);
			if (pos == -1)
			{
				return url; // no ObjectURI specified, only direct URL
			}

			String baseURL = url.Substring(0, pos);
			objectURI = url.Substring(pos); 

			return baseURL;
		}

		internal static void SplitURL(String url, out String host, out String GUID, out int port, out String objectURI)
		{

			if (url.ToLower().StartsWith(TCP_PREFIX)) 
			{
				String baseurl = ParseURL(url,out objectURI);
				
				// baseurl ==> "bidirtcp://hostname:port"

				baseurl = baseurl.Substring(11);

				// baseurl ==> "hostname:port"

				int pos = baseurl.IndexOf(":");
				if (pos == -1) 
				{
					throw new Exception("tcpbidir:// URLs must specify a port number!");
				}
				host = baseurl.Substring(0,pos);
				port = int.Parse(baseurl.Substring(pos+1));
				GUID = null;
			} 
			else 
			{
				String baseurl = ParseGuidURL(url,out objectURI);
				// baseurl ==> "bidirtcpguid://23423-423-423-4-234-234" (or however a GUID should look like ;-))
				GUID = baseurl.Substring(TCPGUID_PREFIX.Length);
				host = null;
				port = 0;
			}
		}

		internal static String GetHostName()
        {
            if (_hostname == null)
            {
                _hostname = Dns.GetHostName();
            }
            return _hostname;
        } 

        internal static String GetMachineName()
        {
            if (_machineName == null)
            {     
                _machineName = Dns.GetHostByName(GetHostName()).HostName;
            }
            return _machineName;      
        } 

        internal static String GetIPAddress()
        {
            if (_IPAddress == null)
            {            
                IPHostEntry ips = Dns.GetHostByName(GetMachineName());
                _IPAddress = ips.AddressList[0].ToString();
            }
            
            return _IPAddress;      
        } 

	}
}
