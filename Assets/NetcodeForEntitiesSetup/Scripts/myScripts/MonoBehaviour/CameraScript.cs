using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;

public class EntityCameraFollow : MonoBehaviour
{
    [Header("Ustawienia Œledzenia")]
    [SerializeField] private Vector3 offset = new Vector3(0, 5f, -10f);
    [SerializeField] private float smoothness = 10f;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0, 1.5f, 0);

    private World _clientWorld;

    void LateUpdate()
    {
        // 1. Szukanie œwiata klienta
        if (_clientWorld == null || !_clientWorld.IsCreated)
        {
            _clientWorld = FindClientWorld();
            if (_clientWorld == null) return;
        }

        var em = _clientWorld.EntityManager;

        // 2. Zapytanie o lokalnego gracza (GhostOwnerIsLocal jest kluczowy)
        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<GhostOwnerIsLocal>()
        );

        // U¿ywamy CalculateEntityCount zamiast HasFilter() dla pewnoœci diagnostyki
        int count = query.CalculateEntityCount();

        if (count > 0)
        {
            using var entities = query.ToEntityArray(Allocator.Temp);
            var ltw = em.GetComponentData<LocalToWorld>(entities[0]);
            Vector3 playerPos = ltw.Position;

            Vector3 targetCameraPos = playerPos + offset;

            // U¿ywamy pozycji cameraGameObject zamiast transform.position skryptu
            transform.position = Vector3.Lerp(transform.position, targetCameraPos, Time.deltaTime * smoothness);
            transform.LookAt(playerPos + lookAtOffset);
           
        }
        else
        {
            // Loguj co jakiœ czas, jeœli nie widzi gracza mimo œwiata klienta
            if (Time.frameCount % 100 == 0)
                Debug.Log("[CameraFollow] Œwiat klienta OK, ale nie widzê encji z GhostOwnerIsLocal.");
        }
    }

    private World FindClientWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                Debug.Log($"[CameraFollow] Sukces! Znaleziono œwiat klienta: {world.Name}");
                return world; // Zwróæ TYLKO jeœli warunki s¹ spe³nione
            }
        }

        if (Time.frameCount % 100 == 0)
            Debug.Log("[CameraFollow] Szukam œwiata klienta...");

        return null;
    }
}