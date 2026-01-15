using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using System.Collections.Generic;
using Unity.Mathematics;

public class HealthBarManager : MonoBehaviour
{
    public GameObject HealthBarPrefab;

    [Header("Ustawienia Pozycji")]
    public float3 HealthBarOffset = new float3(0, 2.5f, 0);

    private Dictionary<Entity, HealthBarLink> _activeBars = new Dictionary<Entity, HealthBarLink>();

    void Update()
    {
        // 1. Znalezienie w³aœciwego œwiata klienta
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
        var em = clientWorld.EntityManager;

        // 2. Pobranie Twojego unikalnego NetworkID (Singleton)
        int myNetworkId = -1;
        var networkIdQuery = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
        if (networkIdQuery.HasSingleton<NetworkId>())
        {
            myNetworkId = networkIdQuery.GetSingleton<NetworkId>().Value;
        }

        // 3. Pobranie wszystkich encji graczy (Cubes)
        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerHealthComponent>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<GhostOwner>() // U¿ywamy GhostOwner zamiast tagu IsLocal
        );

        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        foreach (var entity in entities)
        {
            // Zarz¹dzanie instancjami pasków w s³owniku
            if (!_activeBars.ContainsKey(entity))
            {
                var barGo = Instantiate(HealthBarPrefab, transform);
                var link = barGo.GetComponent<HealthBarLink>();
                link.TargetEntity = entity;
                link.Manager = em;
                _activeBars.Add(entity, link);
            }

            var linkRef = _activeBars[entity];
            var transformData = em.GetComponentData<LocalTransform>(entity);
            var health = em.GetComponentData<PlayerHealthComponent>(entity);

            // 4. LOGIKA KOLORU: Porównujemy NetworkId w³aœciciela z naszym NetworkId
            bool isLocal = false;
            if (em.HasComponent<GhostOwner>(entity))
            {
                var owner = em.GetComponentData<GhostOwner>(entity);
                isLocal = (owner.NetworkId == myNetworkId);
            }

            // Aktualizacja pozycji i rotacji paska (Billboard)
            linkRef.transform.position = transformData.Position + HealthBarOffset;

            if (Camera.main != null)
                linkRef.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

            // Wywo³anie aktualizacji wizualnej
            linkRef.UpdateHealth(health.HealthPoints, isLocal);
        }

        // 5. Usuwanie pasków dla graczy, którzy wyszli z gry
        var toRemove = new List<Entity>();
        foreach (var pair in _activeBars)
        {
            if (!em.Exists(pair.Key))
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
                toRemove.Add(pair.Key);
            }
        }
        foreach (var e in toRemove) _activeBars.Remove(e);

        entities.Dispose();
    }
}