using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
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
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (Physics.Raycast(camTransform.position, camTransform.forward, out RaycastHit hit, 999f))
            {
                hit.collider.GetComponent<Health>()?.AddToHealth(-BaseWeaponDamage);
            }

        }
    }
}

