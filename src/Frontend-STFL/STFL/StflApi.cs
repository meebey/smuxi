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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Unix;

namespace Stfl
{
    internal class StflApi
    {
        public static bool IsXterm { get; private set; }
        static bool IsUtf8Locale { get; set; }
        static string EscapeLessThanCharacter  { get; set; }
        static string EscapeGreaterThanCharacter { get; set; }
        static Encoding Utf32NativeEndian { get; set; }
        static bool BrokenUtf32Handling { get; set; }

        static StflApi()
        {
            // check if he has a graphical terminal. screen/tmux in not
            // detected in case someone is using it in pure text mode
            string termName = Environment.GetEnvironmentVariable("TERM");
            IsXterm = (termName != null && (termName.StartsWith("xterm") ||
                termName.StartsWith("rxvt")));
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

            Utf32NativeEndian = new UTF32Encoding(
                bigEndian: !BitConverter.IsLittleEndian,
                byteOrderMark: false,
                throwOnInvalidCharacters: true
            );

            // UTF-32 handling is broken in mono < 4.2
            // fix in 4.4: https://github.com/mono/mono/commit/6bfb7e6d149f5e5c0fe04d680e3f7d36769ef541
            // fix in 4.2: https://github.com/mono/mono/commit/ea4ed4a47b98832e294d166bee5b8301fe87e216
            BrokenUtf32Handling = IsMonoVersionLessThan(4, 2);
        }

        static bool IsMonoVersionLessThan(int majorVersion, int minorVersion)
        {
            var monoRuntimeType = Type.GetType("Mono.Runtime");
            if (monoRuntimeType != null) {
                var monoRuntimeVersionMethod = monoRuntimeType.GetMethod(
                    "GetDisplayName",
                    BindingFlags.NonPublic | BindingFlags.Static
                );
                if (monoRuntimeVersionMethod != null) {
                    var version = (string)monoRuntimeVersionMethod.Invoke(null, null);
                    var versionPieces = version.Split(new[] { ' ' }, 2);
                    var versionNumberPieces = versionPieces [0].Split(new[] { '.' });

                    int runtimeMajorVersion, runtimeMinorVersion;
                    int.TryParse(versionNumberPieces [0], out runtimeMajorVersion);
                    int.TryParse(versionNumberPieces[1], out runtimeMinorVersion);

                    if (runtimeMajorVersion < majorVersion) {
                        return true;
                    }
                    if (runtimeMajorVersion == majorVersion && runtimeMinorVersion < minorVersion) {
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }

        internal static string PtrToUtf32String(IntPtr ptr)
        {
            // calculate length
            int length = 0;
            while (Marshal.ReadInt32(ptr, 4 * length) != 0) {
                ++length;
            }

            // read the bytes
            var utf32Bytes = new byte[4 * length];
            Marshal.Copy(ptr, utf32Bytes, 0, utf32Bytes.Length);

            // decode to string
            return Utf32NativeEndian.GetString(utf32Bytes);
        }

        public static StringOnHeap ToUnixWideCharacters(string text)
        {
            return new StringOnHeap(text, Utf32NativeEndian);
        }

        public static string FromUnixWideCharacters(IntPtr text)
        {
            if (text == IntPtr.Zero) {
                return null;
            }
            if (BrokenUtf32Handling) {
                return PtrToUtf32String(text);
            } else {
                return UnixMarshal.PtrToString(text, Utf32NativeEndian);
            }
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
            using (var heapText = ToUnixWideCharacters(text)) {
                return stfl_create(heapText.Pointer);
            }
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
            return FromUnixWideCharacters(res);
        }

        [DllImport("stfl")]
        internal static extern void stfl_reset();

        [DllImport("stfl")]
        static extern IntPtr stfl_get(IntPtr form, IntPtr name);
        internal static string stfl_get(IntPtr form, string text)
        {
            using (var heapText = ToUnixWideCharacters(text)) {
                return FromUnixWideCharacters(
                    stfl_get(form, heapText.Pointer)
                );
            }
        }

        [DllImport("stfl")]
        static extern void stfl_set(IntPtr form, IntPtr name, IntPtr value);
        internal static void stfl_set(IntPtr form, string name, string value)
        {
            using (var heapName = ToUnixWideCharacters(name))
            using (var heapValue = ToUnixWideCharacters(value)) {
                stfl_set(form, heapName.Pointer, heapValue.Pointer);
            }
        }
        
        [DllImport("stfl", EntryPoint = "stfl_get_focus")]
        static extern IntPtr stfl_get_focus_native(IntPtr form);
        internal static string stfl_get_focus(IntPtr form)
        {
            IntPtr res = stfl_get_focus_native(form);
            return FromUnixWideCharacters(res);
        }
        
        [DllImport("stfl")]
        static extern void stfl_set_focus(IntPtr form, IntPtr name);
        internal static void stfl_set_focus(IntPtr form, string name)
        {
            using (var heapName = ToUnixWideCharacters(name)) {
                stfl_set_focus(form, heapName.Pointer);
            }
        }

        [DllImport("stfl")]
        static extern IntPtr stfl_quote(IntPtr text);
        internal static string stfl_quote(string text)
        {
            using (var heapText = ToUnixWideCharacters(text)) {
                return FromUnixWideCharacters(
                    stfl_quote(heapText.Pointer)
                );
            }
        }

        [DllImport("stfl")]
        static extern IntPtr stfl_dump(IntPtr form, IntPtr name, IntPtr prefix,
                                       int focus);
        internal static string stfl_dump(IntPtr form, string name,
                                         string prefix, int focus)
        {
            using (var heapName = ToUnixWideCharacters(name))
            using (var heapPrefix = ToUnixWideCharacters(prefix)) {
                return FromUnixWideCharacters(
                    stfl_dump(form, heapName.Pointer, heapPrefix.Pointer, focus)
                );
            }
        }

        [DllImport("stfl")]
        static extern void stfl_modify(IntPtr form, IntPtr name, IntPtr mode,
                                       IntPtr text);
        internal static void stfl_modify(IntPtr form, string name, string mode,
                                         string text)
        {
            using (var heapName = ToUnixWideCharacters(name))
            using (var heapMode = ToUnixWideCharacters(mode))
            using (var heapText = ToUnixWideCharacters(text)) {
                stfl_modify(form, heapName.Pointer, heapMode.Pointer,
                    heapText.Pointer);
            }
        }

        [DllImport("stfl")]
        static extern IntPtr stfl_lookup(IntPtr form, IntPtr path,
                                         IntPtr newname);
        internal static string stfl_lookup(IntPtr form, string path,
                                           string newname)
        {
            using (var heapPath = ToUnixWideCharacters(path))
            using (var heapNewName = ToUnixWideCharacters(newname)) {
                return FromUnixWideCharacters(
                    stfl_lookup(form, heapPath.Pointer, heapNewName.Pointer)
                );
            }
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
            using (var heapMode = ToUnixWideCharacters(mode)) {
                stfl_error_action(heapMode.Pointer);
            }
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
