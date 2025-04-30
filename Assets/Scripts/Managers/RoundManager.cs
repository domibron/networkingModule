using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles the current round that is being played out on the server.
/// </summary>
public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    #region Round Timer Variables

    public NetworkVariable<float> RoundTimer = new NetworkVariable<float>(300f);

    public float RoundDuration = 300;

    public TMP_Text RoundTimerText;

    private float _roundTimer = 0f; // we use this instead of network because network traffic will be laggy.

    #endregion

    #region Collectables Variables
    public NetworkObject[] CollectableObjects;

    public int MaxSpawnedCollectables = 4;

    public float MaxTimeToWaitToSpawn = 30f;
    public float MinTimeToWaitToSpawn = 10f;

    public Transform[] CollectableSpawnPoints;

    private List<GameObject> _spawnedCollectables = new List<GameObject>();

    private float _currentWaitTime = 0f;

    #endregion

    #region Awake
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
    #endregion

    #region Start
    // Start is called before the first frame update
    void Start()
    {
        RoundTimer.OnValueChanged += RoundTimerOnValueChanged;

    }
    #endregion

    #region Update
    // Update is called once per frame
    void Update()
    {
        if (!IsServer && !IsHost)
        {
            _roundTimer -= Time.deltaTime;
            RoundTimerText.text = _roundTimer.ToString("F1");
            return;
        }

        // server side code :3
        if (RoundTimer.Value > 0) RoundTimer.Value -= Time.deltaTime;

        if (RoundTimer.Value <= 0 && RoundTimer.Value > -999)
        {
            RoundTimer.Value = -9999;
            TimerEndServerRPC();
        }

        // handles spawning collectables over time.
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
    #endregion

    #region OnNetworkSpawn
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer || IsHost)
            RoundTimer.Value = RoundDuration;
    }
    #endregion

    #region RoundTimerOnValueChanged
    private void RoundTimerOnValueChanged(float previousValue, float newValue)
    {
        _roundTimer = newValue;
        RoundTimerText.text = newValue.ToString("F1");
    }
    #endregion


    #region TimerEndServerRPC
    [Rpc(SendTo.Server)]
    private void TimerEndServerRPC()
    {
        // we get all the players in the server. (Did it this way in case we want more than 2 players, for future expansion)
        IReadOnlyDictionary<ulong, NetworkClient> playerClient = NetworkManager.Singleton.ConnectedClients;

        // We will get all the health components (1 per player on their arean player body)
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

        // we will try and get the best health value.
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

        // End the round based on that data!
        if (bestHealth != null)
            GamePersistent.Instance.EndRoundServerRPC(true, bestHealth.OwnerClientId);
        else
            GamePersistent.Instance.EndRoundServerRPC();

    }
    #endregion

    #region PlayerDiedServerRPC
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
    #endregion

    #region GetRandomSpawnWithNoCollectable
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
    #endregion

    #region RemoveCollectable
    public void RemoveCollectable(GameObject collectable)
    {
        _spawnedCollectables.Remove(collectable);
    }
    #endregion

    #region DamagePlayerWithIDServerRPC
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
    #endregion

    #region HealPlayerWithIDServerRPC
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
    #endregion

    #region GiveAmmoToPlayerWithIDServerRPC
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
    #endregion

    #region GrantDoubleDamageToPlayerWithIDServerRPC
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
    #endregion

    #region GiveCSGasEffectToPlayerWithIDServerRPC
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
    #endregion

    #region GiveCSGrenadeToPlayerWithIDServerRPC
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
    #endregion
}
