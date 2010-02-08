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
 * Copyright (c) 2010 Andrius Bentkus <Andrius.Bentkus@rwth-aachen.de>
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

namespace Smuxi.Frontend.Stfl
{
    internal class StlfApi
    {
        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_create(IntPtr text);

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_free(IntPtr form);

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_run(IntPtr form, int timeout);

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_reset();

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_get(IntPtr form, IntPtr name);

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_set(IntPtr form, IntPtr name, IntPtr val);

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_focus(IntPtr form);

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_quote(IntPtr text);

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_dump(IntPtr form, IntPtr name, IntPtr prefix, int focus);

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_modify_before(IntPtr w, IntPtr n);

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_modify_after(IntPtr w, IntPtr n);

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_modify_insert(IntPtr w, IntPtr n);

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_modify_append(IntPtr w, IntPtr n);

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_modify(IntPtr w, IntPtr n);

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_lookup(IntPtr f, IntPtr path, IntPtr newname);

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_error();

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_error_action(IntPtr mode);

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_ipool_create(IntPtr code);

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_ipool_add(IntPtr pool, IntPtr data);

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_ipool_towc(IntPtr pool, IntPtr buf);

        [DllImport("libstfl.so.0.21")]
        internal static extern IntPtr stfl_ipool_fromwc(IntPtr pool, IntPtr buf);

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_ipool_flush(IntPtr pool);

        [DllImport("libstfl.so.0.21")]
        internal static extern void stfl_ipool_destroy(IntPtr pool);
    }       
}
