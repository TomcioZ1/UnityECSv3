/*using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct CameraFollowSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. Szukamy gracza, który jest lokalny (nale¿y do nas)
        // U¿ywamy najprostszej sk³adni bez RefReadOnly
        Entity localPlayer = Entity.Null;
        float3 playerPos = float3.zero;

        // Sk³adnia Query w Unity 6: 
        // Pierwszy parametr to dane, które chcemy czytaæ, 
        // WithAll filtruje encje tylko do tych z GhostOwnerIsLocal
        foreach (var (ltw, entity) in SystemAPI.Query<LocalToWorld>().WithAll<GhostOwnerIsLocal>().WithEntityAccess())
        {
            localPlayer = entity;
            playerPos = ltw.Position;
            break;
        }

        // 2. Jeœli nie znaleziono gracza, przerywamy
        if (localPlayer == Entity.Null) return;
        

            // 3. Aktualizacja kamery
            var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Debug.Log("jest kamera");
            // Konfiguracja widoku
            float3 offset = new float3(0, 6f, -10f);
            Vector3 targetPosition = (Vector3)(playerPos + offset);

            // Ustawienie pozycji kamery
            mainCamera.transform.position = targetPosition;

            // Kamera patrzy na gracza (lekko nad jego œrodkiem)
            mainCamera.transform.LookAt((Vector3)playerPos + Vector3.up * 1.5f);
        }
    }
}*/