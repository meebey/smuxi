/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2010 Mirco Bauer <meebey@meebey.net>
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
using System.Text;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace Stfl
{
    internal class StflApi
    {
        public static bool IsXterm { get; private set; }
        static bool IsUtf8Locale { get; set; }
        static string EscapeLessThanCharacter  { get; set; }
        static string EscapeGreaterThanCharacter { get; set; }

        static StflApi()
        {
            IsXterm = Environment.GetEnvironmentVariable("TERM") == "xterm";
            // detect UTF-8 locale according to:
            // http://www.cl.cam.ac.uk/~mgk25/unicode.html#activate
            var locale = Environment.GetEnvironmentVariable("LC_ALL") ??
                         Environment.GetEnvironmentVariable("LC_LCTYPE") ??
                         Environment.GetEnvironmentVariable("LANG") ??
                         String.Empty;
            locale = locale.ToUpperInvariant();
            IsUtf8Locale = locale.Contains("UTF-8") || locale.Contains("UTF8");

            EscapeLessThanCharacter = "<>";
            EscapeGreaterThanCharacter = ">";
        }

        public static IntPtr ToUnixWideCharacters(string text)
        {
            if (text == null) {
                return IntPtr.Zero;
            }
            return UnixMarshal.StringToHeap(text, Encoding.UTF32);
        }

        public static string FromUnixWideCharacters(IntPtr text)
        {
            if (text == IntPtr.Zero) {
                return null;
            }
            return UnixMarshal.PtrToString(text, Encoding.UTF32);
        }

        public static string EscapeRichText(string text)
        {
            text = text.Replace("<", EscapeLessThanCharacter);
            text = text.Replace(">", EscapeGreaterThanCharacter);
            return text;
        }

        [DllImport("stfl")]
        static extern IntPtr stfl_create(IntPtr text);
        internal static IntPtr stfl_create(string text)
        {
            return stfl_create(ToUnixWideCharacters(text));
        }

        [DllImport("stfl")]
        internal static extern void stfl_free(IntPtr form);

        [DllImport("stfl", EntryPoint = "stfl_run")]
        static extern IntPtr stfl_run_native(IntPtr form, int timeout);
        internal static string stfl_run(IntPtr form, int timeout)
        {
            IntPtr res = stfl_run_native(form, timeout);
            if (res == IntPtr.Zero) {
                return null;
            }
            return UnixMarshal.PtrToString(res, Encoding.UTF32);
        }

        [DllImport("stfl")]
        internal static extern void stfl_reset();

        [DllImport("stfl")]
        static extern IntPtr stfl_get(IntPtr form, IntPtr name);
        internal static string stfl_get(IntPtr form, string text)
        {
            return FromUnixWideCharacters(
                stfl_get(form, ToUnixWideCharacters(text))
            );
        }

        [DllImport("stfl")]
        static extern void stfl_set(IntPtr form, IntPtr name, IntPtr value);
        internal static void stfl_set(IntPtr form, string name, string value)
        {
            stfl_set(form, ToUnixWideCharacters(name),
                     ToUnixWideCharacters(value));
        }
        
        [DllImport("stfl", EntryPoint = "stfl_get_focus")]
        static extern IntPtr stfl_get_focus_native(IntPtr form);
        internal static string stfl_get_focus(IntPtr form)
        {
            IntPtr res = stfl_get_focus_native(form);
            if (res == IntPtr.Zero) {
                return null;
            }
            return UnixMarshal.PtrToString(res, Encoding.UTF32);
        }
        
        [DllImport("stfl")]
        static extern void stfl_set_focus(IntPtr form, IntPtr name);
        internal static void stfl_set_focus(IntPtr form, string name)
        {
            stfl_set_focus(form, ToUnixWideCharacters(name));
        }

        [DllImport("stfl")]
        static extern IntPtr stfl_quote(IntPtr text);
        internal static string stfl_quote(string text) {
            return FromUnixWideCharacters(
                stfl_quote(ToUnixWideCharacters(text))
            );
        }

        [DllImport("stfl")]
        static extern IntPtr stfl_dump(IntPtr form, IntPtr name, IntPtr prefix,
                                       int focus);
        internal static string stfl_dump(IntPtr form, string name,
                                         string prefix, int focus)
        {
            return FromUnixWideCharacters(
                    stfl_dump(form, ToUnixWideCharacters(name),
                              ToUnixWideCharacters(prefix), focus)
            );
        }

        [DllImport("stfl")]
        static extern void stfl_modify(IntPtr form, IntPtr name, IntPtr mode,
                                       IntPtr text);
        internal static void stfl_modify(IntPtr form, string name, string mode,
                                         string text)
        {
            stfl_modify(form, ToUnixWideCharacters(name),
                        ToUnixWideCharacters(mode),
                        ToUnixWideCharacters(text));
        }

        [DllImport("stfl")]
        static extern IntPtr stfl_lookup(IntPtr form, IntPtr path,
                                         IntPtr newname);
        internal static string stfl_lookup(IntPtr form, string path,
                                           string newname)
        {
            return FromUnixWideCharacters(
                stfl_lookup(form, ToUnixWideCharacters(path),
                            ToUnixWideCharacters(newname))
            );
        }

        [DllImport("stfl", EntryPoint = "stfl_error")]
        static extern IntPtr stfl_error_native();
        internal static string stfl_error()
        {
            return FromUnixWideCharacters(stfl_error_native());
        }

        [DllImport("stfl")]
        static extern void stfl_error_action(IntPtr mode);
        internal static void stfl_error_action(string mode)
        {
            stfl_error_action(ToUnixWideCharacters(mode));
        }

        /*
        [DllImport("stfl")]
        internal static extern IntPtr stfl_ipool_create(IntPtr code);

        [DllImport("stfl")]
        internal static extern IntPtr stfl_ipool_add(IntPtr pool, IntPtr data);

        [DllImport("stfl")]
        internal static extern IntPtr stfl_ipool_towc(IntPtr pool, IntPtr buf);

        [DllImport("stfl")]
        internal static extern IntPtr stfl_ipool_fromwc(IntPtr pool, IntPtr buf);

        [DllImport("stfl")]
        internal static extern void stfl_ipool_flush(IntPtr pool);

        [DllImport("stfl")]
        internal static extern void stfl_ipool_destroy(IntPtr pool);
        */
    }       
}
