using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI; // ScrollRect

public class ChatUI : MonoBehaviour
{
    [Header("UI Elements")]
    public static ChatUI Instance;      // Singleton

    public GameObject chatPanel;        // Panel chatu
    public TMP_InputField inputField;   // Input do wpisywania wiadomoœci
    public Transform content;           // Content ScrollView
    public GameObject messagePrefab;    // Prefab wiadomoœci (TMP_Text)
    public ScrollRect scrollRect;       // ScrollRect panelu

    private EntityManager em;

    void Awake()
    {
        // Inicjalizacja singletona
        Instance = this;
    }

    void Start()
    {
        // Pobranie EntityManager z ClientWorld
        if (ClientServerBootstrap.ClientWorld != null && ClientServerBootstrap.ClientWorld.IsCreated)
            em = ClientServerBootstrap.ClientWorld.EntityManager;
        else
            Debug.LogWarning("ClientWorld nie jest jeszcze utworzony!");

        // Panel zamkniêty na starcie
        chatPanel.SetActive(false);
    }

    void Update()
    {
        if (em == null) return;

        // W³¹czanie chatu po event ECS ToggleChatUI
        if (em.CreateEntityQuery(typeof(ToggleChatUI)).IsEmpty)
            return;

        // Usuñ event
        em.DestroyEntity(em.CreateEntityQuery(typeof(ToggleChatUI)));

        OpenChat();
    }

    void OpenChat()
    {
        chatPanel.SetActive(true);
        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();
    }

    void CloseChat()
    {
        chatPanel.SetActive(false);
        inputField.DeactivateInputField();
    }

    public void SendMessage()
    {
        string msg = inputField.text;
        if (string.IsNullOrWhiteSpace(msg))
        {
            CloseChat();
            return;
        }

        if (ClientServerBootstrap.ClientWorld == null || !ClientServerBootstrap.ClientWorld.IsCreated)
        {
            Debug.LogWarning("ClientWorld nie jest dostêpny, nie mo¿na wys³aæ RPC!");
            return;
        }

        // Tworzymy encjê RPC w ClientWorld
        var e = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntity();
        ClientServerBootstrap.ClientWorld.EntityManager.AddComponentData(e, new ChatMessageRpc
        {
            Message = new FixedString128Bytes(msg)
        });
        ClientServerBootstrap.ClientWorld.EntityManager.AddComponent<SendRpcCommandRequest>(e);

        CloseChat();
    }

    // Dodaj wiadomoœæ do panelu chatu
    /*public void AddMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // Jeœli panel nieaktywny, w³¹cz go minimalnie
        //if (!chatPanel.activeSelf)
           // chatPanel.SetActive(true);

        // Tworzymy instancjê wiadomoœci w Content
        var msgObj = Instantiate(messagePrefab, content);
        var tmp = msgObj.GetComponent<TMP_Text>();
        if (tmp != null)
            tmp.text = text;
        Debug.Log("ChatUI - Dodano wiadomoœæ: " + text);
        // Auto-scroll do do³u
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }*/
}
