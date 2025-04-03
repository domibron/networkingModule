using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public static CustomNetworkManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        OnConnectionEvent += OnConnectionEventTriggered;
    }



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

    public void StartServerIPAndPort(string ip, ushort port)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
        ip,  // The IP address is a string
        port, // The port number is an unsigned short
        "0.0.0.0"
        );

        StartServer();
    }


    private void OnConnectionEventTriggered(NetworkManager manager, ConnectionEventData data)
    {
        if (!manager.IsServer && !manager.IsHost) return;

        if (manager.ConnectedClients.Count > 2)
        {
            KickPlayer(data.ClientId);
        }
    }

    [ServerRpc]
    private void KickPlayer(ulong clientId)
    {
        DisconnectClient(clientId, "Kicked because the server was full!");
    }

    public static bool IsHeadlessMode()
    {
        return Application.isBatchMode;
    }
}
