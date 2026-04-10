using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;

public class EntityCameraFollow : MonoBehaviour
{
    [Header("Ustawienia Œledzenia")]
    [SerializeField] private Vector3 offset = new Vector3(0, 20f, 0);
    [SerializeField] private float smoothness = 20f;

    [Header("Ustawienia K¹ta")]
    [Range(0, 90)]
    [SerializeField] private float pitchAngle = 90f; // K¹t patrzenia w dó³ (X)

    private World _clientWorld;

    void LateUpdate()
    {
        if (_clientWorld == null || !_clientWorld.IsCreated)
        {
            _clientWorld = FindClientWorld();
            if (_clientWorld == null) return;
        }

        var em = _clientWorld.EntityManager;
        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<GhostOwnerIsLocal>()
        );

        if (query.CalculateEntityCount() > 0)
        {
            using var entities = query.ToEntityArray(Allocator.Temp);
            var ltw = em.GetComponentData<LocalToWorld>(entities[0]);

            // 1. Pozycja docelowa (zgodna z koordynatami œwiata)
            Vector3 targetPos = (Vector3)ltw.Position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothness);

            // 2. KLUCZ: Wymuszenie rotacji kamery "w dó³"
            // Ustawiamy X na k¹t nachylenia, a Y i Z na ZERO.
            // Dziêki temu Y=0 sprawia, ¿e krawêdzie ekranu s¹ idealnie równoleg³e do osi X i Z œwiata.
            transform.rotation = Quaternion.Euler(pitchAngle, 0, 0);
        }
    }

    private World FindClientWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient()) return world;
        }
        return null;
    }
}