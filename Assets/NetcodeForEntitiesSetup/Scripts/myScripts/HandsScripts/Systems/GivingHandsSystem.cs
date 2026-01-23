using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct GivingHandsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<HandsResources>()) return;

        // ZMIANA: U¿ywamy EndSimulation, aby encja by³a gotowa w nastêpnej klatce
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        Entity leftHandPrefab = SystemAPI.GetSingleton<HandsResources>().LeftHand;
        Entity rightHandPrefab = SystemAPI.GetSingleton<HandsResources>().RightHand;



        foreach (var (activeHands, playerEntity) in
                 SystemAPI.Query<RefRW<ActiveHands>>()
                 .WithEntityAccess())
        {
            if (activeHands.ValueRO.LeftHandEntity == Entity.Null)
            {
                // 1. Instantiate
                Entity leftHandSpawend = ecb.Instantiate(leftHandPrefab);
                Entity rightHandSpawend = ecb.Instantiate(rightHandPrefab);


                // 2. PRZYPISANIE - to trafi do GhostFielda po Playbacku ECB
                ecb.SetComponent(playerEntity, new ActiveHands
                {
                    LeftHandEntity = leftHandSpawend,
                    PrevLeftHand = Entity.Null,
                    RightHandEntity = rightHandSpawend,
                    PrevRightHand = Entity.Null
                });

                // 3. Ustawienie w³aœciciela
                if (SystemAPI.HasComponent<GhostOwner>(playerEntity))
                {
                    var playerOwner = SystemAPI.GetComponent<GhostOwner>(playerEntity);
                    ecb.SetComponent(leftHandSpawend, new GhostOwner { NetworkId = playerOwner.NetworkId });
                    ecb.SetComponent(rightHandSpawend, new GhostOwner { NetworkId = playerOwner.NetworkId });
                }

                ecb.AddComponent(leftHandSpawend, new WeaponOwner { Entity = playerEntity });
                ecb.AddComponent(rightHandSpawend, new WeaponOwner { Entity = playerEntity });
            }
        }
    }
}