using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreGameCanvas : NetworkBehaviour
{
    public GameObject PreGameObject;

    public GameObject DisconnectReason;

    public TMP_Text PlayerCount;
    public TMP_Text RoundStats;

    public Image TimerImage;

    public float SecondsToWaitBeforeStarting = 30f;

    private NetworkVariable<float> _timer = new NetworkVariable<float>();

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
        _timer.OnValueChanged += UpdateTimer;


    }

    private void UpdateTimer(float previousValue, float newValue)
    {
        TimerImage.fillAmount = newValue / SecondsToWaitBeforeStarting;
    }

    void OnDisable()
    {
        NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
    }

    void Update()
    {
        RoundStats.text = $"Current round: {GamePersistent.Instance.CurrentRound.Value}\n"
        + $"P1: {GamePersistent.Instance.PlayerScore.Value.x} | P2: {GamePersistent.Instance.PlayerScore.Value.y}";

        if ((IsServer || IsHost) && PlayerCountNumber.Value >= 2) // ah, this pains me. should be fine for a small game.
        {
            _timer.Value = _timer.Value -= Time.deltaTime;
        }
        else if (IsServer || IsHost)
        {
            _timer.Value = SecondsToWaitBeforeStarting;
        }


    }

    public void ForceStartGame()
    {
        if (IsServer || IsHost)
        {
            GamePersistent.Instance.StartGame();
        }
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
