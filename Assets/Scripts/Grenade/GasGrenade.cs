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

    private AudioSource _audioSource;

    public AudioClip SmokeClip;

    // Start is called before the first frame update
    void Start()
    {
        _activationDelay = DelayBeforeActivation;
        ParticleSystem.Stop();
        ShereCollider.enabled = false;

        _audioSource = GetComponentInParent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer && !IsHost)
        {

            if (!_isActive.Value) return;

            if (!ParticleSystem.isPlaying) ParticleSystem.Play();

            if (!ShereCollider.enabled) ShereCollider.enabled = true;

            if (!_audioSource.isPlaying)
            {
                _audioSource.clip = SmokeClip;
                _audioSource.loop = true;
                _audioSource.Play();
            }

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

            _audioSource.clip = SmokeClip;
            _audioSource.loop = true;
            _audioSource.Play();

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
