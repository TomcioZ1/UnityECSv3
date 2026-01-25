/*using Unity.Burst;
using Unity.Entities;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct ChoosenWeaponSystem : ISystem
{
    // Burst dopuszcza EntityManager w OnUpdate, jeœli nie u¿ywamy Jobów
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<WeaponResources>(out var resources)) return;

        // UWAGA: Nie u¿ywamy ECB do Instantiate, u¿ywamy bezpoœrednio EntityManager
        var entityManager = state.EntityManager;

        foreach (var (activeWeapon, input, socket, playerEntity) in
                 SystemAPI.Query<RefRW<ActiveWeapon>, RefRO<MyPlayerInput>, RefRO<WeaponSocket>>()
                 .WithEntityAccess())
        {
            if (activeWeapon.ValueRO.SelectedWeaponId != input.ValueRO.choosenWeapon)
            {
                byte newId = input.ValueRO.choosenWeapon;

                // 1. Natychmiastowe usuniêcie starej broni
                if (activeWeapon.ValueRO.WeaponEntity != Entity.Null && entityManager.Exists(activeWeapon.ValueRO.WeaponEntity))
                {
                    entityManager.DestroyEntity(activeWeapon.ValueRO.WeaponEntity);
                }

                Entity prefabToSpawn = newId switch
                {
                    1 => resources.Pistol,
                    2 => resources.Shotgun,
                    3 => resources.ak47,
                    4 => resources.Sniper,
                    _ => Entity.Null
                };

                // 2. Natychmiastowe stworzenie nowej broni
                if (prefabToSpawn != Entity.Null)
                {
                    // To dzieje siê TU I TERAZ. Nowa encja ma od razu Index >= 0
                    Entity newWeapon = entityManager.Instantiate(prefabToSpawn);

                    // Konfiguracja komponentów (równie¿ natychmiastowa)
                    if (entityManager.HasComponent<GhostOwner>(playerEntity))
                    {
                        var owner = entityManager.GetComponentData<GhostOwner>(playerEntity);
                        entityManager.SetComponentData(newWeapon, new GhostOwner { NetworkId = owner.NetworkId });
                    }

                    entityManager.AddComponentData(newWeapon, new WeaponOwner { Entity = playerEntity });
                    entityManager.SetComponentData(newWeapon, new Parent { Value = socket.ValueRO.WeaponSocketEntity });
                    entityManager.SetComponentData(newWeapon, LocalTransform.FromPosition(Unity.Mathematics.float3.zero));

                    // PRZYPISANIE JEST BEZPIECZNE - encja ju¿ w pe³ni istnieje
                    activeWeapon.ValueRW.WeaponEntity = newWeapon;
                }
                else
                {
                    activeWeapon.ValueRW.WeaponEntity = Entity.Null;
                }

                activeWeapon.ValueRW.SelectedWeaponId = newId;
            }
        }
    }
}*/