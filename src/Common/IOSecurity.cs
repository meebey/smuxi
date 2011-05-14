// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2011 Mirco Bauer <meebey@meebey.net>
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
using System.IO;

namespace Smuxi.Common
{
    public static class IOSecurity
    {
#if LOG4NET
        private static readonly log4net.ILog f_Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif

        public static string GetFilteredPath(string path)
        {
            if (path == null) {
                throw new ArgumentNullException("path");
            }
            if (path.Trim().Length == 0) {
                throw new ArgumentException("Argument must not be empty.",
                                            "path");
            }

            if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1) {
#if LOG4NET
                f_Logger.Debug(
                    "GetFilteredPath(): path: '" +
                    path + "' contains invalid chars, removing them!"
                );
#endif
                // remove invalid chars
                foreach (char invalidChar in Path.GetInvalidPathChars()) {
                    path = path.Replace(invalidChar.ToString(), String.Empty);
                }
            }
            return path;
        }

        public static string GetFilteredFileName(string fileName)
        {
            return GetFilteredFileName(fileName, true);
        }

        public static string GetFilteredFileName(string fileName,
                                                 bool filterSpaces)
        {
            if (fileName == null) {
                throw new ArgumentNullException("fileName");
            }
            if (fileName.Trim().Length == 0) {
                throw new ArgumentException("Argument must not be empty.",
                                            "fileName");
            }

            if (filterSpaces) {
                fileName = fileName.Replace(" ", "_");
            }
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1) {
#if LOG4NET
                f_Logger.Debug(
                    "GetValidFilename(): filename: '" + fileName + "' contains " +
                     "invalid chars, removing them!"
                );
#endif
                // remove invalid chars
                foreach (char invalidChar in Path.GetInvalidFileNameChars()) {
                    fileName = fileName.Replace(invalidChar.ToString(),
                                                String.Empty);
                }
            }
            fileName = fileName.Replace("..", String.Empty);
            return fileName;
        }
    }
}
