
using System;
using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    public static void Welcome(Packet packet)
    {
        string msg = packet.ReadString();
        int myId = packet.ReadInt();
        
        Debug.Log($"Message from server: {msg}");
        Client.Instance.myId = myId;
        
        ClientSend.WelcomeReceived();
        Client.Instance.udp.Connect(((IPEndPoint)Client.Instance.tcp.Socket.Client.LocalEndPoint).Port);
    }

    public static void Spawn(Packet packet)
    {
        int id = packet.ReadInt();
        string username = packet.ReadString();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();

        GameManager.Instance.SpawnPlayer(id, username, position, rotation);
    }

    public static void PlayerPosition(Packet packet)
    {
        try
        {
            int id = packet.ReadInt();
            GameManager.Players[id].transform.position = packet.ReadVector3();
        }
        catch (Exception ex)
        {
            Debug.Log($"error in player position update: {ex}");
        }
    }

    public static void PlayerRotation(Packet packet)
    {
        try{
            int id = packet.ReadInt();
            GameManager.Players[id].transform.rotation = packet.ReadQuaternion();
        }
        catch (Exception ex)
        {
            Debug.Log($"error in player position update: {ex}");
        }
    }

    public static void UDPTest(Packet packet)
    {
        string msg = packet.ReadString();

        Debug.Log($"Received packet via UDP. Contains message: {msg}");
        ClientSend.UDPTestReceived();
    }
}
