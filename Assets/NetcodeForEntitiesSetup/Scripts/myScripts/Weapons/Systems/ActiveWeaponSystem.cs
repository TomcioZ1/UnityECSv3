using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = false)]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct ActiveWeaponSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>())
            return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Potrzebujemy dostêpu do komponentów transformacji broni
        var transformLookup = state.GetComponentLookup<LocalTransform>(true);

        foreach (var (activeWeapon, socket, entity) in
                 SystemAPI.Query<RefRW<ActiveWeapon>, WeaponSocket>()
                 .WithEntityAccess())
        {
            Entity newWeapon = activeWeapon.ValueRO.WeaponEntity;

            if (newWeapon != Entity.Null && newWeapon.Index < 0)
                continue;

            if (newWeapon != activeWeapon.ValueRO.PreviousWeaponEntity)
            {
                if (newWeapon != Entity.Null && transformLookup.HasComponent(newWeapon))
                {
                    // 1. Pobieramy aktualny transform broni (z prefaba)
                    var weaponTransform = transformLookup[newWeapon];

                    // 2. Zerujemy TYLKO pozycjê, zostawiaj¹c rotacjê i skalê z prefaba
                    weaponTransform.Position = float3.zero;

                    // 3. Aplikujemy do ECB
                    ecb.AddComponent(newWeapon, new Parent { Value = socket.WeaponSocketEntity });
                    ecb.SetComponent(newWeapon, weaponTransform);

                    if (state.WorldUnmanaged.IsServer())
                    {
                        ecb.AppendToBuffer(entity, new LinkedEntityGroup { Value = newWeapon });
                    }
                }

                activeWeapon.ValueRW.PreviousWeaponEntity = newWeapon;
            }
        }
    }
}