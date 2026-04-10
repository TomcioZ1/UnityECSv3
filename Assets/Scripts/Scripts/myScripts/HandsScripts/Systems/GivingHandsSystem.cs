using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct GivingHandsSystem : ISystem
{
    private ComponentLookup<GhostOwner> _ghostOwnerLookup;

    public void OnCreate(ref SystemState state)
    {
        _ghostOwnerLookup = state.GetComponentLookup<GhostOwner>(true);

        // KLUCZOWE: System nie ruszy, dopóki te 3 rzeczy nie pojawi¹ siê na serwerze
        state.RequireForUpdate<HandsResources>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<ActiveHands>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Podwójne zabezpieczenie dla Burst
        if (!SystemAPI.TryGetSingleton<HandsResources>(out var resources)) return;
        if (!SystemAPI.TryGetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>(out var ecbSystem)) return;

        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
        _ghostOwnerLookup.Update(ref state);

        foreach (var (activeHands, playerEntity) in
                 SystemAPI.Query<RefRO<ActiveHands>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            if (activeHands.ValueRO.LeftHandEntity == Entity.Null)
            {
                Entity leftHandSpawned = ecb.Instantiate(resources.LeftHand);
                Entity rightHandSpawned = ecb.Instantiate(resources.RightHand);

                ecb.SetComponent(playerEntity, new ActiveHands
                {
                    LeftHandEntity = leftHandSpawned,
                    PrevLeftHand = Entity.Null,
                    RightHandEntity = rightHandSpawned,
                    PrevRightHand = Entity.Null
                });

                if (_ghostOwnerLookup.HasComponent(playerEntity))
                {
                    var playerOwner = _ghostOwnerLookup[playerEntity];
                    var ownerData = new GhostOwner { NetworkId = playerOwner.NetworkId };

                    ecb.SetComponent(leftHandSpawned, ownerData);
                    ecb.SetComponent(rightHandSpawned, ownerData);

                    ecb.AddComponent(leftHandSpawned, new HandsOwner { Entity = playerEntity });
                    ecb.AddComponent(rightHandSpawned, new HandsOwner { Entity = playerEntity });
                }

                ecb.AppendToBuffer(playerEntity, new LinkedEntityGroup { Value = leftHandSpawned });
                ecb.AppendToBuffer(playerEntity, new LinkedEntityGroup { Value = rightHandSpawned });

                var weaponOwner = new WeaponOwner { Entity = playerEntity };
                ecb.AddComponent(leftHandSpawned, weaponOwner);
                ecb.AddComponent(rightHandSpawned, weaponOwner);
            }
        }
    }
}