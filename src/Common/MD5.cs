/*
 * $Id: Page.cs 111 2006-02-20 23:10:45Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GtkGnome/Page.cs $
 * $Rev: 111 $
 * $Author: meebey $
 * $Date: 2006-02-21 00:10:45 +0100 (Tue, 21 Feb 2006) $
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
using System.Text;
using System.Security.Cryptography;

namespace Smuxi.Common
{
    public class MD5
    {
        public static string FromString(string cleartext)
        {
            MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider();
            byte[] md5bytes = csp.ComputeHash(Encoding.UTF8.GetBytes(cleartext));
            StringBuilder md5text = new StringBuilder();
            foreach (byte md5byte in md5bytes) {
                md5text.Append(md5byte.ToString("x2").ToLower());
            }
            return md5text.ToString();
        }
    }
}
