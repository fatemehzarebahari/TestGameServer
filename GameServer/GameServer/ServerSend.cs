namespace GameServer
{
    public class ServerSend
    {
        public static void Welcome(int toClient, string msg)
        {
            using (Packet packet = new Packet((int)ServerPackets.Welcome))
            {
                packet.Write(msg);
                packet.Write(toClient);
                SendTcpData(toClient,packet);
            }
        }

        public static void SpawnPlayer(int toClient, Player player)
        {
            using (Packet packet = new Packet((int)ServerPackets.SpawnPlayer))
            {
                packet.Write(player.Id);
                packet.Write(player.Username);
                packet.Write(player.Position);
                packet.Write(player.Rotation);
                
                SendTcpData(toClient, packet);
            }
        }
        public static void PlayerPosition(Player player)
        {
            using (Packet packet = new Packet((int)ServerPackets.PlayerPosition))
            {
                packet.Write(player.Id);
                packet.Write(player.Position);
                SendUdpDataToAll(packet);
            }
        }

        public static void PlayerRotation(Player player)
        {
            using (Packet packet = new Packet((int)ServerPackets.PlayerRotation))
            {
                packet.Write(player.Id);
                packet.Write(player.Rotation);
                SendUdpDataToAll(player.Id,packet);
            }
        }

        private static void SendTcpData(int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.Clients[toClient].Tcp.SendData(packet);
        }
        private static void SendTcpDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayer; i++)
            {
                Server.Clients[i].Tcp.SendData(packet);
            }
        }
        private static void SendTcpDataToAll(int exceptionClientId,Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayer; i++)
            {
                if (i != exceptionClientId)
                {
                    Server.Clients[i].Tcp.SendData(packet);
                }
            }
        }

        public static void UdpTest(int toClient)
        {
            using (Packet packet = new Packet((int)ServerPackets.UdpTest))
            {
                packet.Write("A test packet for UDP.");

                SendUdpData(toClient, packet);
            }
        }

        private static void SendUdpData(int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.Clients[toClient].Udp.SendData(packet);
        }
        private static void SendUdpDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayer; i++)
            {
                Server.Clients[i].Udp.SendData(packet);
            }
        }
        private static void SendUdpDataToAll(int exceptClient, Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayer; i++)
            {
                if (i != exceptClient)
                {
                    Server.Clients[i].Udp.SendData(packet);
                }
            }
        }


    }
}