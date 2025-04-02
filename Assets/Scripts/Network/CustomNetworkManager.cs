using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public static CustomNetworkManager Instance { get; private set; }

    public void ConnectToIPAndPort(string ip, ushort port)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
        ip,  // The IP address is a string
        port // The port number is an unsigned short
        );

        StartClient();
    }

    public void HostIPAndPort(string ip, ushort port)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
        ip,  // The IP address is a string
        port, // The port number is an unsigned short
        "0.0.0.0"
        );

        StartHost();
    }

    public static bool IsHeadlessMode()
    {
        return Application.isBatchMode;
    }
}
