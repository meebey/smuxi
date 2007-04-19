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
using System.Collections;
using System.IO;

using bedrock.util;

namespace bedrock.io
{
    /// <summary> Aggregate byte arrays together, so we can parse
    /// across IP packet boundaries
    /// </summary>
    [SVN(@"$Id: BufferAggregate.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class BufferAggregate
    { // RingBuffer of the Nieblung
        private class BufNode
        {
            public byte[] buf;
            public BufNode next = null;
        }

        private MemoryStream m_stream = new MemoryStream();
        private BufNode m_head = null;
        private BufNode m_tail = null;

        /// <summary>
        /// Create an empty buffer
        /// </summary>
        public BufferAggregate()
        {
        }

        /// <summary>
        /// Write to the buffer.  Please make sure that you won't use this memory any more after you hand it in.
        /// It will get mangled.
        /// </summary>
        /// <param name="buf"></param>
        public void Write(byte[] buf)
        {
            m_stream.Write(buf, 0, buf.Length);
            if (m_tail == null)
            {
                m_head = m_tail = new BufNode();
                m_head.buf = buf;
            }
            else
            {
                BufNode n = new BufNode();
                n.buf = buf;
                m_tail.next = n;
                m_tail = n;
            }
        }

        /// <summary>
        /// Get the current aggregate contents of the buffer.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBuffer()
        {
            return m_stream.ToArray();
        }

        /// <summary>
        /// Clear the first "offset" bytes of the buffer, so they won't be parsed again.
        /// </summary>
        /// <param name="offset"></param>
        public void Clear(int offset)
        {
            int s = 0;
            int save = -1;

            BufNode bn = null;
            for (bn = m_head; bn != null; bn = bn.next)
            {
                if (s + bn.buf.Length <= offset)
                {
                    if (s + bn.buf.Length == offset)
                    {
                        bn = bn.next;
                        break;
                    }
                    s += bn.buf.Length;
                }
                else
                {
                    save = s + bn.buf.Length - offset;
                    break;
                }
            }

            m_head = bn;
            if (m_head == null)
                m_tail = null;

            if (save > 0)
            {
                byte[] buf = new byte[save];
                System.Buffer.BlockCopy(m_head.buf,
                    m_head.buf.Length - save,
                    buf, 0, save);
                m_head.buf = buf;
            }

            m_stream.SetLength(0);
            for (bn = m_head; bn != null; bn = bn.next)
            {
                m_stream.Write(bn.buf, 0, bn.buf.Length);
            }
        }

        /// <summary>
        /// UTF8 encode the current contents of the buffer.  Just for prettiness in the debugger.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            byte[] b = GetBuffer();
            return System.Text.Encoding.UTF8.GetString(b, 0, b.Length);
        }
    }
}
