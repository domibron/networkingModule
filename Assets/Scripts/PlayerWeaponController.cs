using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerWeaponController : NetworkBehaviour
{
    // Terrible weapon implementation but it will work for now since its one weapon.

    public float BaseWeaponDamage = 35f;

    public float BulletsPerSecond = 2f; // bullets per second

    public float ReloadSpeed = 2f;

    public ArenaPlayerUI ArenaPlayerUI;

    private int _maxMagazineSize = 30; // yeah, this is not going to be nice. Server need to verify ammo some how.
    private int _startingAmmoPool = 90;

    private NetworkVariable<int> _currentAmmoInMag = new NetworkVariable<int>(0);
    private NetworkVariable<int> _currentAmmoPool = new NetworkVariable<int>(0);

    private float _weaponFireRateCoolDown = 0f; // server should really handle the cool down.

    private Transform _camTransform;

    private Coroutine _reloadCoroutine;

    private bool _isReloading = false;

    public NetworkVariable<float> DoubleDamageTimer = new NetworkVariable<float>(0);

    public LayerMask layerMask;

    void Awake()
    {
        _camTransform = GetComponentInChildren<Camera>().transform;
    }

    void Start()
    {

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SetUpAmmoServerRPC();
    }

    [Rpc(SendTo.Server)]
    public void SetDoubleDamageTimerServerRPC(float duration)
    {
        DoubleDamageTimer.Value = duration;
    }

    [Rpc(SendTo.Server)]
    public void SetUpAmmoServerRPC()
    {
        _currentAmmoInMag.Value = _maxMagazineSize;
        _currentAmmoPool.Value = _startingAmmoPool;
    }

    [Rpc(SendTo.Server)]
    public void AddToCurrentPoolServerRPC(int amount)
    {
        _currentAmmoInMag.Value += amount;
    }

    [Rpc(SendTo.Server)]
    public void AddToCurrentAmmoInMagServerRPC(int amount)
    {
        _currentAmmoInMag.Value += amount;
    }

    public void ReloadMagWithPool()
    {
        if (_reloadCoroutine != null || _isReloading || _currentAmmoPool.Value <= 0) return;

        _reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    void Update()
    {
        if (IsHost || IsServer) SortPlayerStuffOnServer();
        if (!IsOwner) return;


        if (_weaponFireRateCoolDown > 0f) _weaponFireRateCoolDown -= Time.deltaTime;

        if (!_isReloading)
        {
            ArenaPlayerUI.AmmoText.text = $"[{_currentAmmoInMag.Value}]\n{_currentAmmoPool.Value}";
        }
        else
        {
            ArenaPlayerUI.AmmoText.text = $"[Reloading]\n{_currentAmmoPool.Value}";
        }

        if (Input.GetKey(KeyCode.Mouse0) && _weaponFireRateCoolDown <= 0f && _currentAmmoInMag.Value > 0 && !_isReloading)
        {
            AddToCurrentAmmoInMagServerRPC(-1);
            _weaponFireRateCoolDown = 1f / BulletsPerSecond;

            print("Fired");
            if (Physics.Raycast(_camTransform.position, _camTransform.forward, out RaycastHit hit, 999f, layerMask))
            {
                print(hit.transform.name);
                Debug.DrawLine(_camTransform.position, hit.point, Color.red, 10f);

                NetworkObject playerNetObject = hit.transform.root.GetComponent<NetworkObject>();


                if (playerNetObject == null) return;
                if (playerNetObject.OwnerClientId == OwnerClientId) return; // TODO Fix player hitting themselves.

                float damage = BaseWeaponDamage * (DoubleDamageTimer.Value > 0 ? 2f : 1f);

                RoundManager.Instance.DamagePlayerWithIDServerRPC(playerNetObject.OwnerClientId,
                    (hit.collider.gameObject.CompareTag("Head") ? 999f : damage));
            }
            else
            {
                Debug.DrawLine(_camTransform.position, _camTransform.position + _camTransform.forward * 999f, Color.red, 10f);
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadMagWithPool();
        }
    }


    private void SortPlayerStuffOnServer()
    {
        if (DoubleDamageTimer.Value > 0)
        {
            DoubleDamageTimer.Value -= Time.deltaTime;
            // give icon.
        }
        else
        {
            // remove Icon.
        }
    }

    [Rpc(SendTo.Server)]
    private void ReloadMagServerRPC()
    {
        int ammoWeNeed = _maxMagazineSize - _currentAmmoInMag.Value;

        if (ammoWeNeed <= _currentAmmoPool.Value)
        {
            _currentAmmoPool.Value -= ammoWeNeed;
            _currentAmmoInMag.Value += ammoWeNeed;
        }
        else if (_currentAmmoPool.Value > 0)
        {
            ammoWeNeed = _currentAmmoPool.Value;

            _currentAmmoPool.Value -= ammoWeNeed;
            _currentAmmoInMag.Value += ammoWeNeed;
        }
    }

    private IEnumerator ReloadCoroutine()
    {
        _isReloading = true;

        yield return new WaitForSeconds(ReloadSpeed);

        ReloadMagServerRPC();

        _isReloading = false;
        _reloadCoroutine = null;
    }
}

