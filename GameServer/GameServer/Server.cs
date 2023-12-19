using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    public class Server
    {
        public static Dictionary<int, Client> Clients = new Dictionary<int, Client>();
        public delegate void PacketHandler(int fromClient, Packet packet);
        public static Dictionary<int, PacketHandler> PacketHandlers;
        public static int MaxPlayer { get; private set; }
        public static int Port { get; private set; }
        
        private static TcpListener _tcpListener;
        private static UdpClient _udpListener;

        public static void Start(int maxPlayer, int port)
        {
            MaxPlayer = maxPlayer;
            Port = port;
            
            Console.WriteLine("server starting...");
            InitializeServerData();
            _tcpListener = new TcpListener(IPAddress.Any, Port);
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallback),null);
            
            _udpListener = new UdpClient(Port);
            _udpListener.BeginReceive(UDPReceiveCallback, null);
            
            Console.WriteLine($"Begin Accepting Clients On Port {Port}");

        }

        private static void UDPReceiveCallback(IAsyncResult result)
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udpListener.EndReceive(result, ref clientEndPoint);
                _udpListener.BeginReceive(UDPReceiveCallback, null);

                if (data.Length < 4)
                {
                    return;
                }

                using (Packet packet = new Packet(data))
                {
                    int clientId = packet.ReadInt();

                    if (clientId == 0)
                    {
                        return;
                    }

                    if (Clients[clientId].Udp.EndPoint == null)
                    {
                        Clients[clientId].Udp.Connect(clientEndPoint);
                        return;
                    }

                    if (Clients[clientId].Udp.EndPoint.ToString() == clientEndPoint.ToString())
                    {
                        Clients[clientId].Udp.HandleData(packet);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving UDP data: {ex}");
            }
        }

        public static void SendUdpData(IPEndPoint clientEndPoint, Packet packet)
        {
            try
            {
                if (clientEndPoint != null)
                {
                    _udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data to {clientEndPoint} via UDP: {ex}");
            }
        }

        private static void TcpConnectCallback(IAsyncResult result)
        {
            TcpClient client = _tcpListener.EndAcceptTcpClient(result);
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallback),null);
            Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}...");

            for (int i = 1; i <= MaxPlayer; i++)
            {
                if (Clients[i].Tcp.Socket == null)
                {
                    Clients[i].Tcp.Connect(client);
                    return;
                }
            }

            Console.WriteLine($"{client.Client.RemoteEndPoint} failed to connect: Server full!");
        }
        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayer; i++)
            {
                Clients.Add(i, new Client(i));
            }

            PacketHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.WelcomeReceived, ServerHandle.WelcomeReceived },
                { (int)ClientPackets.UpdTestReceived, ServerHandle.UdpTestReceived },
                { (int)ClientPackets.PlayerMovement, ServerHandle.PlayerMovement }
                
            };
            Console.WriteLine("Initialized packets.");
        }
    }
}