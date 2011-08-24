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
            lock (ActionQueue) {
                if (ActionQueue.Count == 0) {
                    return;
                }

                if (ActiveWorkers >= MaxWorkers) {
                    return;
                }

                var action = ActionQueue.Dequeue();
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
        }
    }
}
