using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerDeathSystem : ISystem
{
    private ComponentLookup<PlayerName> _playerNameLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _playerNameLookup = state.GetComponentLookup<PlayerName>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<EndSimulationEntityCommandBufferSystem.Singleton>())
            return;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        _playerNameLookup.Update(ref state);
        double currentTime = SystemAPI.Time.ElapsedTime;

        // Szukamy graczy z HP <= 0, którzy nie maj¹ jeszcze tagu zniszczenia
        foreach (var (health, activeHands, transform, entity) in
                 SystemAPI.Query<RefRO<HealthComponent>, RefRO<ActiveHands>, RefRW<LocalTransform>>()
                 .WithNone<IsDestroyedTag>()
                 .WithEntityAccess())
        {
            if (health.ValueRO.HealthPoints <= 0)
            {
                // 1. Dodajemy tagi stanu œmierci
                ecb.AddComponent<IsDestroyedTag>(entity);

                // Dodajemy timer respawnu (np. 3 sekundy)
                ecb.AddComponent(entity, new RespawnTimer
                {
                    RespawnAtTime = currentTime + 5.0f
                });

                // 2. Logika nazw (Victim/Killer)
                FixedString64Bytes victimName = "Unknown";
                FixedString64Bytes killerName = "Environment";

                if (_playerNameLookup.HasComponent(entity))
                    victimName = _playerNameLookup[entity].Value;

                if (_playerNameLookup.HasComponent(health.ValueRO.LastHitBy))
                    killerName = _playerNameLookup[health.ValueRO.LastHitBy].Value;

                Debug.Log($"<color=white>[SERVER]</color> <color=red><b>{victimName}</b></color> killed by <color=orange>{killerName}</color>");

                // Event dla UI/Killfeedu
                var eventEntity = ecb.CreateEntity();
                ecb.AddComponent(eventEntity, new KillEvent { KillerName = killerName });

                // 3. OBS£UGA FIZYKI I POZYCJI
                // Teleportacja pod mapê (bezpieczne miejsce)
                transform.ValueRW.Position = new float3(0, -100, 0);

                // Zerowanie prêdkoœci fizycznej, aby postaæ nie kozio³kowa³a w pró¿ni
                if (SystemAPI.HasComponent<PhysicsVelocity>(entity))
                {
                    ecb.SetComponent(entity, new PhysicsVelocity
                    {
                        Linear = float3.zero,
                        Angular = float3.zero
                    });
                }

                // 4. Sprz¹tanie ekwipunku
                

                // UWAGA: Nie wywo³ujemy ecb.DestroyEntity(entity)! 
                // Encja musi prze¿yæ, aby system respawnu móg³ j¹ przywróciæ.
            }
        }
    }
}


