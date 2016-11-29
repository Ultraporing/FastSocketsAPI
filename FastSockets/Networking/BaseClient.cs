//-----------------------------------------------------------------------
// <copyright file="baseclient.cs" company="Tobias Lenz">
//     Copyright Tobias Lenz. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace FastSockets.Networking
{
    /// <summary>
    /// Base Client Class, inherit this class for creating clients.
    /// </summary>
    /// <typeparam name="PacketEnum">The type of the acket enum.</typeparam>
    /// <typeparam name="classType">The class type of the inheriting class.</typeparam>
    /// <seealso cref="DreamLib.Networking.BaseNetworkMachine{PacketEnum, classType}" />
    // /// <seealso cref="FastSockets.Networking.BaseNetworkMachine{packetsEnum, FastSockets.Networking.BaseClient{packetsEnum, classType}}" />
    public abstract class BaseClient<PacketEnum, classType> : BaseNetworkMachine<PacketEnum, classType> where classType : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseClient{PacketEnum, classType}"/> class.
        /// </summary>
        /// <param name="logFile">The log file.</param>
        /// <param name="tcpTimeout">The TCP timeout.</param>
        public BaseClient(string logFile, int tcpTimeout) : base(logFile, EConnectionType.CLIENT, tcpTimeout)
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="BaseClient{PacketEnum, classType}"/> class.
        /// </summary>
        ~BaseClient()
        {
            if (IsConnected())
            {
                Shutdown();
            }    
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public override void Shutdown()
        {
            base.Shutdown();
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        public override void Update()
        {
            base.Update();
        }
    }
}
