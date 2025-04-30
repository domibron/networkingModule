using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class GrenadeWeaponManager : NetworkBehaviour
{
    public NetworkObject GrenadeObject;

    private NetworkVariable<int> grenadeCount = new NetworkVariable<int>(2);

    public int StartingAmount = 2;


    public float MaxThrowForce = 10f;
    public float MinThrowForce = 3f;

    public float TimeToReachMax = 3f;

    private Transform _camTransform;

    private bool _isHolding = false;

    private float _throwForcePercentage;

    void Awake()
    {
        _camTransform = GetComponentInChildren<Camera>().transform;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SetGrenadeCountToStartingServerRPC();
    }

    [Rpc(SendTo.Server)]
    public void SetGrenadeCountToStartingServerRPC()
    {
        grenadeCount.Value = StartingAmount;
    }

    [Rpc(SendTo.Server)]
    public void AddToGrenadeCountServerRPC(int amount)
    {
        grenadeCount.Value += amount;
    }

    [Rpc(SendTo.Server)]
    public void SpawnGrenadeServerRPC(Vector3 spawnPoint, Vector3 forceToApply)
    {
        NetworkObject grenade = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(GrenadeObject, position: spawnPoint);

        grenade.GetComponent<Rigidbody>().AddForce(forceToApply, ForceMode.Impulse);
    }

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
            _isHolding = true;
        }
        else if (Input.GetKeyUp(KeyCode.G))
        {
            if (grenadeCount.Value > 0)
            {
                // throw
                SpawnGrenadeServerRPC(_camTransform.position, _camTransform.forward * Mathf.Lerp(MinThrowForce, MaxThrowForce, _throwForcePercentage));

                AddToGrenadeCountServerRPC(-1);
            }

            _isHolding = false;
        }
    }
}
