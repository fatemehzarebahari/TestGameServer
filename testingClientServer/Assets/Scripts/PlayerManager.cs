using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int id;
    public string username;

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
    }
}
