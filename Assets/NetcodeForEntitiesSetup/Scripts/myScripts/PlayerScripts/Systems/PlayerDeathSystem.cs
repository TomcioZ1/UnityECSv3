using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerDeathSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // U¿ywamy EndSimulation, aby mieæ pewnoœæ, ¿e to ostatnia rzecz w klatce
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Pobieramy Lookup do nazw (Upewnij siê, ¿e namespace siê zgadza!)
        var nameLookup = state.GetComponentLookup<PlayerName>(true);

        // Dodajemy WithNone<IsDestroyedTag>(), ¿eby nie niszczyæ dwa razy tego samego gracza
        foreach (var (health, activeHands, entity) in
                 SystemAPI.Query<RefRO<HealthComponent>, RefRO<ActiveHands>>()
                 .WithNone<IsDestroyedTag>()
                 .WithEntityAccess())
        {
            if (health.ValueRO.HealthPoints <= 0)
            {
                // 1. Oznaczamy encjê natychmiast w ECB, aby pêtla jej wiêcej nie z³apa³a
                ecb.AddComponent<IsDestroyedTag>(entity);

                FixedString64Bytes victimName = "Unknown";
                FixedString64Bytes killerName = "Environment";

                // Pobieramy nazwê ofiary
                if (nameLookup.HasComponent(entity))
                    victimName = nameLookup[entity].Value;

                // Pobieramy nazwê zabójcy
                if (nameLookup.HasComponent(health.ValueRO.LastHitBy))
                    killerName = nameLookup[health.ValueRO.LastHitBy].Value;

                // LOG ŒMIERCI (Wersja kolorowa dla lepszej widocznoœci)
                Debug.Log($"<color=white>[SERVER]</color> <color=red><b>{victimName}</b></color> was killed by <color=orange>{killerName}</color>");

                // 2. Bezpieczne usuwanie r¹k
                if (activeHands.ValueRO.LeftHandEntity != Entity.Null)
                    ecb.DestroyEntity(activeHands.ValueRO.LeftHandEntity);

                if (activeHands.ValueRO.RightHandEntity != Entity.Null)
                    ecb.DestroyEntity(activeHands.ValueRO.RightHandEntity);

                // 3. Usuwanie gracza
                ecb.DestroyEntity(entity);
            }
        }
    }
}