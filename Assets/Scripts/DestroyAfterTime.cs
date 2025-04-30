using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DestroyAfterTime : NetworkBehaviour
{
    public float WaitForSeconds = 30f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer || IsHost)
            StartCoroutine(Despawn(WaitForSeconds));
    }


    private IEnumerator Despawn(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        GetComponent<NetworkObject>().Despawn();
    }
}
