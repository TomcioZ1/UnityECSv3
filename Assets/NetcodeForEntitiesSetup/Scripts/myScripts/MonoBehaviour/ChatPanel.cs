using TMPro;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using System.ComponentModel.Design;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ChatPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject panel;
    public TMP_InputField inputField;
    public Transform content;
    public GameObject messagePrefab;
    public ScrollRect scrollRect;

    private EntityManager em;

    void Start()
    {
        panel.SetActive(false);
        //mockWiadomosci();
        // Pobranie EntityManager z ClientWorld
        if (ClientServerBootstrap.ClientWorld != null && ClientServerBootstrap.ClientWorld.IsCreated)
            em = ClientServerBootstrap.ClientWorld.EntityManager;
        else
            Debug.LogWarning("ClientWorld nie jest jeszcze gotowy!");
    }

    void Update()
    {
        // Sprawdzanie Enter
        bool enterPressed = false;
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        enterPressed = keyboard != null && keyboard.enterKey.wasPressedThisFrame;
#else
        enterPressed = UnityEngine.Input.GetKeyDown(KeyCode.Return);
#endif

        if (enterPressed)
        {
            //Debug.Log("Enter pressed in ChatPanel");
            if (!panel.activeSelf)
            {
                // Otwórz panel chatu
                OpenChat();
            }
            else if (!string.IsNullOrWhiteSpace(inputField.text))
            {
                // Wyœlij wiadomoœæ
                SendMessage();
            }
            else
            {
                CloseChat();
            }
        }

        // Obs³uga przychodz¹cych wiadomoœci
        if (em != null)
        {
            var msgQuery = em.CreateEntityQuery(typeof(ChatMessageEvent));
            var msgEntities = msgQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var e in msgEntities)
            {
                var msg = em.GetComponentData<ChatMessageEvent>(e).Message.ToString();
                AddMessage(msg);
                em.DestroyEntity(e);
            }
            msgEntities.Dispose();
        }
    }

    public void OpenChat()
    {
        panel.SetActive(true);
        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();
    }

    public void CloseChat()
    {
        panel.SetActive(false);
        inputField.DeactivateInputField();
    }

    public void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(inputField.text))
        {
            CloseChat();
            return;
        }

        if (em == null) return;

        var e = em.CreateEntity();
        em.AddComponentData(e, new ChatMessageRpc
        {
            Message = new FixedString128Bytes(inputField.text)
        });
        em.AddComponent<SendRpcCommandRequest>(e);
        inputField.text = "";
    }

    public void AddMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var msgObj = Instantiate(messagePrefab, content);
        var tmp = msgObj.GetComponent<TMP_Text>();
        if (tmp != null)
            tmp.text = text;

        // Auto-scroll
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }


    void mockWiadomosci()
    {
        for(int i = 0; i < 15; i++)
        {
            AddMessage(i.ToString());
        }
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
