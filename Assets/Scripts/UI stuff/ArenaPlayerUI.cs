using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ArenaPlayerUI : NetworkBehaviour
{

    public Image HealthBar;
    public Image ToxicVignette;

    public TMP_Text AmmoText;

    private Health _health;

    private PlayerController _playerController;

    void Start()
    {
        _health = GetComponentInParent<Health>();

        _playerController = GetComponentInParent<PlayerController>();
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

        Color color = ToxicVignette.color;

        color.a = Mathf.Lerp(_playerController.CSGasEffectTimer.Value / _playerController.CSGasEffectDuration, 1, 0f);

        ToxicVignette.color = color;
    }
}
