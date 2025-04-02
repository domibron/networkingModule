using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ServerListItem : MonoBehaviour
{
    public string ServerName;

    public string ServerID;

    public string ServerLobbyCode;

    public bool IsPrivate;

    public TMP_Text ServerText;

    public void SetUpServerListItem(string serverName, bool isPrivate = false, string serverID = null, string serverLobbyCode = null)
    {
        ServerName = serverName;

        ServerID = serverID;

        IsPrivate = isPrivate;

        ServerLobbyCode = serverLobbyCode;

        ServerText.text = $"{serverName}\nPrivate: " + (isPrivate ? "Yes" : "No") + " | Server ID: " + (isPrivate ? "HIDDEN" : serverID);
    }
}
