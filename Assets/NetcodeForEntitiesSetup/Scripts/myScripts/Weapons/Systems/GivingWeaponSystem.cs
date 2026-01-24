using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct GrantWeaponSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<WeaponResources>()) return;

        // ZMIANA: U¿ywamy EndSimulation, aby encja by³a gotowa w nastêpnej klatce
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        Entity weaponPrefab = SystemAPI.GetSingleton<WeaponResources>().Pistol;

      

        foreach (var (activeWeapon, playerEntity) in
                 SystemAPI.Query<RefRW<ActiveWeapon>>()
                 .WithEntityAccess())
        {
            if (activeWeapon.ValueRO.WeaponEntity == Entity.Null)
            {
                // 1. Instantiate
                Entity spawnedWeapon = ecb.Instantiate(weaponPrefab);

                // 2. PRZYPISANIE - to trafi do GhostFielda po Playbacku ECB
                ecb.SetComponent(playerEntity, new ActiveWeapon
                {
                    WeaponEntity = spawnedWeapon,
                    PreviousWeaponEntity = Entity.Null
                });

                // 3. Ustawienie w³aœciciela
                if (SystemAPI.HasComponent<GhostOwner>(playerEntity))
                {
                    var playerOwner = SystemAPI.GetComponent<GhostOwner>(playerEntity);
                    ecb.SetComponent(spawnedWeapon, new GhostOwner { NetworkId = playerOwner.NetworkId });
                }

                ecb.AddComponent(spawnedWeapon, new WeaponOwner { Entity = playerEntity });
            }
        }
    }
}