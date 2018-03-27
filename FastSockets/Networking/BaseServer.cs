//-----------------------------------------------------------------------
// <copyright file="BaseServer.cs" company="Tobias Lenz">
//     Copyright Tobias Lenz. All rights reserved.
// </copyright>
//------------------------------

namespace FastSockets.Networking
{
    using FastSockets.Helper;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// The base server class for inheritance with servers.
    /// </summary>
    /// <typeparam name="PacketEnum">The type of the packet enum.</typeparam>
    /// <typeparam name="classType">The type of the class type.</typeparam>
    /// <seealso cref="DreamLib.Networking.BaseNetworkMachine{PacketEnum, classType}" />
    public abstract class BaseServer<PacketEnum, classType> : BaseNetworkMachine<PacketEnum, classType> where classType : class
    {
        public delegate void OnClientEvent(int clientID);
        public new event OnPacketReceivedCallback OnPingReceived;
        public event OnClientEvent OnClientAccepted, OnClientDisconnected;
        protected Mutex ClientListMutex = new Mutex();
        protected int PingsDoneCount = 0, PingsRequired = 0;

        /// <summary>
        /// Gets or sets the clients.
        /// </summary>
        /// <value>
        /// The clients.
        /// </value>
        public Dictionary<int, ClientConnection> Clients
        {
            get
            {
                return _clients;
            }

            internal set
            {
                _clients = value;
            }
        }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>
        /// The port.
        /// </value>
        public int Port
        {
            get
            {
                return _port;
            }

            internal set
            {
                _port = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }

            internal set
            {
                _isRunning = value;
            }
        }

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        internal TcpListener Server
        {
            get
            {
                return _server;
            }

            set
            {
                _server = value;
            }
        }

        /// <summary>
        /// Gets or sets the listen thread.
        /// </summary>
        /// <value>
        /// The listen thread.
        /// </value>
        internal Thread ListenThread
        {
            get
            {
                return _listenThread;
            }

            set
            {
                _listenThread = value;
            }
        }

        /// <summary>
        /// Gets or sets the TCP client connected reset event.
        /// </summary>
        /// <value>
        /// The TCP client connected reset event.
        /// </value>
        internal ManualResetEvent TcpClientConnected
        {
            get
            {
                return _tcpClientConnected;
            }

            set
            {
                _tcpClientConnected = value;
            }
        }

        /// <summary>
        /// Gets or sets the number client threads.
        /// </summary>
        /// <value>
        /// The number client threads.
        /// </value>
        internal int NumClientThreads
        {
            get
            {
                return _numClientThreads;
            }

            set
            {
                _numClientThreads = value;
            }
        }

        /// <summary>
        /// Gets or sets the ping timer.
        /// </summary>
        /// <value>
        /// The ping timer.
        /// </value>
        internal Timer PingTimer
        {
            get
            {
                return _pingTimer;
            }

            set
            {
                _pingTimer = value;
            }
        }

        /// <summary>
        /// Gets or sets the ping packet interval.
        /// </summary>
        /// <value>
        /// The ping packet interval.
        /// </value>
        protected int PingPacketInterval
        {
            get
            {
                return _pingPacketInterval;
            }

            private set
            {
                _pingPacketInterval = value;
            }
        }

        /// <summary>
        /// The server
        /// </summary>
        private TcpListener _server = null;

        /// <summary>
        /// The listen thread
        /// </summary>
        private Thread _listenThread = null;

        /// <summary>
        /// The TCP client connected reset event
        /// </summary>
        private ManualResetEvent _tcpClientConnected = new ManualResetEvent(false);

        /// <summary>
        /// The number of client threads
        /// </summary>
        private int _numClientThreads = 0;

        /// <summary>
        /// The clients
        /// </summary>
        private Dictionary<int, ClientConnection> _clients = new Dictionary<int, ClientConnection>();

        /// <summary>
        /// The port
        /// </summary>
        private int _port = 0;

        /// <summary>
        /// The is instance running
        /// </summary>
        private bool _isRunning = false;

        /// <summary>
        /// The _ping timer
        /// </summary>
        private Timer _pingTimer = null;

        /// <summary>
        /// The _ping packet interval
        /// </summary>
        private int _pingPacketInterval = 0;

        private string pingData = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseServer{PacketEnum, classType}" /> class.
        /// </summary>
        /// <param name="cfgFile">The CFG file.</param>
        /// <param name="logFile">The log file.</param>
        /// <param name="connectionType">Type of the connection.</param>
        /// <param name="pingPacketInterval">The ping packet interval.</param>
        /// <param name="tcpTimeout">The TCP timeout.</param>
        public BaseServer(string cfgFile, string logFile, int pingPacketInterval, int tcpTimeout) : base(logFile, EConnectionType.SERVER, tcpTimeout)
        {
            // Setup Config Reader
            if (Directory.Exists("Config"))
            {
                Config.ReadConfig("./Config/" + cfgFile); 
            }
            else
            {
                Directory.CreateDirectory("./Config");
                Config.ReadConfig("./Config/" + cfgFile);
            }

            // Read Port from Config
            int port;
            Config.GetValue("PORT", out port);
            Port = port;

            Clients = new Dictionary<int, ClientConnection>();
            PingPacketInterval = pingPacketInterval;
            PingTimer = new Timer(new TimerCallback(PingClients), null, 0, PingPacketInterval);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseServer{PacketEnum, classType}" /> class.
        /// You need to manually Load the config and set the port if you use this version!
        /// </summary>
        /// <param name="logFile">The log file.</param>
        /// <param name="connectionType">Type of the connection.</param>
        /// <param name="pingPacketInterval">The ping packet interval.</param>
        /// <param name="tcpTimeout">The TCP timeout.</param>
        public BaseServer(string logFile, EConnectionType connectionType, int pingPacketInterval, int tcpTimeout) : base(logFile, connectionType, tcpTimeout)
        {
            Clients = new Dictionary<int, ClientConnection>();
            PingPacketInterval = pingPacketInterval;
            PingTimer = new Timer(new TimerCallback(PingClients), null, 0, PingPacketInterval);
        }

        /// <summary>
        /// Loads the Config from a File inside the Config directory.
        /// </summary>
        public bool LoadConfigFromFile(string cfgFilename)
        {
            // Setup Config Reader
            if (Directory.Exists("Config"))
            {
                return Config.ReadConfig("./Config/" + cfgFilename);
            }
            else
            {
                Directory.CreateDirectory("./Config");
                return Config.ReadConfig("./Config/" + cfgFilename);
            }
        }

        /// <summary>
        /// Loads the Config from a string that uses ; to split the commands.
        /// Example: PORT=1234;
        /// </summary>
        public bool LoadConfigFromString(string cfgString)
        {
            return Config.ReadConfigRaw(cfgString);
        }

        /// <summary>
        /// Sets the Server Port from the config provided.
        /// </summary>
        public bool SetPort()
        {
            // Read Port from Config
            int port;
            if (Config.GetValue("PORT", out port))
            {
                Port = port;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the Server Port from the parameter provided.
        /// </summary>
        public bool SetPort(int port)
        {
            Port = port;

            return true;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="BaseServer{PacketEnum, classType}"/> class.
        /// </summary>
        ~BaseServer()
        {
            if (IsRunning)
            {
                Shutdown(); 
            }
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start(IPAddress listenAddress)
        {
            ConsoleLogger.WriteToLog("Starting Server...", true);

            while (!IsRunning)
            {
                try
                {
                    Server = new TcpListener(listenAddress, Port);
                    Server.Start();
                    IsRunning = true;
                }
                catch (SocketException)
                {
                    Port++;
                }
            }

            ListenThread = new Thread(Listen);
            ListenThread.IsBackground = true;
            ListenThread.Start();

            ConsoleLogger.WriteToLog("Server successfully Started...", true);

            TcpClientConnected.WaitOne(200);

            Thread console = new Thread(new ThreadStart(RunConsoleMenu));
            console.IsBackground = true;
            console.Start();

            Thread title = new Thread(new ThreadStart(UpdateTitle));
            title.IsBackground = true;
            title.Start();
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public override void Shutdown()
        {
            base.Shutdown();

            ConsoleLogger.WriteToLog("Shutting Server down...");

            IsRunning = false;

            TcpClientConnected.Set();

            if (ListenThread != null)
            {
                ListenThread.Join(); 
            }

            while (NumClientThreads > 0)
            {
                Clients.Last().Value.ThisThread.Join();
            }

            Clients.Clear();

            if (Server != null)
            {
                Server.Stop(); 
            }

            ConsoleLogger.WriteToLog("Server successfully Shutdown.");
        }

        /// <summary>
        /// Disconnects the specified unique identifier.
        /// </summary>
        /// <param name="uniqueID">The unique identifier.</param>
        public void Disconnect(int uniqueID = -1)
        {       
            if (Clients.ContainsKey(uniqueID))
            {
                ClientListMutex.WaitOne();

                EConnectionType et = Clients[uniqueID].ConnectionType;

                PacketDesc_ClientDisconnected dcPkt = new PacketDesc_ClientDisconnected();
                dcPkt.OtherClientID = uniqueID;
                dcPkt.PacketTarget = et;
                dcPkt.PacketOriginClientID = UniqueID;
                SendPacketToAllClients(dcPkt, uniqueID);

                ConsoleLogger.WriteToLog("Disconnected Client ID:" + uniqueID, true);

                if (Clients[uniqueID].ThisClient != null)
                {
                    if (Clients[uniqueID].ThisClient.Connected)
                    {
                        Clients[uniqueID].ThisClient.GetStream().Close();
                        if (Clients[uniqueID].ThisClient != null)
                        {
                            Clients[uniqueID].ThisClient.Close();
                        }
                    }

                    Clients[uniqueID].ThisClient = null;
                    Clients.Remove(uniqueID);
                    NumClientThreads--;
                }

                ClientListMutex.ReleaseMutex();

                if (OnClientDisconnected != null)
                {
                    OnClientDisconnected(uniqueID);
                }
            }           
        }

        /// <summary>
        /// Sends the packet to client.
        /// </summary>
        /// <typeparam name="CustomPacketEnum">The type of the custom packet enum.</typeparam>
        /// <param name="packet">The packet.</param>
        /// <param name="targetID">The target identifier.</param>
        public void SendPacketToClient<CustomPacketEnum>(BasePacket<CustomPacketEnum> packet, int targetID)
        {
            ClientConnection clientCon = null;
            
            if (ParentServerConnection != null)
            {
                while (UniqueID == -1)
                {
                }
            }

            if (targetID > -1)
            {
                if (Clients.TryGetValue(targetID, out clientCon))
                {
                    if (!clientCon.ThisClient.Connected)
                    {
                        return;
                    }

                    packet.PacketOriginTotalLatency = clientCon.Ping + (packet.PacketOriginClientID != -1 ? Clients[packet.PacketOriginClientID].Ping : 0);
                    byte[] data = packet.FinalizePacket();
                    try
                    {
                        clientCon.ThisClient.GetStream().Write(data, 0, data.Length);
                    }
                    catch (IOException ex)
                    {
                        // failed to write
                    }
                    
                }
            }
        }

        /// <summary>
        /// Sends the packet to all clients.
        /// </summary>
        /// <typeparam name="CustomPacketEnum">The type of the custom packet enum.</typeparam>
        /// <param name="packet">The packet.</param>
        /// <param name="idToIgnore">The identifier to ignore.</param>
        public void SendPacketToAllClients<CustomPacketEnum>(BasePacket<CustomPacketEnum> packet, int idToIgnore = -1)
        {
            ClientListMutex.WaitOne();
            foreach (KeyValuePair<int, ClientConnection> cc in Clients)
            {
                if (cc.Key == idToIgnore)
                {
                    continue; 
                }
          
                if (cc.Value.ThisClient.Connected)
                {
                    packet.PacketOriginTotalLatency = cc.Value.Ping + packet.PacketOriginClientID != -1 ? Clients[packet.PacketOriginClientID].Ping : 0;
                    byte[] data = packet.FinalizePacket();
                    try
                    {
                        cc.Value.ThisClient.GetStream().Write(data, 0, data.Length);
                    }
                    catch (IOException ex)
                    {
                        // failed to write
                        ClientListMutex.ReleaseMutex();
                    }                   
                }
            }
            ClientListMutex.ReleaseMutex();
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        public override void Update()
        {
            base.Update();
        }

        /// <summary>
        /// Pings the clients.
        /// </summary>
        /// <param name="state">The state.</param>
        internal void PingClients(object state)
        {
            ClientListMutex.WaitOne();
            if (Clients.Count > 0 && !ShuttingDown)
            {
                if (PingsDoneCount == 0)
                {
                    byte[] buffer = Encoding.ASCII.GetBytes(pingData);
                 
                    foreach (KeyValuePair<int, ClientConnection> cc in Clients)
                    {
                        Ping pingSender = new Ping();
                        PingOptions options = new PingOptions(64, true);
                        pingSender.PingCompleted += PingCallback;
                        pingSender.SendAsync(((IPEndPoint)cc.Value.ThisClient.Client.RemoteEndPoint).Address, TCPTimeout, buffer, cc.Value.ThisID);
                    }
                                      
                }
            }
            ClientListMutex.ReleaseMutex();
        }

        /// <summary>
        /// Creates the unique client identifier.
        /// </summary>
        /// <returns>Returns a unique client identifier</returns>
        protected int CreateUniqueClientID()
        {
            Random rndNum = new Random(int.Parse(Guid.NewGuid().ToString().Substring(0, 8), System.Globalization.NumberStyles.HexNumber));
            int rndID = rndNum.Next();

            while (Clients.ContainsKey(rndID))
            {
                rndID = rndNum.Next();
            }

            return rndID;
        }

        /// <summary>
        /// Displays the help.
        /// </summary>
        protected virtual void DisplayHelp()
        {
            Console.WriteLine("===================================================");
            Console.WriteLine("#------------Fast Sockets API Server--------------#");
            Console.WriteLine("===================================================");
            Console.WriteLine("#--Display this with 'help'-----------------------#");
            Console.WriteLine("#--Stop Server with 'stop' or 'exit'--------------#");
            Console.WriteLine("#--Disconnect parent with 'dc'--------------------#");
            Console.WriteLine("#--Disconnect client with 'dc <ID>'---------------#");
            Console.WriteLine("#--Disconnect all clients with 'dc all'-----------#");
            Console.WriteLine("#--List all Client Connections with 'list'--------#");
            Console.WriteLine("===================================================");
        }

        /// <summary>
        /// Gets the console window title.
        /// </summary>
        /// <returns>
        /// Returns string if parent server is connected and the connected number of clients. P:true/false, C:numClients
        /// </returns>
        protected override string GetConsoleWindowTitle()
        {
            return base.GetConsoleWindowTitle() + ", C:" + NumClientThreads;
        }

        /// <summary>
        /// Handles the console command.
        /// </summary>
        /// <param name="cmd">The command.</param>
        protected virtual void HandleConsoleCommand(string cmd)
        {
            string[] spl = cmd.Split(' ');

            // handle parent disconnect
            if (spl[0] == "dc" && spl.Length == 1)
            {
                Disconnect();
            }
            else if (spl[0] == "dc" && spl.Length == 2)
            {
                if (spl[0] == "dc" && spl[1] == "all")
                {
                    while (Clients.Count > 0)
                    {
                        Disconnect(Clients.Last().Key);
                    }
                }
                else
                {
                    int id = -2;
                    try
                    {
                        int.TryParse(spl[1], out id);
                    }
                    catch
                    {
                        Console.WriteLine("Error client ID is not a Integer!");
                        return;
                    }

                    if (id > -2)
                        Disconnect(id);
                }
            }

            
            else if (spl[0] == "help" && spl.Length == 1)
            {
                DisplayHelp();
            }
            else if (spl[0] == "list")
            {
                Console.WriteLine("===================================================");
                
                string s = "#-Listing: " + Clients.Count + " Clients-";
                int n = 50 - s.Length;
                for (int i = 0; i < n; i++)
                {
                    s += "-"; 
                }

                s += "#";
                Console.WriteLine(s);
                Console.WriteLine("===================================================");
                Console.WriteLine("#==========ID============|============TYPE========#");

                foreach (KeyValuePair<int, ClientConnection> cc in Clients)
                {
                    string s1 = "# ";
                    int idNum = 23 - cc.Key.ToString().Length;
                    s1 += cc.Key.ToString();
                    for (int i = 0; i < idNum; i++)
                    {
                        s1 += " "; 
                    }

                    s1 += "| ";
                    idNum = 23 - cc.Value.ConnectionType.ToString().Length;
                    s1 += cc.Value.ConnectionType.ToString();
                    for (int i = 0; i < idNum; i++)
                    {
                        s1 += " "; 
                    }

                    s1 += "#";
                    Console.WriteLine(s1);
                }

                Console.WriteLine("===================================================");
            }
        }

        protected void PingCallback(object sender, System.Net.NetworkInformation.PingCompletedEventArgs e)
        {
            // If the operation was canceled, display a message to the user.
            if (e.Cancelled)
            {
                Console.WriteLine("Ping canceled.");
                Disconnect(((ClientConnection)e.UserState).ThisID);
                ((Ping)sender).Dispose();
                PingsDoneCount++;
                if (PingsDoneCount >= Clients.Count)
                {
                    PingsDoneCount = 0;
                }
                return;
            }

            // If an error occurred, display the exception to the user.
            if (e.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Disconnect(((ClientConnection)e.UserState).ThisID);
                ((Ping)sender).Dispose();
                PingsDoneCount++;
                if (PingsDoneCount >= Clients.Count)
                {
                    PingsDoneCount = 0;
                }
                return;
            }

            PingReply reply = e.Reply;
            int id = ((int)e.UserState);
            ClientConnection cc = null;
            if (!Clients.TryGetValue(id, out cc))
            {
                ((Ping)sender).Dispose();
                PingsDoneCount++;
                if (PingsDoneCount >= Clients.Count)
                {
                    PingsDoneCount = 0;
                }
                return;
            }

            if (!cc.ThisClient.Connected)
            {
                ((Ping)sender).Dispose();
                PingsDoneCount++;
                if (PingsDoneCount >= Clients.Count)
                {
                    PingsDoneCount = 0;
                }
                return;
            }

            cc.Ping = (int)(reply.RoundtripTime / 2);
            PacketDesc_Ping pkt = new PacketDesc_Ping();
            pkt.PacketTarget = EConnectionType.CLIENT;
            pkt.PacketOriginClientID = UniqueID;
            pkt.ToServerLatency = cc.Ping; // send clients one way to server latency
            SendPacketToClient(pkt, id);
            ((Ping)sender).Dispose();
            PingsDoneCount++;

            if (PingsDoneCount >= Clients.Count)
            {
                PingsDoneCount = 0;
            }
        }
        
        /// <summary>
        /// Runs the console menu.
        /// </summary>
        private void RunConsoleMenu()
        {
            string cmd = string.Empty;
            DisplayHelp();
            do
            {
                cmd = Console.ReadLine().ToLower();
                HandleConsoleCommand(cmd);
            }
            while (cmd != "exit" && cmd != "stop");

            ConsoleLogger.WriteToLog("Console Stop");
            Shutdown();
        }

        /// <summary>
        /// Listens for clients.
        /// </summary>
        private void Listen()
        {
            ConsoleLogger.WriteToLog("Listening at Port: " + Port);

            while (IsRunning)
            {
                TcpClientConnected.Reset();

                ConsoleLogger.WriteToLog("Waiting for Client Connections...");
                
                Server.BeginAcceptTcpClient(new AsyncCallback(EventTcpClientAccept), Server);

                TcpClientConnected.WaitOne();

                if (!IsRunning)
                {
                    break; 
                }
            }

            ConsoleLogger.WriteToLog("Stopped Listening for Clients.");
        }

        /// <summary>
        /// Events the TCP client accept.
        /// </summary>
        /// <param name="ar">The async result.</param>
        private void EventTcpClientAccept(IAsyncResult ar)
        {
            try
            {
                TcpListener listener = (TcpListener)ar.AsyncState;
                TcpClient newClient = listener.EndAcceptTcpClient(ar);
                newClient.ReceiveTimeout = 0;
                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.IsBackground = true;
                int uniqueID = CreateUniqueClientID();

                ClientConnection sC = new ClientConnection();
                sC.ThisID = uniqueID;
                sC.ThisClient = newClient;
                sC.ThisThread = t;

                switch (ConnectionType)
                {
                    case EConnectionType.SERVER:
                        sC.ConnectionType = EConnectionType.CLIENT;
                        break;
                }

                Clients.Add(uniqueID, sC);
                Clients[uniqueID].ThisThread.Start(Clients[uniqueID]);

                ConsoleLogger.WriteToLog("Client ID: " + uniqueID + " Connected.", true);

                PacketDesc_SetClientID sidpkg = new PacketDesc_SetClientID();
                sidpkg.PacketTarget = sC.ConnectionType;
                sidpkg.Id = uniqueID;
                sidpkg.SenderID = UniqueID;
                sidpkg.PacketOriginClientID = UniqueID;
                SendPacketToClient(sidpkg, uniqueID);

                PacketDesc_ClientConnected copkg = new PacketDesc_ClientConnected();
                copkg.PacketTarget = sC.ConnectionType;
                copkg.OtherClientID = uniqueID;
                copkg.PacketOriginClientID = UniqueID;
                SendPacketToAllClients(copkg, uniqueID);

                UpdateTitle();

                TcpClientConnected.Set();

                if (OnClientAccepted != null)
                {
                    OnClientAccepted(uniqueID);
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }

        /// <summary>
        /// Handles the client.
        /// </summary>
        /// <param name="client">The client.</param>
        private void HandleClient(object client)
        {
            ClientConnection theClient = (ClientConnection)client;
            NumClientThreads++;

            try
            {
                bool clientConnected = true;

                NetworkStream stream = theClient.ThisClient.GetStream();
                byte[] buffer = new byte[PACKET_BUFFER_SIZE];

                while (clientConnected)
                {
                    if (!IsClientConnected(Clients[theClient.ThisID]))
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
                        ParsePacket(ref byteList, theClient);
                    }

                    if (!Clients.ContainsKey(theClient.ThisID))
                    {
                        break; 
                    }
                }

                //ConsoleLogger.WriteToLog("Client ID: " + theClient.ThisID + " Disconnected.", true);
                Disconnect(theClient.ThisID);

                UpdateTitle();
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
