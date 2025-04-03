using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;


[Serializable]
public class UIItem
{
    public string UIName;
    public GameObject UIObject;
}

public class MainMenuManager : MonoBehaviour
{

    public UIItem[] MainMenuUIs;

    // joining
    public TMP_InputField InputField;

    // creating
    public TMP_InputField IPInput;
    public TMP_InputField PortInput;

    // disconnected

    public TMP_Text DisconnectedTextDisplay;

    // connecting


    public TMP_Text ConnectingTextDisplay;


    public string GameSceneName = "GameScene";

    private string _lastMenu;


    private float loadingTimer = 0f;


    void Awake()
    {
        // ! THIS LACKS PROPER IMPLEMENTATION.
        if (CustomNetworkManager.IsHeadlessMode())
        {
            CustomNetworkManager.Instance.StartServerIPAndPort("86.10.14.161", (ushort)7777);
        }

        ShowMenu("mainmenu");
    }

    void Start()
    {
        CustomNetworkManager.Instance.OnConnectionEvent += OnConnectionEvent;
        CustomNetworkManager.Instance.OnServerStopped += OnServerStopped;
        CustomNetworkManager.Instance.OnTransportFailure += OnTransportFailure;

    }



    void OnDisable()
    {
        // we make sure that the net manager, a object that is persistent does not have a a null reference.
        CustomNetworkManager.Instance.OnConnectionEvent -= OnConnectionEvent;
        CustomNetworkManager.Instance.OnServerStopped -= OnServerStopped;
        CustomNetworkManager.Instance.OnTransportFailure -= OnTransportFailure;
    }

    void Update()
    {
        if (IsInMenu("connecting"))
        {
            loadingTimer += Time.deltaTime;
            ConnectingTextDisplay.text = $"Connecting to {CustomNetworkManager.Instance.GetComponent<UnityTransport>().ConnectionData.Address}"
            + $":{CustomNetworkManager.Instance.GetComponent<UnityTransport>().ConnectionData.Port}\nDuration: {loadingTimer.ToString("F1")}";
        }
        else
        {
            loadingTimer = 0f;
        }

    }

    private void OnTransportFailure()
    {
        print("Transport failure");
        NetworkManager.Singleton.Shutdown();
        ShowMenu(_lastMenu);
    }

    private void OnServerStopped(bool obj)
    {
        print(obj);
        NetworkManager.Singleton.Shutdown();
        ShowMenu(_lastMenu);

    }

    private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        // we can ignore this for now.
        if (data.ClientId != manager.LocalClientId) return;

        switch (data.EventType)
        {
            case ConnectionEvent.ClientDisconnected:
                Disconnected(manager.DisconnectReason);
                break;
        }
    }

    public void LeaveServer()
    {
        loadingTimer = 0f;
        NetworkManager.Singleton.Shutdown();
        ShowMenu(_lastMenu);
    }


    public void ConnectedToServer()
    {
        _lastMenu = GetActiveMenu();
        ShowMenu("connecting");

        try
        {
            string input = InputField.text;

            string[] ipAndPort = input.Split(':');

            ushort port = ushort.Parse(ipAndPort[1]);


            CustomNetworkManager.Instance.ConnectToIPAndPort(ipAndPort[0], port);
        }
        catch (Exception e)
        {
            ShowMenu(_lastMenu);
            print(e.Message);
        }
    }

    public void HostServer()
    {
        _lastMenu = GetActiveMenu();
        ShowMenu("connecting");

        try
        {
            string ip = IPInput.text;


            ushort port = ushort.Parse(PortInput.text);


            CustomNetworkManager.Instance.HostIPAndPort(ip, port);

            print(NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address);

            CustomNetworkManager.Instance.SceneManager.LoadScene(GameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            ShowMenu(_lastMenu);
            print(e.Message);
        }
    }

    public void ShowMenu(string uiName)
    {
        foreach (var item in MainMenuUIs)
        {
            if (item.UIName == uiName) item.UIObject.SetActive(true);
            else item.UIObject.SetActive(false);
        }
    }

    public bool IsInMenu(string uiName)
    {
        foreach (var item in MainMenuUIs)
        {
            if (item.UIName == uiName) return item.UIObject.activeSelf;
        }

        return false;
    }

    public string GetActiveMenu()
    {
        foreach (var item in MainMenuUIs)
        {
            if (item.UIObject.activeSelf) return item.UIName;
        }

        return "mainmenu";
    }

    public void Disconnected(string reason)
    {
        ShowMenu("disconnected");

        if (string.IsNullOrEmpty(reason)) reason = "No reason was specified";

        DisconnectedTextDisplay.text = reason;
    }

    public void QuitGame()
    {
        NetworkManager.Singleton.Shutdown();
        Application.Quit();
    }
}
