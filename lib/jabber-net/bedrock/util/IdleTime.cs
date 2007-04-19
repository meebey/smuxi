/* --------------------------------------------------------------------------
 * Copyrights
 *
 * Portions created by or assigned to Cursive Systems, Inc. are
 * Copyright (c) 2002-2007 Cursive Systems, Inc.  All Rights Reserved.  Contact
 * information for Cursive Systems, Inc. is available at
 * http://www.cursive.net/.
 *
 * License
 *
 * Jabber-Net can be used under either JOSL or the GPL.
 * See LICENSE.txt for details.
 * --------------------------------------------------------------------------*/
using System;
using System.Text;
using System.Runtime.InteropServices;

namespace bedrock.util
{
    /// <summary>
    /// TimeSpan event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="span"></param>
    public delegate void SpanEventHandler(object sender, TimeSpan span);

    /// <summary>
    /// Idle time calculations and notifications.
    /// </summary>
    public class IdleTime
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public int cbSize;
            public int dwTime;
        }

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// Get the number of seconds since last user input (mouse or keyboard) system-wide.
        /// </summary>
        /// <returns></returns>
        public static double GetIdleTime()
        {
            LASTINPUTINFO lii = new LASTINPUTINFO();
            lii.cbSize = Marshal.SizeOf(lii.GetType());
            if (!GetLastInputInfo(ref lii))
                throw new ApplicationException("Error executing GetLastInputInfo");
            return (Environment.TickCount - lii.dwTime) / 1000.0;
        }

        /// <summary>
        /// Fired when user has been idle (mouse, keyboard) for the configured number of seconds.
        /// </summary>
        public event SpanEventHandler OnIdle;

        /// <summary>
        /// Fired when the user comes back.
        /// </summary>
        public event SpanEventHandler OnUnIdle;

        private System.Timers.Timer m_timer = null;
        private int m_notifySecs;
        private int m_pollSecs;
        private bool m_idle = false;
        private DateTime m_idleStart = DateTime.MinValue;
        private System.ComponentModel.ISynchronizeInvoke m_invoker = null;

        /// <summary>
        /// Create an idle timer.  Make sure to set Enabled = true to start.
        /// </summary>
        /// <param name="pollSecs">Every pollSecs seconds, poll to see how long we've been away.</param>
        /// <param name="notifySecs">If we've been away notifySecs seconds, fire notification.</param>
        public IdleTime(int pollSecs, int notifySecs)
        {
            m_pollSecs = pollSecs;
            m_notifySecs = notifySecs;
            if (m_pollSecs > m_notifySecs)
                throw new ArgumentException("Poll more often than you notify.");
            m_timer = new System.Timers.Timer(m_pollSecs);
            m_timer.Elapsed += new System.Timers.ElapsedEventHandler(m_timer_Elapsed);
        }

        /// <summary>
        /// Is the timer running?
        /// </summary>
        public bool Enabled
        {
            get { return m_timer.Enabled; }
            set { m_timer.Enabled = value; }
        }

        /// <summary>
        /// Fire events in the GUI thread for this control.
        /// </summary>
        public System.ComponentModel.ISynchronizeInvoke InvokeControl
        {
            get { return m_invoker;  }
            set { m_invoker = value; }
        }

        private void m_timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            double idle = GetIdleTime();
            if (m_idle)
            {
                if (idle < m_pollSecs)
                {
                    m_idle = false;
                    if (OnUnIdle != null)
                    {
                        TimeSpan span = DateTime.Now - m_idleStart;
                        if ((m_invoker != null) &&
                            (m_invoker.InvokeRequired))
                        {
                            m_invoker.Invoke(OnUnIdle, new object[] { this, span });
                        }
                        else
                            OnUnIdle(this, span);
                    }
                    m_idleStart = DateTime.MinValue;
                }
            }
            else
            {
                if (idle > m_notifySecs)
                {
                    m_idle = true;
                    m_idleStart = DateTime.Now;
                    if (OnIdle != null)
                    {
                        TimeSpan span = new TimeSpan((long)(idle * 1000L));
                        if ((m_invoker != null) &&
                            (m_invoker.InvokeRequired))
                        {
                            m_invoker.Invoke(OnIdle, new object[] { this, span });
                        }
                        else
                            OnIdle(this, span);
                    }
                }
            }
        }
    }
}
