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

using System.IO;
using bedrock.util;

namespace bedrock.io
{
    /// <summary>
    /// Wrap a stream, so that OnRead events can be fired.
    /// </summary>
    [SVN(@"$Id: ReadEventStream.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ReadEventStream : Stream
    {
        private Stream m_stream;

        /// <summary>
        /// Create a new stream.
        /// </summary>
        /// <param name="s"></param>
        public ReadEventStream(Stream s)
        {
            m_stream = s;
        }

        /// <summary>
        /// Bytes have been read from the underlying stream.
        /// </summary>
        public event ByteOffsetHandler OnRead;

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get { return m_stream.CanRead; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get { return m_stream.CanSeek; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get { return m_stream.CanWrite; }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length
        {
            get { return m_stream.Length; }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get { return m_stream.Position; }
            set { m_stream.Position = value; }
        }

        /// <summary>
        /// Begins an asynchronous read operation.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return m_stream.BeginRead (buffer, offset, count, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous write operation.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return m_stream.BeginWrite (buffer, offset, count, callback, state);
        }

        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
        /// </summary>
        public override void Close()
        {
            m_stream.Close ();
        }

        /// <summary>
        /// Waits for the pending asynchronous read to complete.
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        public override int EndRead(IAsyncResult asyncResult)
        {
            int count = m_stream.EndRead(asyncResult);
            byte[] buf = System.Text.Encoding.UTF8.GetBytes("Read " + count + " bytes from async stream");
            FireOnRead(buf, 0, buf.Length);
            return count;
        }

        /// <summary>
        /// Ends an asynchronous write operation.
        /// </summary>
        /// <param name="asyncResult"></param>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            m_stream.EndWrite (asyncResult);
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to
        /// the underlying device.
        /// </summary>
        public override void Flush()
        {
            m_stream.Flush();
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream
        /// by the number of bytes read.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int rcount = m_stream.Read(buffer, offset, count);
            FireOnRead(buffer, offset, rcount);
            return rcount;
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte,
        /// or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns></returns>
        public override int ReadByte()
        {
            return m_stream.ReadByte();
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_stream.Seek(offset, origin);
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            m_stream.SetLength(value);
        }

        /// <summary>
        /// writes a sequence of bytes to the current stream and advances
        /// the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            m_stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances
        /// the position within the stream by one byte.
        /// </summary>
        /// <param name="val"></param>
        public override void WriteByte(byte val)
        {
            m_stream.WriteByte(val);
        }

        /// <summary>
        /// Serves as a hash function for a particular type, suitable for use
        /// in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return m_stream.GetHashCode();
        }

        private void FireOnRead(byte[] buf, int offset, int length)
        {
            if ((OnRead != null) && (length > 0))
            {
                OnRead(this, buf, offset, length);
            }
        }
    }
}
