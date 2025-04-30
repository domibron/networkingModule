using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class GrenadeWeaponManager : NetworkBehaviour
{
    public NetworkObject GrenadeObject;

    public NetworkVariable<int> GrenadeCount = new NetworkVariable<int>(2);

    public int StartingAmount = 2;


    public float MaxThrowForce = 10f;
    public float MinThrowForce = 3f;

    public float TimeToReachMax = 3f;

    private Transform _camTransform;

    private bool _isHolding = false;

    private float _throwForcePercentage;

    public AudioClip GrenadeOut;

    private AudioSource _audioSource;

    private Animator _animator;

    #region Awake
    void Awake()
    {
        _camTransform = GetComponentInChildren<Camera>().transform;
        _audioSource = GetComponent<AudioSource>();

        _animator = GetComponentInChildren<Animator>();
    }
    #endregion

    #region Update
    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        if (_isHolding)
        {
            // 1 / 10 = one tenth of a second. or 10 times as long.
            _throwForcePercentage += Time.deltaTime / TimeToReachMax;

        }
        else
        {
            _throwForcePercentage = 0f;
        }

        _throwForcePercentage = Mathf.Clamp(_throwForcePercentage, 0, 1);

        if (Input.GetKeyDown(KeyCode.G))
        {
            _animator.SetFloat("WeaponStateX", 1);
            _isHolding = true;
        }
        else if (Input.GetKeyUp(KeyCode.G))
        {
            if (GrenadeCount.Value > 0)
            {
                // throw
                PlayThrowSoundEveryOneRPC();

                SpawnGrenadeServerRPC(_camTransform.position, _camTransform.forward * Mathf.Lerp(MinThrowForce, MaxThrowForce, _throwForcePercentage));

                AddToGrenadeCountServerRPC(-1);
            }
            _animator.SetFloat("WeaponStateX", 0);

            _isHolding = false;
        }
    }
    #endregion

    #region OnNetworkSpawn
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SetGrenadeCountToStartingServerRPC();
    }
    #endregion

    #region SetGrenadeCountToStartingServerRPC
    [Rpc(SendTo.Server)]
    public void SetGrenadeCountToStartingServerRPC()
    {
        GrenadeCount.Value = StartingAmount;
    }
    #endregion

    #region AddToGrenadeCountServerRPC
    [Rpc(SendTo.Server)]
    public void AddToGrenadeCountServerRPC(int amount)
    {
        GrenadeCount.Value += amount;
    }
    #endregion

    #region SpawnGrenadeServerRPC
    [Rpc(SendTo.Server)]
    public void SpawnGrenadeServerRPC(Vector3 spawnPoint, Vector3 forceToApply)
    {
        NetworkObject grenade = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(GrenadeObject, position: spawnPoint);

        grenade.GetComponent<Rigidbody>().AddForce(forceToApply, ForceMode.Impulse);
    }
    #endregion

    #region PlayThrowSoundEveryOneRPC
    [Rpc(SendTo.Everyone)]
    void PlayThrowSoundEveryOneRPC()
    {
        _audioSource.PlayOneShot(GrenadeOut);
    }
    #endregion


}
