using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    public float MaxHealth = 100f;

    private NetworkVariable<float> _currentHealth = new NetworkVariable<float>();

    void Start()
    {

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _currentHealth.OnValueChanged += OnCurrentHealthChanged;

        SetHealthServerRPC(MaxHealth);
    }

    private void OnCurrentHealthChanged(float previousValue, float newValue)
    {

    }

    public void AddToHealth(float amount)
    {
        AddToHealthServerRPC(amount);
    }

    [Rpc(SendTo.Server)]
    private void AddToHealthServerRPC(float amount)
    {
        _currentHealth.Value += amount;

        if (_currentHealth.Value > MaxHealth)
        {
            _currentHealth.Value = MaxHealth;
        }
        else if (_currentHealth.Value <= 0)
        {
            // GamePersistent.Instance.EndGame(playerID);
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [Rpc(SendTo.Server)]
    private void SetHealthServerRPC(float value)
    {
        // Me no like this, local client to client to server, instead of local client to server to client.
        _currentHealth.Value = value;
    }
}
