using UnityEngine;
using TMPro;
using Unity.NetCode;
using Unity.Entities;
using Unity.Collections;

public class PlayerNameInput : MonoBehaviour
{
    public static PlayerNameInput Instance;
    public string Name = "";

    private void Awake()
    {
        Instance = this;
        Name = PlayerInfoClass.PlayerName;
    }

    public TMP_InputField inputField;

    public void SubmitName()
    {
        Name = inputField.text;
        Debug.Log("Player name set to: " + Name);
    }

   
}
