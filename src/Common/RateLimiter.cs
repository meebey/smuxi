// This file is part of Smuxi and is licensed under the terms of MIT/X11
//
// Copyright (c) 2014 Mirco Bauer <meebey@meebey.net>
// Copyright (c) 2014 Oliver Schneider <smuxi@oli-obk.de>
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

namespace Smuxi.Common
{
    public class RateLimiter
    {
        TimeSpan TimeWindow { get; set; }
        int CallCounter { get; set; }
        int CallLimit { get; set; }
        DateTime FirstCall { get; set; }
        object SyncRoot { get; set; }

        public bool IsRateLimited {
            get {
                return IsAboveLimit && IsInWindow;
            }
        }

        bool IsAboveLimit {
            get {
                return CallLimit <= CallCounter;
            }
        }

        bool IsInWindow {
            get {
                return (DateTime.UtcNow - FirstCall) < TimeWindow;
            }
        }

        public RateLimiter(int callLimit, TimeSpan timeWindow)
        {
            if (callLimit <= 0) {
                throw new ArgumentException("callLimit must be greater than 0.", "callLimit");
            }

            CallLimit = callLimit;
            TimeWindow = timeWindow;
            SyncRoot = new object();
        }

        public static RateLimiter operator ++(RateLimiter l)
        {
            if (l.IsRateLimited) {
                throw new InvalidOperationException("IsRateLimited must not be true.");
            }

            lock (l.SyncRoot) {
                if (!l.IsInWindow) {
                    l.CallCounter = 0;
                    l.FirstCall = DateTime.UtcNow;
                }

                l.CallCounter++;
            }
            return l;
        }
    }
}

