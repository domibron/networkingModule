using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ArenaPlayerUI : NetworkBehaviour
{

    public Image HealthBar;

    private Health _health;

    void Start()
    {
        _health = GetComponentInParent<Health>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        HealthBar.fillAmount = _health.GetHealthNormalized();
    }
}
