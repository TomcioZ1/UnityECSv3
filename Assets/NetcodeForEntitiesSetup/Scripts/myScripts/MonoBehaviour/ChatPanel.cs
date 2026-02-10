using TMPro;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;

public class ChatPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField inputField;
    public Transform content;
    public GameObject messagePrefab;
    public ScrollRect scrollRect;

    void Update()
    {
        World clientWorld = GetClientWorld();
        if (clientWorld == null) return;

        var em = clientWorld.EntityManager;

        // Obs³uga przychodz¹cych wiadomoœci (ChatMessageEvent musi istnieæ w Twoim kodzie)
        var msgQuery = em.CreateEntityQuery(typeof(ChatMessageEvent));
        if (!msgQuery.IsEmpty)
        {
            var msgEntities = msgQuery.ToEntityArray(Allocator.Temp);

            foreach (var e in msgEntities)
            {
                var data = em.GetComponentData<ChatMessageEvent>(e);
                AddMessage($"<color=#00FF00>{data.Sender}:</color> {data.Message}");
                em.DestroyEntity(e);
            }
            msgEntities.Dispose();
        }
    }

    public void OpenChat()
    {
        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();
    }

    public void CloseChat()
    {
        inputField.DeactivateInputField();
        // Czyœcimy pole przy zamkniêciu, ¿eby nie "wisia³o" przy nastêpnym otwarciu
        inputField.text = "";
    }

    public void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        World clientWorld = GetClientWorld();
        if (clientWorld == null) return;
        var em = clientWorld.EntityManager;

        // 1. Pobierz po³¹czenie - U¿ywamy ToEntityArray zamiast GetSingletonEntity
        var connectionQuery = em.CreateEntityQuery(typeof(NetworkStreamConnection));
        if (connectionQuery.IsEmpty) return;

        // Pobieramy pierwsz¹ encjê z brzegu (dla klienta zawsze bêdzie to po³¹czenie z serwerem)
        using var connections = connectionQuery.ToEntityArray(Allocator.Temp);
        Entity connection = connections[0];

        // 2. Pobierz nazwê lokalnego gracza
        var playerQuery = em.CreateEntityQuery(typeof(PlayerName), typeof(GhostOwnerIsLocal));
        if (playerQuery.IsEmpty)
        {
            Debug.LogWarning("Nie znaleziono lokalnego gracza!");
            return;
        }

        using var players = playerQuery.ToEntityArray(Allocator.Temp);
        Entity localPlayer = players[0];

        // 3. Stwórz RPC
        var e = em.CreateEntity();
        em.AddComponentData(e, new ChatMessageRpc
        {
            Sender = em.GetComponentData<PlayerName>(localPlayer).Value,
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

        // Auto-scroll do do³u
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