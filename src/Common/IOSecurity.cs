// This file is part of Smuxi and is licensed under the terms of MIT/X11
//
// Copyright (c) 2011 Mirco Bauer <meebey@meebey.net>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
