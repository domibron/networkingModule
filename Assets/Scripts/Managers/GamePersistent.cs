using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePersistent : NetworkBehaviour
{
    public static GamePersistent Instance { get; private set; }

    public NetworkVariable<int> PlayerOneScore = new NetworkVariable<int>(0);
    public NetworkVariable<int> PlayerTwoScore = new NetworkVariable<int>(0);

    public NetworkVariable<int> CurrentRound = new NetworkVariable<int>(1);

    // public NetworkVariable<float> Timer = new NetworkVariable<float>(300f);

    public NetworkVariable<bool> InRound = new NetworkVariable<bool>(false);

    public NetworkObject ArenaPlayer;

    public GameObject WinnerTextBoxContainer;
    public TMP_Text WinnerTextBox;

    public GameObject DisconnectReasonPrefab;
    // public NetworkVariable<Dictionary<ulong, NetworkObject>> ArenaObjects;

    // TODO can turn this into rpc calls and let client calculate all this.


    // we need to keep track of the players, but we cannot store them here.

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Timer.OnValueChanged += OnTimerChanged;
        InRound.OnValueChanged += OnInRoundChanged;

        NetworkManager.OnConnectionEvent += OnConnectionEvent;

        WinnerTextBoxContainer.SetActive(false);

        // if (IsServer || IsHost)
        // {
        //     // Timer.Value = RoundTime;
        // }
    }

    [Rpc(SendTo.Server)]
    private void ResetEveryThingServerRPC()
    {
        PlayerOneScore.Value = 0;
        PlayerTwoScore.Value = 0;

        CurrentRound.Value = 1;
    }

    private void OnInRoundChanged(bool previousValue, bool newValue)
    {
        // if (newValue && (IsHost || IsServer))
        // {
        //     Timer.Value = RoundTime;
        // }
    }

    private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        if (data.EventType == ConnectionEvent.ClientDisconnected)
        {
            // TODO figure this out



            if (data.ClientId == manager.LocalClientId) Destroy(gameObject); // we dont want this to be on the main menu
            else if (manager.IsHost || manager.IsServer)
            {
                // call end game
                ResetEveryThingServerRPC();
                WrapEverythingUpAndLeaveGameSceneServerRPC(false);
            }
            else
            {

            }
        }
    }

    void OnDisable()
    {
        NetworkManager.OnConnectionEvent -= OnConnectionEvent;
        // KickAllServerRPC();
    }

    public override void OnDestroy()
    {
        KickAllServerRPC();
        base.OnDestroy();
    }

    [Rpc(SendTo.Server)]
    private void KickAllServerRPC()
    {
        foreach (var client in NetworkManager.ConnectedClients.Values)
        {
            if (NetworkManager.Singleton.ServerIsHost && client.ClientId == NetworkManager.Singleton.ConnectedClientsIds[0]) continue;

            NetworkManager.Singleton.DisconnectClient(client.ClientId, "Server Closed");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        // if ((IsServer || IsHost) && Timer.Value > 0)
        // {
        //     Timer.Value -= Time.deltaTime;
        // }
        // else if (Timer.Value <= 0 && (IsServer || IsHost) && !InRound.Value)
        // {
        //     InRound.Value = true;
        //     // Dont think we need this. since somewhere in unity docs said it does scene syncing automatically.
        //     //NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        // }

        if (InRound.Value)
        {
            if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;

            if (Cursor.visible) Cursor.visible = false;
        }
        else
        {
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;

            if (!Cursor.visible) Cursor.visible = true;
        }
    }

    [Rpc(SendTo.Server)]
    public void EndRoundServerRPC(bool thereIsAWinner = false, ulong winnerID = 0)
    {
        if (!thereIsAWinner)
        {
            // no one wins. no point


            DisplayDrawRPC();
            return;
        }

        if (winnerID == NetworkManager.Singleton.ConnectedClientsIds[0])
        {
            // p1 win
            PlayerOneScore.Value++;
            // WrapEverythingUpAndLeaveGameSceneServerRPC();


        }
        else
        {
            // p2 win
            PlayerTwoScore.Value++;

        }
        // compare winners?

        DisplayWinnersEveryOneRPC(winnerID);






    }

    [Rpc(SendTo.Everyone)]
    private void DisplayWinnersEveryOneRPC(ulong winnerID)
    {
        StartCoroutine(DisplayWinner(winnerID));
    }

    [Rpc(SendTo.Everyone)]
    private void DisplayDrawRPC()
    {
        StartCoroutine(DisplayDraw());
    }

    private IEnumerator DisplayWinner(ulong winnerID)
    {
        WinnerTextBox.text = (winnerID == NetworkManager.Singleton.ConnectedClientsIds[0] ? "Player <color=blue>One</color> Wins The Round!" : "Player <color=red>Two</color> Wins The Round!");
        WinnerTextBoxContainer.SetActive(true);

        yield return new WaitForSeconds(2);

        if (CurrentRound.Value >= 5 || PlayerOneScore.Value >= 3 || PlayerTwoScore.Value >= 3)
        {
            // winner of the game
            WinnerTextBox.text = (PlayerOneScore.Value >= 3 ? "Player <color=blue>One</color> Has Won The Game!" : "Player <color=red>Two</color> Has Won The Game!");
            yield return new WaitForSeconds(2);

            ResetEveryThingServerRPC();
        }

        WinnerTextBoxContainer.SetActive(false);

        if (IsHost || IsServer)
        {
            WrapEverythingUpAndLeaveGameSceneServerRPC();
        }
    }

    private IEnumerator DisplayDraw()
    {
        WinnerTextBox.text = "No One Wins The Round! Again!";
        WinnerTextBoxContainer.SetActive(true);

        yield return new WaitForSeconds(2);

        WinnerTextBoxContainer.SetActive(false);

        if (IsHost || IsServer)
        {
            WrapEverythingUpAndLeaveGameSceneServerRPC(false);
        }
    }

    [Rpc(SendTo.Server)]
    public void WrapEverythingUpAndLeaveGameSceneServerRPC(bool someOneWon = true)
    {
        InRound.Value = false;
        if (someOneWon) CurrentRound.Value++;
        NetworkManager.Singleton.SceneManager.LoadScene("PreGameScene", LoadSceneMode.Single);
    }

    public void StartGame()
    {
        if (!IsHost && !IsServer) return;

        if (InRound.Value) return;

        InRound.Value = true;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        // Timer.Value = RoundTime;
    }

    // TODO use transforms to align rotation to spawn points.
    [Rpc(SendTo.Server)]
    public void SpawnPlayersServerRPC(Vector3 playerOneStart, Vector3 playerTwoStart)
    {
        int playerCount = 0;
        foreach (var player in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Vector3 spawnPoint = playerCount == 0 ? playerOneStart : playerTwoStart;

            // print(spawnPoint);

            NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(ArenaPlayer, player, true, true, false, spawnPoint, Quaternion.identity);
            playerCount++;

            playerObject.GetComponent<PlayerController>().SetLocationOwnerRPC(spawnPoint);

            //playerObject.transform.localScale = Vector3.one;
            // playerObject.GetComponent<CharacterController>().enabled = false;
            //playerObject.GetComponent<AnticipatedNetworkTransform>().SetState(spawnPoint, teleportDisabled: false);
            // playerObject.GetComponent<CharacterController>().enabled = true;

        }
    }

    public void DisconnectFromServer(string message = "User disconnected")
    {
        GameObject NetworkManagerObject = NetworkManager.Singleton.gameObject;

        NetworkManager.Singleton.Shutdown();

        string reason = NetworkManager.Singleton.DisconnectReason;

        if (string.IsNullOrEmpty(reason)) reason = message;

        GameObject disconnectReason = Instantiate(DisconnectReasonPrefab);
        disconnectReason.GetComponent<DisconnectReason>().reason = reason;

        Destroy(NetworkManagerObject); // we don't want it to go wrong.


        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }


}
