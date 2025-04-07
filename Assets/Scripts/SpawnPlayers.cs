using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnPlayers : NetworkBehaviour
{
    public Transform PlayerOneStart;
    public Transform PlayerTwoStart;

    // Start is called before the first frame update
    void Start()
    {
        if (IsServer || IsHost) SpawnPlayersServerRPC(PlayerOneStart.position, PlayerTwoStart.position);
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayersServerRPC(Vector3 p1Spawn, Vector3 p2Spawn)
    {
        GamePersistent.Instance.SpawnPlayersServerRPC(p1Spawn, p2Spawn);
    }
}
