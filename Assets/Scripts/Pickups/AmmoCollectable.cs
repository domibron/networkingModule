using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AmmoCollectable : NetworkBehaviour
{
    private bool _hasCollectedCollectable = false;
    public int AmmoGiveAmount = 30;

    public AudioClip CollectAudioClip;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && other.gameObject.GetComponent<NetworkObject>().IsOwner && !_hasCollectedCollectable)
        {
            _hasCollectedCollectable = true;
            PlaySoundEveryOneRPC();
            PickUpCollectableServerRPC(other.gameObject.GetComponent<NetworkObject>().OwnerClientId);
        }
    }


    void PlaySoundEveryOneRPC()
    {
        RoundManager.Instance.GetComponent<AudioSource>().PlayOneShot(CollectAudioClip);
    }

    [Rpc(SendTo.Server)]
    void PickUpCollectableServerRPC(ulong playerID)
    {
        print("Collected " + this.GetType().ToString());
        RoundManager.Instance.GiveAmmoToPlayerWithIDServerRPC(playerID, AmmoGiveAmount);

        // remove from spawn manager on round manager
        RoundManager.Instance.RemoveCollectable(gameObject);

        // then we can throw this out of here
        GetComponent<NetworkObject>().Despawn();
    }

}
