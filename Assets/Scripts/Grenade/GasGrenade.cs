using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GasGrenade : NetworkBehaviour
{
    public float DelayBeforeActivation = 3f;

    public float LifeTimeAfterActivation = 10f;

    public ParticleSystem ParticleSystem;

    public SphereCollider ShereCollider;

    private float _activationDelay = 1f;

    private NetworkVariable<bool> _isActive = new NetworkVariable<bool>(false);

    // Start is called before the first frame update
    void Start()
    {
        _activationDelay = DelayBeforeActivation;
        ParticleSystem.Stop();
        ShereCollider.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer && !IsHost)
        {

            if (!_isActive.Value) return;

            if (!ParticleSystem.isPlaying) ParticleSystem.Play();

            if (!ShereCollider.enabled) ShereCollider.enabled = true;


            return;

        }

        if (_activationDelay > 0f)
        {
            _activationDelay -= Time.deltaTime;
        }
        else if (!_isActive.Value)
        {
            _isActive.Value = true;

            ParticleSystem.Play();

            ShereCollider.enabled = true;

            StartCoroutine(DestroyAfterTime(LifeTimeAfterActivation));
        }
    }

    private IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        GetComponentInParent<NetworkObject>().Despawn();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && other.gameObject.GetComponent<NetworkObject>().IsOwner)
        {
            // print("STAYING");
            PlayerController pController = other.gameObject.GetComponent<PlayerController>();

            if (pController.CSGasEffectTimer.Value <= pController.CSGasEffectDuration - 1)
                RoundManager.Instance.GiveCSGasEffectToPlayerWithIDServerRPC(other.gameObject.GetComponent<NetworkObject>().OwnerClientId);
        }
    }
}
