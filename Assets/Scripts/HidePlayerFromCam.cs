using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HidePlayerFromCam : NetworkBehaviour
{

    public SkinnedMeshRenderer[] MeshRenderersToHide;

    public Collider[] CollidersToDisable;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            foreach (var meshRenderer in MeshRenderersToHide)
            {
                meshRenderer.enabled = false;
            }

            foreach (var collider in CollidersToDisable)
            {
                collider.enabled = false;
            }
        }
    }
}
