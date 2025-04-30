using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DamageCollectable : NetworkBehaviour
{
    private bool _hasCollectedCollectable = false;
    public float Duration = 10f;

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
        RoundManager.Instance.GrantDoubleDamageToPlayerWithIDServerRPC(playerID, Duration);

        RoundManager.Instance.RemoveCollectable(gameObject);


        GetComponent<NetworkObject>().Despawn();
    }
}
