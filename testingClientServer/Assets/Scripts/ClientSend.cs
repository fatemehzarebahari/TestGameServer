using UnityEngine;

public class ClientSend : MonoBehaviour
{
    public static void WelcomeReceived()
    {
        using (Packet packet = new Packet((int)ClientPackets.WelcomeReceived))
        {
            packet.Write(Client.Instance.myId);
            packet.Write(UIManager.Instance.usernameField.text);

            SendTcpData(packet);
        }
    }
    public static void PlayerMovement(bool[] inputs)
    {
        using (Packet packet = new Packet((int)ClientPackets.PlayerMovement))
        {
            packet.Write(inputs.Length);
            foreach (bool input in inputs)
            {
                packet.Write(input);
            }

            packet.Write(GameManager.Players[Client.Instance.myId].transform.rotation);
            SendUDPData(packet);
        }
    }

    private static void SendTcpData(Packet packet)
    {
        packet.WriteLength();

        Client.Instance.tcp.SendData(packet);
    }

    public static void UDPTestReceived()
    {
        using (Packet packet = new Packet((int)ClientPackets.UpdTestReceived))
        {
            packet.Write("Received a UDP packet.");

            SendUDPData(packet);
        }
    }

    private static void SendUDPData(Packet packet)
    {
        packet.WriteLength();
        Client.Instance.udp.SendData(packet);
    }
}
