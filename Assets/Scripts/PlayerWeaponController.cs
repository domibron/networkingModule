using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerWeaponController : NetworkBehaviour
{
    public float BaseWeaponDamage = 35f;

    public float FireRate = 2f; // bullets per second

    public int MaxMagazineSize = 9;

    private Transform camTransform;


    void Awake()
    {
        camTransform = GetComponentInChildren<Camera>().transform;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            print("Fired");
            if (Physics.Raycast(camTransform.position, camTransform.forward, out RaycastHit hit, 999f))
            {
                if (hit.transform.GetComponent<NetworkObject>() == null) return;
                GamePersistent.Instance.DamagePlayerWithIDServerRPC(hit.transform.GetComponent<NetworkObject>().OwnerClientId, BaseWeaponDamage);
            }

        }
    }
}

