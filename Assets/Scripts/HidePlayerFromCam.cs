using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HidePlayerFromCam : NetworkBehaviour
{

    public SkinnedMeshRenderer[] MeshRenderersToHide;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            foreach (var meshRenderer in MeshRenderersToHide)
            {
                meshRenderer.enabled = false;
            }
        }
    }
}
