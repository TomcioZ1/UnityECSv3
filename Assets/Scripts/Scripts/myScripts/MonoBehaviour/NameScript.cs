using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using TMPro;
using UnityEngine;
using Unity.NetCode;

public class NameScript : MonoBehaviour
{
    [Header("Ustawienia Prefaba")]
    [SerializeField] private GameObject textMeshPrefab; // Prefab ze zwyk³ym TextMeshPro (nie UI)
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);

    [Header("Diagnostyka")]
    [SerializeField] private bool showDebugLogs = true;

    private Dictionary<Entity, TextMeshPro> _activeLabels = new Dictionary<Entity, TextMeshPro>();
    private World _clientWorld;

    void Update()
    {
        // 1. ZnajdŸ œwiat klienta (wymagane w Netcode for Entities)
        if (_clientWorld == null || !_clientWorld.IsCreated)
        {
            _clientWorld = FindClientWorld();
            if (_clientWorld == null) return;
        }

        EntityManager em = _clientWorld.EntityManager;

        // 2. Stwórz zapytanie o encje posiadaj¹ce komponenty PlayerName i LocalToWorld
        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerName>(),
            ComponentType.ReadOnly<LocalToWorld>()
        );

        // Pobieramy encje przy u¿yciu Allocator.Temp, co jest bezpieczne w MonoBehaviour
        using (var entities = query.ToEntityArray(Allocator.Temp))
        {
            HashSet<Entity> seenEntities = new HashSet<Entity>();

            foreach (var entity in entities)
            {
                seenEntities.Add(entity);

                // Jeœli nie mamy jeszcze etykiety dla tej encji, stwórzmy j¹
                if (!_activeLabels.ContainsKey(entity))
                {
                    CreateNewLabel(entity, em);
                }

                // Aktualizuj pozycjê i tekst
                UpdateLabel(entity, em);
            }

            // 3. Usuñ etykiety dla encji, które przesta³y istnieæ (np. gracz wyszed³)
            CleanupOrphanedLabels(seenEntities);
        }
    }

    private void CreateNewLabel(Entity entity, EntityManager em)
    {
        if (textMeshPrefab == null)
        {
            Debug.LogError("NameScript: Nie przypisano prefaba TextMeshPro w Inspektorze!");
            return;
        }

        // Spawnujemy pod tym obiektem (NameManagerem)
        GameObject go = Instantiate(textMeshPrefab, transform);
        var tmp = go.GetComponent<TextMeshPro>();

        if (tmp != null)
        {
            _activeLabels.Add(entity, tmp);
            //if (showDebugLogs) Debug.Log($"<color=green>[NameScript] Stworzono napis dla encji: {entity}</color>");
        }
        else
        {
            Debug.LogError("NameScript: Prefab nie posiada komponentu TextMeshPro!");
            Destroy(go);
        }
    }

    private void UpdateLabel(Entity entity, EntityManager em)
    {
        // BEZPIECZEÑSTWO: Sprawdzamy czy encja wci¹¿ ma dane i czy label fizycznie istnieje
        if (!em.HasComponent<PlayerName>(entity) || !em.HasComponent<LocalToWorld>(entity)) return;
        if (!_activeLabels.TryGetValue(entity, out var label) || label == null) return;

        try
        {
            // Pobieranie danych z ECS
            var nameData = em.GetComponentData<PlayerName>(entity);
            var transformData = em.GetComponentData<LocalToWorld>(entity);

            // Synchronizacja treœci
            label.text = nameData.Value.ToString();

            // Synchronizacja pozycji (Pozycja encji + offset)
            label.transform.position = (Vector3)transformData.Position + offset;

            // Billboard: Obrót w stronê kamery
            Camera cam = Camera.main;
            if (cam != null)
            {
                label.transform.rotation = cam.transform.rotation;
            }
        }
        catch (System.Exception e)
        {
            // To z³apie ewentualne b³êdy dostêpu do danych w trakcie ich usuwania
            Debug.LogWarning($"NameScript: B³¹d aktualizacji {entity}: {e.Message}");
        }
    }

    private void CleanupOrphanedLabels(HashSet<Entity> currentEntities)
    {
        List<Entity> toRemove = new List<Entity>();

        // Szukamy kluczy w s³owniku, których nie ma ju¿ w aktualnej liœcie encji
        foreach (var entity in _activeLabels.Keys)
        {
            if (!currentEntities.Contains(entity))
            {
                toRemove.Add(entity);
            }
        }

        // Fizyczne usuwanie obiektów i czyszczenie s³ownika
        foreach (var entity in toRemove)
        {
            if (showDebugLogs) Debug.Log($"<color=red>[NameScript] Usuwanie napisu dla encji: {entity}</color>");

            if (_activeLabels[entity] != null)
            {
                Destroy(_activeLabels[entity].gameObject);
            }
            _activeLabels.Remove(entity);
        }
    }

    private World FindClientWorld()
    {
        foreach (var world in World.All)
        {
            // Filtrujemy œwiaty, by znaleŸæ ten nale¿¹cy do Klienta (nie ThinClient/Bot)
            if (world.IsClient() && !world.IsThinClient())
            {
                return world;
            }
        }
        return null;
    }
}