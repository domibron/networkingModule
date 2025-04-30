using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePersistent : NetworkBehaviour
{
    public static GamePersistent Instance { get; private set; }

    public NetworkVariable<Vector2Int> PlayerScore = new NetworkVariable<Vector2Int>(new Vector2Int(0, 0));

    public NetworkVariable<int> CurrentRound = new NetworkVariable<int>(0);

    // public NetworkVariable<float> Timer = new NetworkVariable<float>(300f);

    public float RoundTime = 120f;

    public NetworkVariable<bool> InRound = new NetworkVariable<bool>(false);

    public NetworkObject ArenaPlayer;

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
        PlayerScore.OnValueChanged += OnScoreChanged;
        // Timer.OnValueChanged += OnTimerChanged;
        InRound.OnValueChanged += OnInRoundChanged;

        NetworkManager.OnConnectionEvent += OnConnectionEvent;

        // if (IsServer || IsHost)
        // {
        //     // Timer.Value = RoundTime;
        // }
    }

    private void OnInRoundChanged(bool previousValue, bool newValue)
    {
        // if (newValue && (IsHost || IsServer))
        // {
        //     Timer.Value = RoundTime;
        // }
    }

    private void OnTimerChanged(float previousValue, float newValue)
    {
        // UI Update
    }

    private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        if (data.EventType == ConnectionEvent.ClientDisconnected)
        {
            // end the game.

            if (data.ClientId == manager.LocalClientId) Destroy(gameObject); // we dont want this to be on the main menu
            else if (manager.IsHost || manager.IsServer)
            {
                // call end game
            }
        }
    }

    void OnDisable()
    {
        NetworkManager.OnConnectionEvent -= OnConnectionEvent;

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
    }


    public void OnScoreChanged(Vector2Int oldValue, Vector2Int newValue)
    {
        // do ui stuff?
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


}
