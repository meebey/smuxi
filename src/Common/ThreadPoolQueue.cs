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
using System.Threading;
using System.Collections.Generic;

namespace Smuxi.Common
{
    public class ThreadPoolQueue
    {
        public int MaxWorkers { set; get; }
        Queue<Action> ActionQueue { set; get; }
        int ActiveWorkers;

        public ThreadPoolQueue()
        {
            MaxWorkers = Environment.ProcessorCount;
            ActionQueue = new Queue<Action>();
        }

        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        public void Enqueue(Action action)
        {
            if (action == null) {
                throw new ArgumentNullException("action");
            }

            lock (ActionQueue) {
                ActionQueue.Enqueue(action);
            }

            CheckQueue();
        }

        void CheckQueue()
        {
            var action = Dequeue();
            if (action == null) {
                return;
            }

            Interlocked.Increment(ref ActiveWorkers);

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    action();
                } finally {
                    Interlocked.Decrement(ref ActiveWorkers);
                    CheckQueue();
                }
            });
        }

        Action Dequeue()
        {
            lock (ActionQueue) {
                if (ActionQueue.Count == 0) {
                    return null;
                }

                if (ActiveWorkers >= MaxWorkers) {
                    return null;
                }

                return ActionQueue.Dequeue();
            }
        }
    }
}
