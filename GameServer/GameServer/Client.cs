using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace GameServer
{
    public class Client
    {
        private static readonly int DataBufferSize = 4096;
        public int Id;
        public TCP Tcp;
        public UDP Udp;
        public Player Player;

        public Client(int clientId)
        {
            Id = clientId;
            Tcp = new TCP(clientId);
            Udp = new UDP(clientId);
        }

        public class TCP
        {
            public TcpClient Socket;

            private NetworkStream _stream;
            private readonly int _id;
            private byte[] _receiveBuffer;
            private Packet _receivedData;

            public TCP(int id)
            {
                _id = id;
            }

            public void Connect(TcpClient socket)
            {
                Socket = socket;
                Socket.ReceiveBufferSize = DataBufferSize;
                Socket.SendBufferSize = DataBufferSize;
                _stream = socket.GetStream();

                _receivedData = new Packet();
                _receiveBuffer = new byte[DataBufferSize];

                _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);

                ServerSend.Welcome(_id, "Welcome To The Server");
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if (Socket != null)
                    {
                        _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending data to player {_id} via TCP: {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = _stream.EndRead(result);
                    if (byteLength <= 0)
                    {
                        Server.Clients[_id].Disconnect();
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(_receiveBuffer, data, byteLength);

                    _receivedData.Reset(HandleData(data));

                    _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {ex}");
                    Server.Clients[_id].Disconnect();
                }
            }

            private bool HandleData(byte[] data)
            {
                int packetLength = 0;

                _receivedData.SetBytes(data);

                if (_receivedData.UnreadLength() >= 4)
                {
                    packetLength = _receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }

                while (packetLength > 0 && packetLength <= _receivedData.UnreadLength())
                {
                    byte[] packetBytes = _receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet packet = new Packet(packetBytes))
                        {
                            int packetId = packet.ReadInt();
                            Server.PacketHandlers[packetId](_id, packet);
                        }
                    });

                    packetLength = 0;
                    if (_receivedData.UnreadLength() >= 4)
                    {
                        packetLength = _receivedData.ReadInt();
                        if (packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                if (packetLength <= 1)
                {
                    return true;
                }

                return false;
            }

            public void Disconnect()
            {
                Socket.Close();
                _stream = null;
                _receiveBuffer = null;
                _receivedData = null;
                Socket = null;
            }
        }

        public class UDP
        {
            public IPEndPoint EndPoint;

            private int _id;

            public UDP(int id)
            {
                _id = id;
            }

            public void Connect(IPEndPoint endPoint)
            {
                EndPoint = endPoint;
                ServerSend.UdpTest(_id);
            }

            public void SendData(Packet packet)
            {
                Server.SendUdpData(EndPoint, packet);
            }

            public void HandleData(Packet packetData)
            {
                int packetLength = packetData.ReadInt();
                byte[] packetBytes = packetData.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.ReadInt();
                        Server.PacketHandlers[packetId](_id, packet);
                    }
                });
            }

            public void Disconnect()
            {
                EndPoint = null;
            }

        }

        public void SendIntoGame(string playerName)
        {
            Player = new Player(Id, playerName, new Vector3(0, 0, 0));
            foreach (Client client in Server.Clients.Values)
            {
                if (client.Player != null)
                {
                    if (client.Id != Id)
                    {
                        ServerSend.SpawnPlayer(Id, client.Player);
                    }
                }
            }

            foreach (Client client in Server.Clients.Values)
            {
                if (client.Player != null)
                {
                    ServerSend.SpawnPlayer(client.Id, Player);
                }
            }
            
        }

        private void Disconnect()
        {
            Console.WriteLine($"{Tcp.Socket.Client.RemoteEndPoint} Disconnected");
            Player = null;
            Tcp.Disconnect();
            Udp.Disconnect();
        }

    }

}
  
