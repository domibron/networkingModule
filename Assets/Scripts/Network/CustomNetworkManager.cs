using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public static CustomNetworkManager Instance { get; private set; }

    public Lobby CurrentLobby = null;

    public ILobbyEvents LobbyEvent = null;

    public event Action OnJoinLobby;

    public event Action<List<LobbyPlayerJoined>> OnPlayerJoined;
    public event Action<List<int>> OnPlayedLeft;

    public string PlayerID;

    private Coroutine heartBeat;

    async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        PlayerID = AuthenticationService.Instance.PlayerId;
    }

    // Start is called before the first frame update
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CallJoinLobby(string serverID)
    {
        print("Attempting to join");
        if (string.IsNullOrEmpty(serverID)) return;

        JoinLobby(serverID);
    }


    public void StartGame()
    {
        StartHost();
    }

    public void CallCreateLobby()
    {
        CreateLobby();
    }

    public async Task JoinLobby(string lobbyID)
    {
        if (CurrentLobby != null) return;

        CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID);

        print("Joined target lobby");

        OnJoinLobby?.Invoke();
    }

    public async Task CreateLobby()
    {
        if (CurrentLobby != null) return;

        string lobbyName = "lobby name " + UnityEngine.Random.Range(0, 99999);
        int maxPlayers = 2;

        CreateLobbyOptions options = new CreateLobbyOptions();
        options.IsPrivate = false;

        options.Player = new Player(
        id: AuthenticationService.Instance.PlayerId
        );

        CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        var callBacks = new LobbyEventCallbacks();
        callBacks.KickedFromLobby += KickedFromLobby;
        callBacks.LobbyChanged += LobbyChanged;
        callBacks.LobbyEventConnectionStateChanged += LobbyEventConnectionStateChanged;
        callBacks.PlayerJoined += PlayerJoined;
        callBacks.PlayerLeft += PlayerLeft;


        LobbyEvent = await LobbyService.Instance.SubscribeToLobbyEventsAsync(CurrentLobby.Id, callBacks);

        Debug.Log("In lobby");

        OnJoinLobby?.Invoke();

        heartBeat = StartCoroutine(HeartbeatLobbyCoroutine(CurrentLobby.Id, 15f));

    }

    private void PlayerLeft(List<int> list)
    {
        OnPlayedLeft?.Invoke(list);
    }

    private void PlayerJoined(List<LobbyPlayerJoined> list)
    {
        OnPlayerJoined?.Invoke(list);
    }

    private void LobbyEventConnectionStateChanged(LobbyEventConnectionState state)
    {

    }

    public void KickedFromLobby()
    {

    }

    public void LobbyChanged(ILobbyChanges lobbyChanges)
    {

    }

    public static bool IsHeadlessMode()
    {
        return Application.isBatchMode;
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }
}
