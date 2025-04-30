using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HealthCollectable : NetworkBehaviour
{
    private bool _hasCollectedCollectable = false;

    public float HealthToGive = 25f;

    public AudioClip CollectAudioClip;


    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && other.gameObject.GetComponent<NetworkObject>().IsOwner && !_hasCollectedCollectable && (IsHost || !IsServer))
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
        RoundManager.Instance.HealPlayerWithIDServerRPC(playerID, HealthToGive);

        RoundManager.Instance.RemoveCollectable(gameObject);


        GetComponent<NetworkObject>().Despawn();
    }
}
