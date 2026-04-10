using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using TMPro;

public class LeaderboardUIPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject cellPrefab; // Przeci¹gnij tutaj prefab LeaderboardCellPrefab
    public Transform container;  // Przeci¹gnij tutaj Content z Vertical Layout Group

    private void OnEnable()
    {
        // Odœwie¿amy przy ka¿dym w³¹czeniu (np. naciœniêciu TAB)
        RefreshLeaderboard();
    }

    public void RefreshLeaderboard()
    {
        // 1. ZnajdŸ œwiat klienta
        World clientWorld = null;
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                clientWorld = world;
                break;
            }
        }

        if (clientWorld == null) return;

        EntityManager em = clientWorld.EntityManager;

        // 2. Wyczyœæ stare wiersze
        foreach (Transform child in container)
        {
            
            Destroy(child.gameObject);
        }

        // 3. Pobierz dane z Singletona Leaderboarda
        var query = em.CreateEntityQuery(typeof(LeaderboardTag));
        if (query.IsEmpty) return;

        // Pobieramy encjê singletona (bezpiecznie dla NetCode)
        using var entities = query.ToEntityArray(Allocator.Temp);
        Entity leaderboardEntity = entities[0];

        if (!em.HasBuffer<LeaderboardElement>(leaderboardEntity)) return;

        var buffer = em.GetBuffer<LeaderboardElement>(leaderboardEntity);

        // 4. Instancjonuj nowe wiersze
        for (int i = 0; i < buffer.Length; i++)
        {
            var data = buffer[i];
            AddCell(i + 1, data.PlayerName.ToString(), data.Kills, data.Deaths);
        }
        //Debug.Log($"[LEADERBOARD UI] Odœwie¿ono tabelê wyników z {buffer.Length} wpisami.");
    }

    private void AddCell(int place, string pName, int kills, int deaths)
    {
        // Tworzymy obiekt z prefaba
        GameObject newCell = Instantiate(cellPrefab, container);

        // Pobieramy komponent i ustawiamy dane
        LeaderboardCell cellScript = newCell.GetComponent<LeaderboardCell>();
        if (cellScript != null)
        {
            cellScript.SetData(place, pName, kills, deaths);
        }

        // Jeœli masz ScrollRect, mo¿esz tu wymusiæ aktualizacjê layoutu
        // Canvas.ForceUpdateCanvases();
    }
}