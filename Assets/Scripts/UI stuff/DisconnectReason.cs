using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisconnectReason : MonoBehaviour
{
    public string reason;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            GameObject.Find("Canvas").GetComponent<MainMenuManager>().Disconnected(reason);

            Destroy(this.gameObject);
        }
    }
}
