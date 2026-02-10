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
    // 1. Deklarujemy Lookup jako pole struktury
    private ComponentLookup<PlayerName> _playerNameLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 2. Inicjalizujemy Lookup w OnCreate (true = tylko do odczytu)
        _playerNameLookup = state.GetComponentLookup<PlayerName>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<EndSimulationEntityCommandBufferSystem.Singleton>())
            return;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 3. KLUCZOWE: Aktualizujemy stan Lookup na pocz¹tku OnUpdate
        _playerNameLookup.Update(ref state);

        foreach (var (health, activeHands, entity) in
                 SystemAPI.Query<RefRO<HealthComponent>, RefRO<ActiveHands>>()
                 .WithNone<IsDestroyedTag>()
                 .WithEntityAccess())
        {
            if (health.ValueRO.HealthPoints <= 0)
            {
                ecb.AddComponent<IsDestroyedTag>(entity);

                FixedString64Bytes victimName = "Unknown";
                FixedString64Bytes killerName = "Environment";

                // U¿ywamy pola _playerNameLookup zamiast lokalnej zmiennej
                if (_playerNameLookup.HasComponent(entity))
                    victimName = _playerNameLookup[entity].Value;

                if (_playerNameLookup.HasComponent(health.ValueRO.LastHitBy))
                    killerName = _playerNameLookup[health.ValueRO.LastHitBy].Value;

                // Logi s¹ bezpieczne w Burst, o ile u¿ywasz sta³ych stringów lub FixedStrings
                Debug.Log($"<color=white>[SERVER]</color> <color=red><b>{victimName}</b></color> was killed by <color=orange>{killerName}</color>");
                var eventEntity = ecb.CreateEntity();
                ecb.AddComponent(eventEntity, new KillEvent { KillerName = killerName });


                if (activeHands.ValueRO.LeftHandEntity != Entity.Null)
                    ecb.DestroyEntity(activeHands.ValueRO.LeftHandEntity);

                if (activeHands.ValueRO.RightHandEntity != Entity.Null)
                    ecb.DestroyEntity(activeHands.ValueRO.RightHandEntity);

                ecb.DestroyEntity(entity);
            }
        }
    }
}