using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerListItem : MonoBehaviour
{
    public string NameOfPlayer;
    public string PlayerId;

    public TMP_Text NamePlate;

    public void SetValues(string name, string playerId)
    {
        NameOfPlayer = name;
        PlayerId = playerId;

        NamePlate.text = $"{NameOfPlayer}\n{PlayerId}";

        Debug.Log("Values set");
    }
}
