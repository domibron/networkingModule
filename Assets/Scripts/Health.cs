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

    #region Start
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }
    #endregion

    #region OnNetworkSpawn
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ResetHealthServerRPC();
    }
    #endregion

    #region Update
    void Update()
    {
        if (transform.position.y < -20f)
        {
            AddToHealth(-10);
        }
    }
    #endregion

    #region AddToHealth
    public void AddToHealth(float amount)
    {
        AddToHealthServerRPC(amount);
    }
    #endregion

    #region ResetHealthServerRPC
    [Rpc(SendTo.Server)]
    private void ResetHealthServerRPC()
    {
        CurrentHealth.Value = MaxHealth;
    }
    #endregion

    #region AddToHealthServerRPC
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
    #endregion

    #region TakenDamageOwnerRPC
    [Rpc(SendTo.Owner)]
    private void TakenDamageOwnerRPC()
    {
        PlayHurtSoundEveryOneRPC();
        ArenaPlayerUI.TakeDamage();
    }
    #endregion

    #region PlayHurtSoundEveryOneRPC
    [Rpc(SendTo.Everyone)]
    void PlayHurtSoundEveryOneRPC()
    {
        _audioSource.PlayOneShot(HurtClip, 0.8f);
    }
    #endregion

    #region PlayDeathSFXEveryOneRPC
    [Rpc(SendTo.Everyone)]
    void PlayDeathSFXEveryOneRPC()
    {
        // I think this is playing the audio on EVERYONE, instead of a specific player or location. :/
        RoundManager.Instance.GetComponent<AudioSource>().PlayOneShot(DeathClip);
    }
    #endregion

    #region GetHealthNormalized
    public float GetHealthNormalized()
    {
        return CurrentHealth.Value / MaxHealth;
    }
    #endregion
}
