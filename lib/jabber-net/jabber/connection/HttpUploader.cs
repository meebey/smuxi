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
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.IO;
using System.Collections;

namespace jabber.connection
{
    /// <summary>
    /// An implementation of XEP-70, I suppose.F
    /// </summary>
    public class HttpUploader
    {
        /// <summary>
        /// An upload has finished.
        /// </summary>
        public event bedrock.ObjectHandler OnUpload;

        /// <summary>
        /// Constructor
        /// </summary>
        public HttpUploader()
        {
        }

        private void ResponseCallback(IAsyncResult result)
        {
            HttpWebRequest request  = (HttpWebRequest)result.AsyncState;
            //request.GetResponse().GetResponseStream();
            if (OnUpload != null)
                OnUpload(this);
        }

        /// <summary>
        /// Upload a file to a given URL, doing XEP-70 authentication.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="jid"></param>
        /// <param name="filename"></param>
        public void Upload(string uri, string jid, string filename)
        {
            //try
            //{
            StreamReader reader = new StreamReader(filename);
            HttpWebRequest request =
                (HttpWebRequest)HttpWebRequest.Create(uri);

            request.Method = "POST";
            request.Headers.Add(HttpRequestHeader.Authorization,
                                "x-xmpp-auth jid=\"" + jid + "\"");

            StreamWriter writer = new StreamWriter(request.GetRequestStream());
            writer.Write(reader.ReadToEnd());

            reader.Close();

            request.BeginGetResponse(new AsyncCallback(ResponseCallback),
                                     request);
            writer.Close();
            // }
            // catch (WebException)
            // {
            // }
        }
    }
}
