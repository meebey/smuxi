// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
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
using System.Runtime.InteropServices;

namespace Smuxi.Engine
{
    enum CertificateFormat : int {
        DER = 0,
        PEM = 1
    }

    /*
    typedef struct
    {
        unsigned char *data;
        unsigned int size;
    } gnutls_datum_t;
    */
    [StructLayout(LayoutKind.Sequential)]
    public struct Datum {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
        public byte[] Data;
        public uint Size;
    }

    public class GnuTls
    {
        const string LIB_NAME = "libgnutls.so.26";

        static GnuTls()
        {
            gnutls_global_init();
        }

        [DllImport(LIB_NAME)]
        internal static extern void gnutls_global_init();

        [DllImport(LIB_NAME)]
        // int gnutls_x509_crt_init (gnutls_x509_crt_t * cert)
        internal static extern int gnutls_x509_crt_init(out IntPtr cert);

        [DllImport(LIB_NAME)]
        // int gnutls_x509_crt_import (gnutls_x509_crt_t cert, const gnutls_datum_t * data, gnutls_x509_crt_fmt_t format)
        internal static extern int gnutls_x509_crt_import(IntPtr cert, ref Datum data, int format);
    }
}
