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
    public CustomNetworkManager NetManager;

    public UIItem[] MainMenuUIs;

    // joining
    public TMP_InputField InputField;

    // creating
    public TMP_InputField IPInput;
    public TMP_InputField PortInput;

    private string _lastMenu;

    void Awake()
    {
        ShowMenu("mainmenu");
    }

    void Start()
    {
        NetManager.OnConnectionEvent += OnConnectionEvent;
        NetManager.OnServerStopped += OnServerStopped;
        NetManager.OnTransportFailure += OnTransportFailure;

    }



    void OnDisable()
    {
        // we make sure that the net manager, a object that is persistent does not have a a null reference.
        NetManager.OnConnectionEvent -= OnConnectionEvent;
        NetManager.OnServerStopped -= OnServerStopped;
        NetManager.OnTransportFailure -= OnTransportFailure;
    }

    void Update()
    {

    }

    private void OnTransportFailure()
    {
        print("Transport failure");

        ShowMenu(_lastMenu);
    }

    private void OnServerStopped(bool obj)
    {
        print(obj);

        ShowMenu(_lastMenu);

    }

    private void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        if (data.EventType == ConnectionEvent.ClientDisconnected)
        {
            if (data.ClientId == manager.LocalClientId)
                ShowMenu(_lastMenu);
        }

        if (data.EventType == ConnectionEvent.ClientConnected)
        {
            if (data.ClientId == manager.LocalClientId)
            {
                NetManager.SceneManager.LoadScene("PreGameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            // ShowMenu("inserver");
        }

        print(data.EventType.ToString());

    }

    public void LeaveServer()
    {
        NetworkManager.Singleton.Shutdown();
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


            NetManager.ConnectToIPAndPort(ipAndPort[0], port);
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


            NetManager.HostIPAndPort(ip, port);

            print(NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address);
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
}
