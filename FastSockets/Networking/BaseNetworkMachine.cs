//-----------------------------------------------------------------------
// <copyright file="basenetworkmachine.cs" company="Tobias Lenz">
//     Copyright Tobias Lenz. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace FastSockets.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Threading;

    /// <summary>
    /// Lowest layer of the Networking hierarchy. Cannot be inherited outside the Assembly!
    /// </summary>
    /// <typeparam name="packetsEnumType">The type of the packet enum.</typeparam>
    /// <typeparam name="classType">The type of the class inheriting BaseServer or BaseClient.</typeparam>
    public abstract class BaseNetworkMachine<packetsEnumType, classType> where classType : class
    {
        /// <summary>
        /// The packet buffer size
        /// </summary>
        internal readonly int PACKET_BUFFER_SIZE = 2048;

        /// <summary>
        /// The _unique identifier
        /// </summary>
        private int _uniqueID = -1;

        /// <summary>
        /// The _parent server connection
        /// </summary>
        private ClientConnection _parentServerConnection = null;

        /// <summary>
        /// The packet methods
        /// </summary>
        private Dictionary<int, Func<classType, object, object>> _packetMethods = new Dictionary<int, Func<classType, object, object>>();

        /// <summary>
        /// The _connection type
        /// </summary>
        private EConnectionType _connectionType = EConnectionType.NONE;

        /// <summary>
        /// The _is shutting down
        /// </summary>
        private bool _isShuttingDown = false;

        /// <summary>
        /// The _TCP timeout
        /// </summary>
        private int _tcpTimeout = 0;

        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        /// <value>
        /// The unique identifier.
        /// </value>
        public int UniqueID
        {
            get
            {
                return _uniqueID;
            }

            internal set
            {
                _uniqueID = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the connection.
        /// </summary>
        /// <value>
        /// The type of the connection.
        /// </value>
        public EConnectionType ConnectionType
        {
            get
            {
                return _connectionType;
            }

            internal set
            {
                _connectionType = value;
            }
        }

        /// <summary>
        /// Gets or sets the packet methods.
        /// </summary>
        /// <value>
        /// The packet methods.
        /// </value>
        internal Dictionary<int, Func<classType, object, object>> PacketMethods
        {
            get
            {
                return _packetMethods;
            }

            set
            {
                _packetMethods = value;
            }
        }

        /// <summary>
        /// Gets or sets the parent server connection.
        /// </summary>
        /// <value>
        /// The parent server connection.
        /// </value>
        internal ClientConnection ParentServerConnection
        {
            get
            {
                return _parentServerConnection;
            }

            set
            {
                _parentServerConnection = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [shutting down].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [shutting down]; otherwise, <c>false</c>.
        /// </value>
        public bool ShuttingDown
        {
            get
            {
                return _isShuttingDown;
            }

            internal set
            {
                _isShuttingDown = value;
            }
        }

        /// <summary>
        /// Gets or sets the TCP timeout.
        /// </summary>
        /// <value>
        /// The TCP timeout.
        /// </value>
        public int TCPTimeout
        {
            get
            {
                return _tcpTimeout;
            }

            internal set
            {
                _tcpTimeout = value;
            }
        }

        public delegate void OnPacketReceivedCallback(KeyValuePair<ClientConnection, object> pkt);
        public delegate void OnApiEvent();
        public event OnPacketReceivedCallback OnClientConnectedReceived, OnClientDisconnectedReceived, OnPingReceived, OnSetClientIDReceived;
        public event OnApiEvent OnDisconnected, OnConnectionFailed, OnConnectionSuccess;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNetworkMachine{packetsEnumType, classType}"/> class.
        /// </summary>
        internal BaseNetworkMachine()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNetworkMachine{packetsEnumType, classType}" /> class.
        /// </summary>
        /// <param name="logFile">The log file. When string.Empty dont use logfiles!</param>
        /// <param name="connectionType">Type of the connection.</param>
        /// <param name="tcpTimeout">The TCP timeout.</param>
        internal BaseNetworkMachine(string logFile, EConnectionType connectionType, int tcpTimeout)
        {
            if (logFile != string.Empty)
            {
                SetupLogger("Logs", logFile + "_" + DateTime.Now.ToString("ddMMyyyyHHmmssfffff") + ".txt");
            }
                
            Debug.Assert(connectionType != EConnectionType.NONE, "Machine has to have a Connection Type!");
            _connectionType = connectionType;

            string s = typeof(classType).ToString();
            string s1 = typeof(BaseNetworkMachine<packetsEnumType, classType>).ToString();
            InitPacketEnum(typeof(EBasePackets), ref _packetMethods);
            InitPacketEnum(typeof(packetsEnumType), ref _packetMethods);

            Console.Title = GetConsoleWindowTitle();
            TCPTimeout = tcpTimeout;
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public virtual void Shutdown()
        {
            ShuttingDown = true;
            if (ParentServerConnection != null)
            {
                ParentServerConnection.ThisThread.Join(); 
            }
        }

        /// <summary>
        /// Determines whether this instance is connected.
        /// </summary>
        /// <returns>Returns true if connected to a Parent Server.</returns>
        public bool IsConnected()
        {
            if (ParentServerConnection != null)
            {
                if (ParentServerConnection.ThisClient != null)
                {
                    return ParentServerConnection.ThisClient.Connected;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Sends the packet to parent.
        /// </summary>
        /// <typeparam name="T">Type of Packet.</typeparam>
        /// <param name="packet">The packet.</param>
        public void SendPacketToParent<T>(BasePacket<T> packet)
        {
            byte[] data = packet.FinalizePacket();

            if (ParentServerConnection != null)
            {
                while (UniqueID == -1)
                {
                }

                ParentServerConnection.ThisClient.GetStream().Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Connects to the specified IP address.
        /// </summary>
        /// <param name="ipAddr">The IP address.</param>
        /// <param name="localBindingPort">The local binding port.</param>
        /// <param name="connectionTries">The connection tries.</param>
        /// <param name="sleepMS">The sleep milliseconds.</param>
        /// <returns>Returns true on connection success.</returns>
        public bool Connect(IPEndPoint ipAddr, IPEndPoint localEndpoint, int connectionTries = 5, int sleepMS = 1000)
        {
            if (IsConnected())
            {
                Disconnect(); 
            }

            ConsoleLogger.WriteToLog("Connecting to Parent Server...", true);
            
            ParentServerConnection = new ClientConnection();

            switch (_connectionType)
            {
                case EConnectionType.CLIENT:
                    ParentServerConnection.ConnectionType = EConnectionType.SECTOR_SERVER;
                    break;
                case EConnectionType.SECTOR_SERVER:
                    ParentServerConnection.ConnectionType = EConnectionType.SUPERVISOR_SERVER;
                    break;
                case EConnectionType.SUPERVISOR_SERVER:
                    ParentServerConnection.ConnectionType = EConnectionType.LOGIN_SERVER;
                    break;
            }

            while (ParentServerConnection.ThisClient == null)
            {
                try
                {
                    ParentServerConnection.ThisClient = new TcpClient(localEndpoint);
                }
                catch (SocketException e)
                {
                    ConsoleLogger.WriteToLog(e.Message);

                    return false;
                }
            }

            ParentServerConnection.ThisID = -1; // there is only 1 parent server, so it does not need an unique id
            ParentServerConnection.ThisClient.ReceiveTimeout = TCPTimeout;

            while (connectionTries > 0)
            {
                try
                {
                    ParentServerConnection.ThisClient.Connect(ipAddr);
                    break;
                }
                catch (Exception e)
                {
                    ConsoleLogger.WriteToLog(e.Message);
                    connectionTries--;
                    Thread.Sleep(sleepMS);
                }
            }

            if (connectionTries <= 0)
            {
                ConsoleLogger.WriteToLog("Failed to Connect to Parent Server...", true);

                if (OnConnectionFailed != null)
                {
                    OnConnectionFailed();
                }

                return false;
            }

            bool ret = StartParentServerHandling();

            if (ret)
            {
                ConsoleLogger.WriteToLog("Successfully Connected to Parent Server...", true); 

                if (OnConnectionSuccess != null)
                {
                    OnConnectionSuccess();
                }
            }
            else
            {
                ConsoleLogger.WriteToLog("Failed to Connect to Parent Server...", true);

                if (OnConnectionFailed != null)
                {
                    OnConnectionFailed();
                }
            }

            UpdateTitle();

            return ret;
        }

        /// <summary>
        /// Disconnects client from parent server.
        /// </summary>
        public void Disconnect()
        {
            if (ParentServerConnection != null)
            {
                if (ParentServerConnection.ThisClient != null)
                {
                    if (ParentServerConnection.ThisClient.Connected)
                    {
                        ParentServerConnection.ThisClient.GetStream().Close();
                        ParentServerConnection.ThisClient.Close();
                    }

                    ParentServerConnection.ThisClient = null;
                }

                ParentServerConnection.ThisThread = null;
                ParentServerConnection = null;

                UniqueID = -1;
                ConsoleLogger.WriteToLog("Disconnected Parent Server...", true);

                UpdateTitle();

                if (OnDisconnected != null)
                {
                    OnDisconnected();
                }
            }  
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Tests the port availability.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns>Returns true if port is available.</returns>
        internal bool TestPortAvailability(int port)
        {
                TcpListener tcpClient = null;
                try
                {
                    tcpClient = new TcpListener(new IPEndPoint(IPAddress.Loopback, port));
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
        }

        /// <summary>
        /// Initializes the packet enum.
        /// </summary>
        /// <typeparam name="ClassType">The type of the class.</typeparam>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="dict">The dictionary.</param>
        internal void InitPacketEnum<ClassType>(Type enumType, ref Dictionary<int, Func<ClassType, object, object>> dict) where ClassType : class
        {
            string[] nameList = Enum.GetNames(enumType);
            int[] valueList = (int[])Enum.GetValues(enumType);

            // ignore the last packet, wich is NUM_PACKETS
            for (int i = 0; i < nameList.Length - 1; i++) 
            {
                MethodInfo meth = this.GetType().GetMethod("ReceivedPacket_" + nameList[i], BindingFlags.NonPublic | BindingFlags.Instance);
 
                if (meth != null)
                {
                    Type t = GetType();
                    Func<ClassType, object, object> me = MagicMethod<ClassType>(meth);
                    string b = me.GetType().ToString();
                    dict.Add(valueList[i], me);
                }
            }
        }

        /// <summary>
        /// Determines whether [is client connected] [the specified client].
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns>Returns true if client is connected.</returns>
        internal bool IsClientConnected(TcpClient client)
        {
            try
            {
                if (client.Client.Connected)
                {
                    if (client.Client.Poll(0, SelectMode.SelectWrite) && !client.Client.Poll(0, SelectMode.SelectError))
                    {
                        byte[] buffer = new byte[1];
                        if (client.Client.Receive(buffer, SocketFlags.Peek) == 0)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        /// <summary>
        /// Parses the packet.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="sender">The sender.</param>
        internal void ParsePacket(List<byte> data, ClientConnection sender)
        {
            ParsePacket(data.ToArray(), ref sender);
        }

        /// <summary>
        /// Relays the packet to parent.
        /// </summary>
        /// <param name="packet">The packet.</param>
        internal void RelayPacketToParent(byte[] packet)
        {
            if (ParentServerConnection != null)
            {
                ParentServerConnection.ThisClient.GetStream().Write(packet, 0, packet.Length);
            }
        }

        /// <summary>
        /// Parses the packet.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="sender">The sender.</param>
        internal void ParsePacket(byte[] data, ref ClientConnection sender)
        {
            try
            {
                int packetID = BitConverter.ToInt32(data, 0);
                byte[] barr = new byte[data.Length - sizeof(int)];
                Array.Copy(data.ToArray(), sizeof(int), barr, 0, barr.Length);

                CallPacketFunction(packetID, sender, ref barr, ref _packetMethods);
            }
            catch
            {
                ConsoleLogger.WriteErrorToLog("Recieved Invalid Packet by Client ID: " + sender.ThisID + ", Data: " + System.Text.Encoding.Default.GetString(data));
            }
        }

        /// <summary>
        /// Calls the packet function.
        /// </summary>
        /// <typeparam name="ClassType">The type of the class.</typeparam>
        /// <param name="packetID">The packet identifier.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The data.</param>
        /// <param name="dict">The dictionary.</param>
        /// <returns>Returns true if call succeeded.</returns>
        internal bool CallPacketFunction<ClassType>(int packetID, ClientConnection sender, ref byte[] data, ref Dictionary<int, Func<ClassType, object, object>> dict) where ClassType : class
        {
            object ret = Serializer.DataSerializers.ByteArrayToObject(data);

            if (!dict.ContainsKey(packetID))
            {
                ConsoleLogger.WriteErrorToLog("Packet does not exist, Invalid packetID: " + packetID);
                return false;
            }

            Func<ClassType, object, object> meth = null;

            if (dict.TryGetValue(packetID, out meth))
            {
                if (sender.ThisClient != null)
                {
                    try
                    {
                        ThreadStart threadMain = delegate 
                        {
                            try
                            {
                                meth.DynamicInvoke(new object[] { this, (new KeyValuePair<ClientConnection, object>(sender, ret)) });
                            }
                            catch
                            {
                                return;
                            }
                        };
                        new Thread(threadMain).Start();
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Received the packet set client identifier.
        /// </summary>
        /// <param name="conPkt">The sender connection and packet.</param>
        /// <returns>use how needed</returns>
        protected bool ReceivedPacket_SetClientID(KeyValuePair<ClientConnection, object> conPkt)
        {
            PacketDesc_SetClientID packet = (PacketDesc_SetClientID)conPkt.Value;

            ConsoleLogger.WriteToLog("Recieved SetClientID Packet! Client ID: " + packet.SenderID + ", Data: assigned client ID " + packet.Id);
            UniqueID = packet.Id;
            ParentServerConnection.ThisID = packet.SenderID;

            if (OnSetClientIDReceived != null)
            {
                OnSetClientIDReceived(conPkt);
            }

            return true;
        }

        /// <summary>
        /// Received the packet ping.
        /// </summary>
        /// <param name="conPkt">The sender connection and packet.</param>
        /// <returns>use how needed</returns>
        protected virtual bool ReceivedPacket_Ping(KeyValuePair<ClientConnection, object> conPkt)
        {
            PacketDesc_Ping packet = (PacketDesc_Ping)conPkt.Value;

            if (!packet.IsPong)
            {
                ParentServerConnection.Ping = DateTime.UtcNow.Millisecond - packet.Time;
                //ConsoleLogger.WriteToLog("Recieved Ping Packet! Sender ID: " + conPkt.Key.ThisID + ", Data: " + ParentServerConnection.Ping);

                PacketDesc_Ping pkt = new PacketDesc_Ping();
                pkt.PacketTarget = conPkt.Key.ConnectionType;
                pkt.Time = DateTime.UtcNow.Millisecond;
                pkt.IsPong = true;
                SendPacketToParent(pkt);

                if (OnPingReceived != null)
                {
                    OnPingReceived(conPkt);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Recieved the client connected packet.
        /// </summary>
        /// <param name="conPkt">The con PKT.</param>
        /// <returns>use as needed</returns>
        protected virtual bool ReceivedPacket_ClientConnected(KeyValuePair<ClientConnection, object> conPkt)
        {
            PacketDesc_ClientConnected packet = (PacketDesc_ClientConnected)conPkt.Value;
            Console.WriteLine("Recieved ClientConnected Packet! Client ID: " + conPkt.Key.ThisID + ", Data: connected client ID " + packet.OtherClientID);

            if (OnClientConnectedReceived != null)
            {
                OnClientConnectedReceived(conPkt);
            }

            return true;
        }

        /// <summary>
        /// Recieved the client disconnected packet.
        /// </summary>
        /// <param name="conPkt">The con PKT.</param>
        /// <returns>use as needed</returns>
        protected virtual bool ReceivedPacket_ClientDisconnected(KeyValuePair<ClientConnection, object> conPkt)
        {
            PacketDesc_ClientDisconnected packet = (PacketDesc_ClientDisconnected)conPkt.Value;
            Console.WriteLine("Recieved ClientDisconnected Packet! Client ID: " + conPkt.Key.ThisID + ", Data: disconnected client ID " + packet.OtherClientID);

            if (OnClientDisconnectedReceived != null)
            {
                OnClientDisconnectedReceived(conPkt);
            }

            return true;
        }

        /// <summary>
        /// Updates the title.
        /// </summary>
        internal void UpdateTitle()
        {
            Console.Title = GetConsoleWindowTitle();
        }

        /// <summary>
        /// Gets the console window title.
        /// </summary>
        /// <returns>Returns string if parent server is connected. P:true/false</returns>
        protected virtual string GetConsoleWindowTitle()
        {
            return "P:" + (ParentServerConnection != null).ToString();
        }

        /// <summary>
        /// Method to create delegates from MethodInfo.
        /// </summary>
        /// <typeparam name="ClassType">class type.</typeparam>
        /// <param name="method">The method.</param>
        /// <returns>Returns delegate.</returns>
        private static Func<ClassType, object, object> MagicMethod<ClassType>(MethodInfo method) where ClassType : class
        {
            // First fetch the generic form
            MethodInfo genericHelper = typeof(BaseNetworkMachine<packetsEnumType, classType>).GetMethod("MagicMethodHelper", BindingFlags.Static | BindingFlags.NonPublic);

            // Now supply the type arguments
            MethodInfo constructedHelper = genericHelper.MakeGenericMethod(typeof(ClassType), method.GetParameters()[0].ParameterType, method.ReturnType);

            // Now call it. The null argument is because it's a static method.
            object ret = constructedHelper.Invoke(null, new object[] { method });

            // Cast the result to the right kind of delegate and return it
            return (Func<ClassType, object, object>)ret;
        }

        /// <summary>
        /// Helper for MagicMethod.
        /// </summary>
        /// <typeparam name="TTarget">The type of the target.</typeparam>
        /// <typeparam name="TParam">The type of the parameter.</typeparam>
        /// <typeparam name="TReturn">The type of the return.</typeparam>
        /// <param name="method">The method.</param>
        /// <returns>Returns delegate.</returns>
        private static Func<TTarget, object, object> MagicMethodHelper<TTarget, TParam, TReturn>(MethodInfo method) where TTarget : class
        {
            // Convert the slow MethodInfo into a fast, strongly typed, open delegate
            Func<TTarget, TParam, TReturn> func = (Func<TTarget, TParam, TReturn>)Delegate.CreateDelegate(typeof(Func<TTarget, TParam, TReturn>), method);

            // Now create a more weakly typed delegate which will call the strongly typed one
            Func<TTarget, object, object> ret = (TTarget target, object param) => func(target, (TParam)param);
            return ret;
        }

        /// <summary>
        /// Setups the logger.
        /// </summary>
        /// <param name="logDir">The log directory.</param>
        /// <param name="logFile">The log file.</param>
        private void SetupLogger(string logDir, string logFile)
        {
            // Setup Logger
            if (Directory.Exists(logDir))
            {
                ConsoleLogger.InitLogger(logDir + "/" + logFile);
            }
            else
            {
                Directory.CreateDirectory(logDir);
                ConsoleLogger.InitLogger(logDir + "/" + logFile);
            }
        }

        /// <summary>
        /// Starts the parent server handling.
        /// </summary>
        /// <returns>Returns true on success.</returns>
        private bool StartParentServerHandling()
        {
            try
            {
                Thread t = new Thread(new ParameterizedThreadStart(HandleParentServer));
                t.IsBackground = true;

                ParentServerConnection.ThisThread = t;
                ParentServerConnection.ThisThread.Start(null);
            }
            catch (ObjectDisposedException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles the parent server.
        /// </summary>
        /// <param name="obj">The object.</param>
        private void HandleParentServer(object obj)
        {
            try
            {
                bool serverConnected = true;

                NetworkStream stream = ParentServerConnection.ThisClient.GetStream();
                byte[] buffer = new byte[PACKET_BUFFER_SIZE];

                while (serverConnected)
                {
                    if (!ParentServerConnection.ThisClient.Connected)
                    {
                        break; 
                    }

                    buffer = new byte[PACKET_BUFFER_SIZE];
                    int bytesRead = 0;
                    List<byte> byteList = new List<byte>();

                    while (stream.DataAvailable)
                    {
                        bytesRead = stream.Read(buffer, 0, PACKET_BUFFER_SIZE);
                        byte[] b = new byte[bytesRead];
                        Array.Copy(buffer, 0, b, 0, bytesRead);
                        byteList.AddRange(b);
                    }

                    if (byteList.Count > 0)
                    {
                        ParsePacket(byteList, ParentServerConnection);
                    }

                    serverConnected = IsClientConnected(ParentServerConnection.ThisClient) & (!ShuttingDown);
                }

                ConsoleLogger.WriteToLog("Server ID: " + ParentServerConnection.ThisID + " Disconnected.", true);

                Disconnect();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException)
            {
            }
        }
    }
}
