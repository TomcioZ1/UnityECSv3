using TMPro;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using System.ComponentModel.Design;
using Unity.VisualScripting;
using System.Linq;




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

        World clientWorld = GetClientWorld();
        if (clientWorld == null) return;

        var em = clientWorld.EntityManager;

        // 1. Musimy znaleŸæ encjê, która posiada ten komponent
        var query = em.CreateEntityQuery(typeof(PressedKeyesComponent));

        if (!query.IsEmpty)
        {
            Entity entity = query.GetSingletonEntity();

            // 1. Pobierasz KOPIE danych
            var data = em.GetComponentData<PressedKeyesComponent>(entity);
            

            if (enterPressed && !em.GetComponentData<PressedKeyesComponent>(entity).EscPressed)
            {
                //Debug.Log("Enter pressed in ChatPanel");
                if (!panel.activeSelf)
                {
                    // Otwórz panel chatu
                    OpenChat();
                    // 2. Zmieniasz wartoœæ w KOPII
                    data.EnterPressed = true;

                }
                else if (!string.IsNullOrWhiteSpace(inputField.text))
                {
                    // Wyœlij wiadomoœæ
                    SendMessage();
                    OpenChat();
                    // 2. Zmieniasz wartoœæ w KOPII
                    data.EnterPressed = true;

                }
                else
                {
                    CloseChat();
                    // 2. Zmieniasz wartoœæ w KOPII
                    data.EnterPressed = false;

                }
                // 3. KLUCZOWY KROK: Wysy³asz zmodyfikowan¹ kopiê z powrotem do ECS
                em.SetComponentData(entity, data);
            }
        }

        // Obs³uga przychodz¹cych wiadomoœci
        var msgQuery = em.CreateEntityQuery(typeof(ChatMessageEvent));
        var msgEntities = msgQuery.ToEntityArray(Allocator.Temp);

        foreach (var e in msgEntities)
        {
            var data = em.GetComponentData<ChatMessageEvent>(e);
            AddMessage($"<color=#00FF00>{data.Sender}:</color> {data.Message}");
            em.DestroyEntity(e); //  event jednorazowy
        }

        msgEntities.Dispose();

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
        if (string.IsNullOrWhiteSpace(inputField.text)) return;
        if (em == null) return;

        // pobierz po³¹czenie klienta z serwerem
        var connectionQuery = em.CreateEntityQuery(typeof(NetworkStreamConnection));
        if (connectionQuery.IsEmpty) return;

        var connection = connectionQuery.GetSingletonEntity();

        var e = em.CreateEntity();

        em.AddComponentData(e, new ChatMessageRpc
        {
            Sender = em.GetComponentData<PlayerName>(
            em.CreateEntityQuery(typeof(PlayerName), typeof(GhostOwnerIsLocal))
              .ToEntityArray(Allocator.Temp)[0]
            ).Value,
            Message = new FixedString128Bytes(inputField.text)
        });

        em.AddComponentData(e, new SendRpcCommandRequest
        {
            TargetConnection = connection
        });

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



    private World GetClientWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                return world;
            }
        }
        return null;
    }
}
