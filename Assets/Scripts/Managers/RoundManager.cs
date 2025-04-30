using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    public float RoundDuration = 300;

    public NetworkVariable<float> RoundTimer = new NetworkVariable<float>(300f);

    public TMP_Text RoundTimerText;

    public NetworkObject[] CollectableObjects;

    public int MaxSpawnedCollectables = 4;

    public float MaxTimeToWaitToSpawn = 30f;
    public float MinTimeToWaitToSpawn = 10f;

    private float _currentWaitTime = 0f;

    public Transform[] CollectableSpawnPoints;

    private List<GameObject> _spawnedCollectables = new List<GameObject>();

    private float roundTimer = 0f; // we use this instead of network because network traffic will be laggy.

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        RoundTimer.OnValueChanged += RoundTimerOnValueChanged;





    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer || IsHost)
            RoundTimer.Value = RoundDuration;
    }

    private void RoundTimerOnValueChanged(float previousValue, float newValue)
    {
        roundTimer = newValue;
        RoundTimerText.text = newValue.ToString("F1");
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer && !IsHost)
        {
            roundTimer -= Time.deltaTime;
            RoundTimerText.text = roundTimer.ToString("F1");
            return;
        }

        // server side code :3
        if (RoundTimer.Value > 0) RoundTimer.Value -= Time.deltaTime;

        if (RoundTimer.Value <= 0 && RoundTimer.Value > -999)
        {
            RoundTimer.Value = -9999;
            TimerEndServerRPC();
        }

        if (_spawnedCollectables.Count < MaxSpawnedCollectables && _currentWaitTime <= 0f)
        {
            print("Spawning collectable");
            _currentWaitTime = UnityEngine.Random.Range(MinTimeToWaitToSpawn, MaxTimeToWaitToSpawn);

            Vector3? spawnLocation = GetRandomSpawnWithNoCollectable();

            if (!spawnLocation.HasValue) return;

            NetworkObject collectableNetObject = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(
                CollectableObjects[UnityEngine.Random.Range(1, CollectableObjects.Length * CollectableObjects.Length) % CollectableObjects.Length],
                destroyWithScene: true,
                position: spawnLocation.Value);

            _spawnedCollectables.Add(collectableNetObject.gameObject);

        }
        else if (_currentWaitTime > 0f)
        {
            _currentWaitTime -= Time.deltaTime;
        }

    }

    [Rpc(SendTo.Server)]
    private void TimerEndServerRPC()
    {
        IReadOnlyDictionary<ulong, NetworkClient> playerClient = NetworkManager.Singleton.ConnectedClients;

        List<Health> healths = new List<Health>();

        foreach (NetworkClient client in playerClient.Values)
        {
            foreach (var ownedObj in client.OwnedObjects)
            {
                if (ownedObj.GetComponent<Health>() != null)
                {
                    healths.Add(ownedObj.GetComponent<Health>());
                }
            }
        }

        Health bestHealth = null;

        foreach (Health health in healths)
        {
            if (bestHealth == null)
            {
                bestHealth = health;
                continue;
            }

            if (health.CurrentHealth.Value > bestHealth.CurrentHealth.Value)
            {
                bestHealth = health;
                continue;
            }

            if (bestHealth.CurrentHealth.Value == health.CurrentHealth.Value) bestHealth = null; // we use this for now because this is a 2p game.

        }

        if (bestHealth != null)
            GamePersistent.Instance.EndRoundServerRPC(true, bestHealth.OwnerClientId);
        else
            GamePersistent.Instance.EndRoundServerRPC();

    }

    [Rpc(SendTo.Server)]
    public void PlayerDiedServerRPC(ulong deadPlayerID)
    {
        if (NetworkManager.Singleton.ConnectedClientsIds[0] == deadPlayerID)
        {
            GamePersistent.Instance.EndRoundServerRPC(true, NetworkManager.Singleton.ConnectedClientsIds[1]);
        }
        else
        {
            GamePersistent.Instance.EndRoundServerRPC(true, NetworkManager.Singleton.ConnectedClientsIds[0]);
        }
    }

    private Vector3? GetRandomSpawnWithNoCollectable()
    {
        List<Vector3> potentialSpawnPoints = new List<Vector3>();

        // cycle through all spawn points.
        foreach (var spawnPoint in CollectableSpawnPoints)
        {
            bool isViableSpawnPoint = true;

            // check to see if there is a spawn point.
            foreach (var collectable in _spawnedCollectables)
            {
                // if there is, we can just skip this one.
                if (Vector3.Distance(collectable.transform.position, spawnPoint.position) < .2f)
                {
                    isViableSpawnPoint = false;
                    break;
                }
            }

            if (isViableSpawnPoint)
            {
                potentialSpawnPoints.Add(spawnPoint.position);
            }
        }

        if (potentialSpawnPoints.Count <= 0) return null;

        return potentialSpawnPoints[UnityEngine.Random.Range(0, potentialSpawnPoints.Count - 1)];
    }

    public void RemoveCollectable(GameObject collectable)
    {
        _spawnedCollectables.Remove(collectable);
    }

    [Rpc(SendTo.Server)]
    public void DamagePlayerWithIDServerRPC(ulong playerID, float damage)
    {
        print("Seeking target player");
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerID)) return;

        NetworkClient playerClient = NetworkManager.Singleton.ConnectedClients[playerID];

        foreach (var ownedObj in playerClient.OwnedObjects)
        {
            ownedObj.GetComponent<Health>()?.AddToHealth(-damage);
        }
    }

    // yeah, its the same.
    [Rpc(SendTo.Server)]
    public void HealPlayerWithIDServerRPC(ulong playerID, float healAmount)
    {
        print("Seeking target player");
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerID)) return;

        NetworkClient playerClient = NetworkManager.Singleton.ConnectedClients[playerID];

        foreach (var ownedObj in playerClient.OwnedObjects)
        {
            ownedObj.GetComponent<Health>()?.AddToHealth(healAmount);
        }
    }

    [Rpc(SendTo.Server)]
    public void GiveAmmoToPlayerWithIDServerRPC(ulong playerID, int ammoAmount)
    {
        print("Seeking target player");
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerID)) return;

        NetworkClient playerClient = NetworkManager.Singleton.ConnectedClients[playerID];

        foreach (var ownedObj in playerClient.OwnedObjects)
        {
            ownedObj.GetComponent<PlayerWeaponController>()?.AddToCurrentPoolServerRPC(ammoAmount);
        }
    }

    [Rpc(SendTo.Server)]
    public void GrantDoubleDamageToPlayerWithIDServerRPC(ulong playerID, float duration)
    {
        print("Seeking target player");
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerID)) return;

        NetworkClient playerClient = NetworkManager.Singleton.ConnectedClients[playerID];

        foreach (var ownedObj in playerClient.OwnedObjects)
        {
            ownedObj.GetComponent<PlayerWeaponController>()?.SetDoubleDamageTimerServerRPC(duration);
        }
    }

    [Rpc(SendTo.Server)]
    public void GiveCSGasEffectToPlayerWithIDServerRPC(ulong playerID)
    {

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerID)) return;

        NetworkClient playerClient = NetworkManager.Singleton.ConnectedClients[playerID];

        foreach (var ownedObj in playerClient.OwnedObjects)
        {
            ownedObj.GetComponent<PlayerController>()?.GiveCSGasEffectServerRPC();
        }
    }

    [Rpc(SendTo.Server)]
    public void GiveCSGrenadeToPlayerWithIDServerRPC(ulong playerID, int quantity)
    {
        print("Seeking target player");
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerID)) return;

        NetworkClient playerClient = NetworkManager.Singleton.ConnectedClients[playerID];

        foreach (var ownedObj in playerClient.OwnedObjects)
        {
            ownedObj.GetComponent<GrenadeWeaponManager>()?.AddToGrenadeCountServerRPC(quantity);
        }
    }
}
