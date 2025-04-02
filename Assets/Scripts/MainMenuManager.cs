using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;


[Serializable]
public class UIItem
{
    public string UIName;
    public GameObject UIObject;
}

public class MainMenuManager : MonoBehaviour
{
    public CustomNetworkManager NetManager;

    public UIItem[] MainMenuUIs;

    // lobby shit that should be moved to a lobby ui manager.

    public TMP_Text LobbyNamePlate;

    public GameObject PlayerListItem;

    public GameObject PlayerListSpawnLocation;

    public GameObject StartGameButton;


    // server list

    public GameObject ServerListItem;

    public GameObject ServerListSpawnLocation;



    void Awake()
    {
        ShowMenu("mainmenu");
    }

    void Start()
    {
        NetManager.OnJoinLobby += OnJoinedLobby;
        NetManager.OnPlayerJoined += OnPlayerJoined;
        NetManager.OnPlayedLeft += OnPlayedLeft;
    }

    private void OnPlayedLeft(List<int> list)
    {
        ClearLobbyOfNamePlates();

        foreach (var player in NetManager.CurrentLobby.Players)
        {
            GameObject playerNamePlate = Instantiate(PlayerListItem, PlayerListSpawnLocation.transform);

            string name = "anonymous";
            if (player.Profile != null) name = player.Profile.Name;


            playerNamePlate.GetComponent<PlayerListItem>()?.SetValues(name, player.Id);
        }
    }

    private void OnPlayerJoined(List<LobbyPlayerJoined> list)
    {
        ClearLobbyOfNamePlates();

        foreach (var player in NetManager.CurrentLobby.Players)
        {
            GameObject playerNamePlate = Instantiate(PlayerListItem, PlayerListSpawnLocation.transform);

            string name = "anonymous";
            if (player.Profile != null) name = player.Profile.Name;


            playerNamePlate.GetComponent<PlayerListItem>()?.SetValues(name, player.Id);
        }
    }

    void Update()
    {
        if (IsInMenu("serverlist"))
        {

        }
    }

    public void CallRefreshServerList()
    {
        RefreshServerList();
    }

    // its actually a lobby not a server.
    public async Task RefreshServerList()
    {
        Transform[] serverListItems = ServerListSpawnLocation.transform.GetComponentsInChildren<Transform>();

        for (int i = serverListItems.Length - 1; i > 0; i--)
        {
            Destroy(serverListItems[i].gameObject);
        }

        QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

        foreach (var server in queryResponse.Results)
        {
            GameObject serverItem = Instantiate(ServerListItem, ServerListSpawnLocation.transform);

            string serverID = (server.IsPrivate || server.IsLocked ? null : server.Id);

            serverItem.GetComponent<ServerListItem>()?.SetUpServerListItem(server.Name, server.IsPrivate, serverID);
        }
    }


    private void OnJoinedLobby()
    {
        Debug.Log("Joined lobby");

        ShowMenu("lobby");

        ClearLobbyOfNamePlates();

        foreach (var player in NetManager.CurrentLobby.Players)
        {
            GameObject playerNamePlate = Instantiate(PlayerListItem, PlayerListSpawnLocation.transform);

            string name = "anonymous";
            if (player.Profile != null) name = player.Profile.Name;


            playerNamePlate.GetComponent<PlayerListItem>()?.SetValues(name, player.Id);
        }

        Debug.Log("Player list populated");


        if (NetManager.CurrentLobby.HostId == AuthenticationService.Instance.PlayerInfo.Id)
        {
            Debug.Log("Player is host");
            StartGameButton.SetActive(true);
        }
        else
        {
            Debug.Log("Player is not host");
            StartGameButton.SetActive(false);
        }

        LobbyNamePlate.text = NetManager.CurrentLobby.Name;
    }

    private void ClearLobbyOfNamePlates()
    {
        Transform[] namePlates = PlayerListSpawnLocation.transform.GetComponentsInChildren<Transform>();

        for (int i = namePlates.Length - 1; i > 0; i--)
        {
            Destroy(namePlates[i].gameObject);
        }
    }

    public void ShowMenu(string uiName)
    {
        foreach (var item in MainMenuUIs)
        {
            if (item.UIName == uiName) item.UIObject.SetActive(true);
            else item.UIObject.SetActive(false);
        }
    }

    public bool IsInMenu(string uiName)
    {
        foreach (var item in MainMenuUIs)
        {
            if (item.UIName == uiName) return item.UIObject.activeSelf;
        }

        return false;
    }
}
