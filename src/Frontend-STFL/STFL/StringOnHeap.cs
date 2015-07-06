/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2010-2015 Mirco Bauer <meebey@meebey.net>
 * Copyright (c) 2015 Ondrej Hosek <ondra.hosek@gmail.com>
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
using Mono.Unix;

namespace Stfl
{
    public sealed class StringOnHeap : IDisposable
    {
        public IntPtr Pointer { get; private set; }

        public StringOnHeap(string str, Encoding encoding)
        {
            if (str == null) {
                Pointer = IntPtr.Zero;
                return;
            }
            Pointer = UnixMarshal.StringToHeap(str, encoding);
        }

        public void Dispose()
        {
            if (Pointer != IntPtr.Zero) {
                UnixMarshal.FreeHeap(Pointer);
                Pointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        ~StringOnHeap()
        {
            if (Pointer != IntPtr.Zero) {
                UnixMarshal.FreeHeap(Pointer);
            }
        }
    }
}

