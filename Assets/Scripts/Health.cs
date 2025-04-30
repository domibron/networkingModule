using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    public float MaxHealth = 100f;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>();

    public ArenaPlayerUI ArenaPlayerUI; // BAD, coupling the code dammit.

    public AudioClip HurtClip;
    public AudioClip DeathClip;

    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CurrentHealth.OnValueChanged += OnCurrentHealthChanged;

        SetHealthServerRPC(MaxHealth);
    }

    void Update()
    {
        if (transform.position.y < 20f)
        {
            AddToHealth(-10);
        }
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
        print("updating health by" + amount);
        CurrentHealth.Value += amount;

        if (amount < 0) TakenDamageOwnerRPC();

        if (CurrentHealth.Value > MaxHealth)
        {
            CurrentHealth.Value = MaxHealth;
        }
        else if (CurrentHealth.Value <= 0)
        {
            // GamePersistent.Instance.EndGame(playerID);
            PlayDeathSFXEveryOneRPC();
            RoundManager.Instance.PlayerDiedServerRPC(OwnerClientId);
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [Rpc(SendTo.Owner)]
    private void TakenDamageOwnerRPC()
    {
        PlayHurtSoundEveryOneRPC();
        ArenaPlayerUI.TakeDamage();
    }

    [Rpc(SendTo.Everyone)]
    void PlayHurtSoundEveryOneRPC()
    {
        _audioSource.PlayOneShot(HurtClip, 0.8f);
    }

    [Rpc(SendTo.Everyone)]
    void PlayDeathSFXEveryOneRPC()
    {
        RoundManager.Instance.GetComponent<AudioSource>().PlayOneShot(DeathClip);
    }

    [Rpc(SendTo.Server)]
    private void SetHealthServerRPC(float value)
    {
        // Me no like this, local client to client to server, instead of local client to server to client.
        CurrentHealth.Value = value;
    }

    public float GetHealthNormalized()
    {
        return CurrentHealth.Value / MaxHealth;
    }
}
