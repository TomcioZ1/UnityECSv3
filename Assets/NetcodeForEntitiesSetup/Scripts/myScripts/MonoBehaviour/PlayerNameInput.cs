using UnityEngine;
using TMPro;
using Unity.NetCode;
using Unity.Entities;
using Unity.Collections;

public class PlayerNameInput : MonoBehaviour
{
    public string Name = "";

    

    public TMP_InputField inputField;

    public void SubmitName()
    {
        Name = inputField.text;
        PlayerInfoClass.PlayerName = Name;
        Debug.Log("Player name set to: " + Name);
    }

   
}
