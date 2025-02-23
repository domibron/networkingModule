using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkTest : MonoBehaviour
{
	public NetworkManager networkManager;

	void Start()
	{
		networkManager.OnClientConnectedCallback += OnClientConnect;

#if UNITY_EDITOR
		networkManager.StartHost();
#else
		networkManager.StartClient();
#endif
	}

	private void OnClientConnect(ulong obj)
	{
		Debug.Log("Fucking stupid bull shit garbage unity");
	}
}
