using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BaseCollectable : NetworkBehaviour
{

    protected bool _hasCollectedCollectable = false;


    [Rpc(SendTo.Server)]
    protected virtual void PickUpCollectableServerRPC(ulong playerID)
    {
        // will remove the object
        GetComponent<NetworkObject>().Despawn();
    }

}
