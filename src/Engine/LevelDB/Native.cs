// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2012 Mirco Bauer <meebey@meebey.net>
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

namespace LevelDB
{
    internal static class Native
    {
        static void CheckError(string error)
        {
            if (String.IsNullOrEmpty(error)) {
                return;
            }
            throw new ApplicationException(error);
        }

        // extern leveldb_t* leveldb_open(const leveldb_options_t* options, const char* name, char** errptr);
        [DllImport("leveldb")]
        public static extern IntPtr leveldb_open(IntPtr options, string name, out string error);
        public static IntPtr leveldb_open(IntPtr options, string name)
        {
            string error;
            var db = leveldb_open(options, name, out error);
            CheckError(error);
            return db;
        }

        [DllImport("leveldb")]
        public static extern void leveldb_close(IntPtr db);

        // extern void leveldb_put(leveldb_t* db, const leveldb_writeoptions_t* options, const char* key, size_t keylen, const char* val, size_t vallen, char** errptr);
        [DllImport("leveldb")]
        public static extern void leveldb_put(IntPtr db,
                                              IntPtr writeOptions,
                                              string key,
                                              UIntPtr keyLength,
                                              string value,
                                              UIntPtr valueLength,
                                              out string error);
        public static void leveldb_put(IntPtr db,
                                       IntPtr writeOptions,
                                       string key,
                                       string value)
        {
            string error;
            // FIXME: bytes vs chars, we need to pass bytes here
            var keyLength = new UIntPtr((uint) key.Length);
            var valueLength = new UIntPtr((uint) value.Length);
            Native.leveldb_put(db, writeOptions,
                               key, keyLength,
                               value, valueLength, out error);
            CheckError(error);
        }

        [DllImport("leveldb")]
        public static extern void leveldb_delete(IntPtr db, IntPtr options, string key, UIntPtr keylen, out string error);

        [DllImport("leveldb")]
        public static extern void leveldb_write(IntPtr db, IntPtr options, IntPtr batch, out string error);

        [DllImport("leveldb")]
        public static extern IntPtr leveldb_get(IntPtr db,
                                                IntPtr readOptions,
                                                string key,
                                                UIntPtr keyLength,
                                                out UIntPtr valueLength,
                                                out string error);
        public static string leveldb_get(IntPtr db,
                                         IntPtr readOptions,
                                         string key)
        {
            UIntPtr valueLength;
            string error;
            var keyLength =  new UIntPtr((uint) key.Length);
            var valuePtr = leveldb_get(db, readOptions, key, keyLength,
                                       out valueLength, out error);
            CheckError(error);
            if (valuePtr == IntPtr.Zero || valueLength == UIntPtr.Zero) {
                return null;
            }
            return Marshal.PtrToStringAnsi(valuePtr, (int) valueLength);
        }

        [DllImport("leveldb")]
        public static extern IntPtr leveldb_create_iterator(IntPtr db, IntPtr readOptions);

        /* Options */
        [DllImport("leveldb")]
        public static extern IntPtr leveldb_options_create();

        [DllImport("leveldb")]
        public static extern void leveldb_options_destroy(IntPtr options);

        [DllImport("leveldb")]
        public static extern void leveldb_options_set_comparator(IntPtr options, IntPtr comparator);

        [DllImport("leveldb")]
        public static extern void leveldb_options_set_create_if_missing(IntPtr options, char value);

        [DllImport("leveldb")]
        public static extern void leveldb_options_set_error_if_exists(IntPtr options, char value);

        [DllImport("leveldb")]
        public static extern void leveldb_options_set_paranoid_checks(IntPtr options, char value);

        // extern void leveldb_options_set_max_open_files(leveldb_options_t*, int);
        [DllImport("leveldb")]
        public static extern void leveldb_options_set_max_open_files(IntPtr options, int value);

        // TODO:
        /*
        extern void leveldb_options_set_env(leveldb_options_t*, leveldb_env_t*);
        extern void leveldb_options_set_info_log(leveldb_options_t*, leveldb_logger_t*);
        extern void leveldb_options_set_write_buffer_size(leveldb_options_t*, size_t);
        extern void leveldb_options_set_cache(leveldb_options_t*, leveldb_cache_t*);
        extern void leveldb_options_set_block_size(leveldb_options_t*, size_t);
        extern void leveldb_options_set_block_restart_interval(leveldb_options_t*, int);
        enum {
          leveldb_no_compression = 0,
          leveldb_snappy_compression = 1
        };
        extern void leveldb_options_set_compression(leveldb_options_t*, int);
        */

        [DllImport("leveldb")]
        public static extern IntPtr leveldb_readoptions_create();

        [DllImport("leveldb")]
        public static extern void leveldb_readoptions_destroy(IntPtr readOptions);
        /*
        extern void leveldb_readoptions_set_verify_checksums(
            leveldb_readoptions_t*,
            unsigned char);
        extern void leveldb_readoptions_set_fill_cache(
            leveldb_readoptions_t*, unsigned char);
        extern void leveldb_readoptions_set_snapshot(
            leveldb_readoptions_t*,
            const leveldb_snapshot_t*);
        */

        [DllImport("leveldb")]
        public static extern IntPtr leveldb_writeoptions_create();

        [DllImport("leveldb")]
        public static extern void leveldb_writeoptions_destroy(IntPtr writeOptions);
        /*
        extern void leveldb_writeoptions_set_sync(
            leveldb_writeoptions_t*, unsigned char);
        */

/* Iterator */
        [DllImport("leveldb")]
        public static extern void leveldb_iter_seek_to_first(IntPtr iter);

        [DllImport("leveldb")]
        public static extern void leveldb_iter_seek_to_last(IntPtr iter);

        [DllImport("leveldb")]
        public static extern bool leveldb_iter_valid(IntPtr iter);

        [DllImport("leveldb")]
        public static extern void leveldb_iter_next(IntPtr iter);

        // extern const char* leveldb_iter_key(const leveldb_iterator_t*, size_t* klen);
        [DllImport("leveldb")]
        public static extern IntPtr leveldb_iter_key(IntPtr iter, out UIntPtr keyLength);
        public static string leveldb_iter_key(IntPtr iter)
        {
            UIntPtr keyLength;
            var keyPtr = leveldb_iter_key(iter, out keyLength);
            if (keyPtr == IntPtr.Zero || keyLength == UIntPtr.Zero) {
                return null;
            }
            return Marshal.PtrToStringAnsi(keyPtr, (int) keyLength);
        }

        // extern const char* leveldb_iter_value(const leveldb_iterator_t*, size_t* vlen);
        [DllImport("leveldb")]
        public static extern IntPtr leveldb_iter_value(IntPtr iter, out UIntPtr valueLength);
        public static string leveldb_iter_value(IntPtr iter)
        {
            UIntPtr valueLength;
            var valuePtr = leveldb_iter_value(iter, out valueLength);
            if (valuePtr == IntPtr.Zero || valueLength == UIntPtr.Zero) {
                return null;
            }
            return Marshal.PtrToStringAnsi(valuePtr, (int) valueLength);
        }

        // extern void leveldb_iter_destroy(leveldb_iterator_t*);
        [DllImport("leveldb")]
        public static extern void leveldb_iter_destroy(IntPtr iter);

        /*
extern unsigned char leveldb_iter_valid(const leveldb_iterator_t*);
extern void leveldb_iter_seek_to_first(leveldb_iterator_t*);
extern void leveldb_iter_seek_to_last(leveldb_iterator_t*);
extern void leveldb_iter_seek(leveldb_iterator_t*, const char* k, size_t klen);
extern void leveldb_iter_next(leveldb_iterator_t*);
extern void leveldb_iter_prev(leveldb_iterator_t*);
extern void leveldb_iter_get_error(const leveldb_iterator_t*, char** errptr);
         */
    }
}
