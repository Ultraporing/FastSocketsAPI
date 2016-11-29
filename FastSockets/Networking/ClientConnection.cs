//-----------------------------------------------------------------------
// <copyright file="clientconnection.cs" company="Tobias Lenz">
//     Copyright Tobias Lenz. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace FastSockets.Networking
{
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// The Client Connection Container
    /// </summary>
    public class ClientConnection
    {
        /// <summary>
        /// The identifier
        /// </summary>
        public int ThisID = 0;

        /// <summary>
        /// The TCP client
        /// </summary>
        public TcpClient ThisClient = null;

        /// <summary>
        /// The Thread
        /// </summary>
        public Thread ThisThread = null;

        /// <summary>
        /// The connection type
        /// </summary>
        public EConnectionType ConnectionType = EConnectionType.NONE;

        /// <summary>
        /// The ping
        /// </summary>
        public int Ping = 0;
    }
}
