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

using bedrock.util;

namespace jabber.connection
{
    /// <summary>
    /// Base class for all states.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public abstract class BaseState
    {
    }

    /// <summary>
    /// Up and running.  If subclasses change the state transition
    /// approach, they should end at the RunningState state.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class RunningState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new RunningState();
    }

    /// <summary>
    /// Not connected.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ClosedState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new ClosedState();
    }

    /// <summary>
    /// In the process of connecting.  DNS lookup, socket setup, etc.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ConnectingState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new ConnectingState();
    }

    /// <summary>
    /// Have a connected socket.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ConnectedState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new ConnectedState();
    }

    /// <summary>
    /// Got the stream:stream.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class StreamState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new StreamState();
    }

    /// <summary>
    /// A close was requested, but hasn't yet finalized.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ClosingState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new ClosingState();
    }

    /// <summary>
    /// Paused, waiting for reconnect timeout.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ReconnectingState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new ReconnectingState();
    }

    /// <summary>
    /// Accepting incoming socket connections.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class AcceptingState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new AcceptingState();
    }
    /// <summary>
    /// Old-style auth, iq:auth or handshake.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class NonSASLAuthState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new NonSASLAuthState();
    }
    /// <summary>
    /// Waiting for the server to send the features element
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ServerFeaturesState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new ServerFeaturesState();
    }
    /// <summary>
    /// Start-TLS is starting to TLS.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class StartTLSState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new StartTLSState();
    }
    /// <summary>
    /// SASL Authentication in process
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class SASLState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new SASLState();
    }
    /// <summary>
    /// SASL Authentication finished.  Restarting the stream for the last time.
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class SASLAuthedState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new SASLAuthedState();
    }
    /// <summary>
    /// Binding session
    /// </summary>
    [SVN(@"$Id: States.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class BindState : BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly BaseState Instance = new BindState();
    }

}
