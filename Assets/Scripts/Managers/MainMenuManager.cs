using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

#region UIItem
[Serializable]
public class UIItem
{
    public string UIName;
    public GameObject UIObject;
}
#endregion

/// <summary>
/// Handles the main menu UI and passing data to connect to servers.
/// </summary>
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

    private float _loadingTimer = 0f;

    #region Awake
    void Awake()
    {
        // ! THIS LACKS PROPER IMPLEMENTATION. headless server, need config files and setup implementation.
        if (CustomNetworkManager.IsHeadlessMode())
        {
            CustomNetworkManager.Instance.StartServerIPAndPort("127.0.0.1", (ushort)7777);
        }

        ShowMenu("mainmenu");
    }
    #endregion

    #region Start
    void Start()
    {
        CustomNetworkManager.Instance.OnConnectionEvent += OnConnectionEvent;
        CustomNetworkManager.Instance.OnServerStopped += OnServerStopped;
        CustomNetworkManager.Instance.OnTransportFailure += OnTransportFailure;
    }
    #endregion

    #region OnDisable
    void OnDisable()
    {
        // we make sure that the net manager, a object that is persistent does not have a a null reference.
        CustomNetworkManager.Instance.OnConnectionEvent -= OnConnectionEvent;
        CustomNetworkManager.Instance.OnServerStopped -= OnServerStopped;
        CustomNetworkManager.Instance.OnTransportFailure -= OnTransportFailure;
    }
    #endregion

    #region Update
    void Update()
    {
        if (IsInMenu("connecting"))
        {
            _loadingTimer += Time.deltaTime;
            ConnectingTextDisplay.text = $"Connecting to {CustomNetworkManager.Instance.GetComponent<UnityTransport>().ConnectionData.Address}"
            + $":{CustomNetworkManager.Instance.GetComponent<UnityTransport>().ConnectionData.Port}\nDuration: {_loadingTimer.ToString("F1")}";
        }
        else
        {
            _loadingTimer = 0f;
        }

        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;

        if (!Cursor.visible) Cursor.visible = true;


    }
    #endregion

    #region OnTransportFailure
    private void OnTransportFailure()
    {
        print("Transport failure");
        NetworkManager.Singleton.Shutdown();
        ShowMenu(_lastMenu);
    }
    #endregion

    #region OnServerStopped
    private void OnServerStopped(bool obj)
    {
        print(obj);
        NetworkManager.Singleton.Shutdown();
        ShowMenu(_lastMenu);

    }
    #endregion

    #region OnConnectionEvent
    private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        // we can ignore this for now.
        if (data.ClientId != manager.LocalClientId) return;

        if (!manager.IsConnectedClient && data.EventType == ConnectionEvent.ClientDisconnected)
        {
            Disconnected("Server timed out");

        }
        else if (manager.IsConnectedClient && data.EventType == ConnectionEvent.ClientDisconnected)
        {
            Disconnected(manager.DisconnectReason);
        }
    }
    #endregion

    #region LeaveServer
    public void LeaveServer()
    {
        _loadingTimer = 0f;
        NetworkManager.Singleton.Shutdown();
        ShowMenu(_lastMenu);
    }
    #endregion

    #region ConnectedToServer
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
    #endregion

    #region HostServer
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
    #endregion

    #region ShowMenu
    public void ShowMenu(string uiName)
    {
        foreach (var item in MainMenuUIs)
        {
            if (item.UIName == uiName) item.UIObject.SetActive(true);
            else item.UIObject.SetActive(false);
        }
    }
    #endregion

    #region IsInMenu
    public bool IsInMenu(string uiName)
    {
        foreach (var item in MainMenuUIs)
        {
            if (item.UIName == uiName) return item.UIObject.activeSelf;
        }

        return false;
    }
    #endregion

    #region GetActiveMenu
    public string GetActiveMenu()
    {
        foreach (var item in MainMenuUIs)
        {
            if (item.UIObject.activeSelf) return item.UIName;
        }

        return "mainmenu";
    }
    #endregion

    #region Disconnected
    public void Disconnected(string reason)
    {
        ShowMenu("disconnected");

        if (string.IsNullOrEmpty(reason)) reason = "No reason was specified";

        DisconnectedTextDisplay.text = reason;
    }
    #endregion

    #region QuitGame
    public void QuitGame()
    {
        NetworkManager.Singleton.Shutdown();
        Application.Quit();
    }
    #endregion
}
