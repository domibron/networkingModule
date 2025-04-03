using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PreGameCanvas : NetworkBehaviour
{
    public GameObject PreGameObject;

    public GameObject DisconnectReason;

    public TMP_Text PlayerCount;

    public NetworkVariable<int> PlayerCountNumber = new NetworkVariable<int>(0);

    void Start()
    {
        NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            PlayerCountNumber.Value = NetworkManager.Singleton.ConnectedClients.Count;
        }

        PlayerCount.text = "Players\n" + PlayerCountNumber.Value;

        PlayerCountNumber.OnValueChanged += UpdatePlayerCount;

    }

    private void OnServerStopped(bool obj)
    {
        DisconnectFromServer();
    }

    public void UpdatePlayerCount(int previous, int current)
    {
        PlayerCount.text = "Player count = " + current;
    }

    private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        if (manager.IsServer || manager.IsHost)
        {
            PlayerCountNumber.Value = manager.ConnectedClients.Count;
        }

        switch (data.EventType)
        {
            case ConnectionEvent.ClientDisconnected:
                if (data.ClientId == manager.LocalClientId)
                    DisconnectFromServer();

                break;
        }
    }

    // after we know we are not present, we clean up and remove the network manager.
    public void DisconnectFromServer()
    {
        GameObject NetworkManagerObject = NetworkManager.Singleton.gameObject;

        NetworkManager.Singleton.Shutdown();

        string reason = NetworkManager.Singleton.DisconnectReason;

        if (string.IsNullOrEmpty(reason)) reason = "User disconnected";

        GameObject disconnectReason = Instantiate(DisconnectReason);
        disconnectReason.GetComponent<DisconnectReason>().reason = reason;

        Destroy(NetworkManagerObject); // we don't want it to go wrong.


        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }


}
