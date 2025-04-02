using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinServerButton : MonoBehaviour
{
    public ServerListItem serverListItem;

    public void JoinServer()
    {
        CustomNetworkManager.Instance.CallJoinLobby(serverListItem.ServerID);
    }
}
