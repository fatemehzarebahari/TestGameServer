using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client : MonoBehaviour
{
    public static Client Instance;
    public static int DataBufferSize = 4096;

    public string ip = "127.0.0.1";
    public int port = 26950;
    public int myId = 0;
    public TCP tcp;
    public UDP udp;

    private bool _isConnected = false;
    
    private delegate void PacketHandler(Packet _packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();
    }

    public void ConnectToServer()
    { 
        InitializeClientData();
        _isConnected = true;
        tcp.Connect();
    }

    public class TCP
    {
        public TcpClient Socket;

        private NetworkStream _stream;
        private byte[] _receiveBuffer;
        private Packet _receivedData;

        public void Connect()
        {
            Socket = new TcpClient
            {
                ReceiveBufferSize = DataBufferSize,
                SendBufferSize = DataBufferSize
            };

            _receiveBuffer = new byte[DataBufferSize];
            Socket.BeginConnect(Instance.ip, Instance.port, ConnectCallback, Socket);
        }

        private void ConnectCallback(IAsyncResult result)
        {
            Socket.EndConnect(result);

            if (!Socket.Connected)
            {
                return;
            }

            _stream = Socket.GetStream();
            
            _receivedData = new Packet();
            
            _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = _stream.EndRead(result);
                if (byteLength <= 0)
                {
                    Instance.Disconnect();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(_receiveBuffer, data, byteLength);
                _receivedData.Reset(HandleData(data));
                _stream.BeginRead(_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                Disconnect();
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
                byte[] _packetBytes = _receivedData.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        packetHandlers[_packetId](_packet);
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

        public void SendData(Packet packet)
        {
            try
            {
                if (Socket != null)
                {
                    _stream.BeginWrite(packet.ToArray(),0,packet.Length(),null,null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error sending data to server via TCP: {ex}");
            }
        }

        private void Disconnect()
        {
            Instance.Disconnect();
            Socket = null;
            _stream = null;
            _receivedData = null;
            _receiveBuffer = null;
            
        }
    }

     public class UDP
    {
        public UdpClient Socket;
        public IPEndPoint EndPoint;

        public UDP()
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(Instance.ip), Instance.port);
        }

        public void Connect(int localPort)
        {
            Socket = new UdpClient(localPort);

            Socket.Connect(EndPoint);
            Socket.BeginReceive(ReceiveCallback, null);

            using (Packet packet = new Packet())
            {
                SendData(packet);
            }
        }

        public void SendData(Packet packet)
        {
            try
            {
                packet.InsertInt(Instance.myId);
                if (Socket != null)
                {
                    Socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error sending data to server via UDP: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                byte[] data = Socket.EndReceive(result, ref EndPoint);
                Socket.BeginReceive(ReceiveCallback, null);

                if (data.Length < 4)
                {
                    Instance.Disconnect();
                    return;
                }

                HandleData(data);
            }
            catch
            {
                Disconnect();
            }
        }

        private void HandleData(byte[] data)
        {
            using (Packet packet = new Packet(data))
            {
                int packetLength = packet.ReadInt();
                data = packet.ReadBytes(packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet packet = new Packet(data))
                {
                    int packetId = packet.ReadInt();
                    packetHandlers[packetId](packet);
                }
            });
        }

        private void Disconnect()
        {
            Instance.Disconnect();
            Socket = null;
            EndPoint = null;
        }
    }
    
    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.Welcome, ClientHandle.Welcome },
            { (int)ServerPackets.UDPTest, ClientHandle.UDPTest },
            { (int)ServerPackets.SpawnPlayer, ClientHandle.Spawn},
            { (int)ServerPackets.PlayerPosition, ClientHandle.PlayerPosition},
            { (int)ServerPackets.PlayerRotation, ClientHandle.PlayerRotation}
        };
        Debug.Log("Initialized packets.");
    }

    private void Disconnect()
    {
        _isConnected = false;
        
        tcp.Socket.Close();
        udp.Socket.Close();
        
        
        Debug.Log("Disconnected from server");
    }
    
}
