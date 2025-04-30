using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles the pre game UI.
/// </summary>
public class PreGameCanvas : NetworkBehaviour
{
    public GameObject PreGameObject;

    public GameObject DisconnectReason;

    public TMP_Text PlayerCount;
    public TMP_Text RoundStats;

    public Image TimerImage;

    public float SecondsToWaitBeforeStarting = 15f;

    public NetworkVariable<int> PlayerCountNumber = new NetworkVariable<int>(0);

    private NetworkVariable<float> _timer = new NetworkVariable<float>();

    #region Start
    void Start()
    {
        NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;

        if (IsServer || IsHost)
        {
            PlayerCountNumber.Value = NetworkManager.Singleton.ConnectedClients.Count;
            _timer.Value = SecondsToWaitBeforeStarting;
        }

        PlayerCount.text = "Players\n" + PlayerCountNumber.Value;

        PlayerCountNumber.OnValueChanged += UpdatePlayerCount;
        _timer.OnValueChanged += UpdateTimer;

    }
    #endregion

    #region Update
    void Update()
    {
        RoundStats.text = $"Current round: {GamePersistent.Instance.CurrentRound.Value}\n"
        + $"P1: {GamePersistent.Instance.PlayerOneScore.Value} | P2: {GamePersistent.Instance.PlayerTwoScore.Value}";

        if ((IsServer || IsHost) && PlayerCountNumber.Value >= 2)
        {
            _timer.Value = _timer.Value -= Time.deltaTime;
        }
        else if (IsServer || IsHost)
        {
            _timer.Value = SecondsToWaitBeforeStarting;
        }

        if ((IsServer || IsHost) && _timer.Value <= 0)
        {
            GamePersistent.Instance.StartGame();
        }

    }
    #endregion

    #region UpdateTimer
    private void UpdateTimer(float previousValue, float newValue)
    {
        TimerImage.fillAmount = newValue / SecondsToWaitBeforeStarting;
    }
    #endregion

    #region OnDisable
    void OnDisable()
    {
        // unsubscribe to any events to prevent any issues.
        NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
    }
    #endregion

    #region ForceStartGame
    public void ForceStartGame()
    {
        if (IsServer || IsHost)
        {
            GamePersistent.Instance.StartGame();
        }
    }
    #endregion

    #region OnServerStopped
    private void OnServerStopped(bool obj)
    {
        DisconnectFromServer();
    }
    #endregion

    #region UpdatePlayerCount
    public void UpdatePlayerCount(int previous, int current)
    {
        PlayerCount.text = "Players\n" + PlayerCountNumber.Value;
    }
    #endregion

    #region OnConnectionEvent
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
    #endregion

    #region DisconnectFromServer
    // after we know we are not present, we clean up and remove the network manager.
    public void DisconnectFromServer() // Duplicated on GamePersistent.
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
    #endregion


}
