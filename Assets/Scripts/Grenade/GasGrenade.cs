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

    private bool _isActive = false;

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
        if (_activationDelay > 0f)
        {
            _activationDelay -= Time.deltaTime;
        }
        else if (!_isActive)
        {
            _isActive = true;

            ParticleSystem.Play();

            ShereCollider.enabled = true;

            StartCoroutine(DestroyAfterTime(LifeTimeAfterActivation));
        }

        if (GetComponentInParent<Rigidbody>().velocity.magnitude < .5f)
            GetComponentInParent<Rigidbody>().velocity = Vector3.zero;
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
