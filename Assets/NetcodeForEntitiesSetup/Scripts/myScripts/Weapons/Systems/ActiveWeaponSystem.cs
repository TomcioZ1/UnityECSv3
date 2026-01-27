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
    // 1. Deklarujemy Lookup jako pole struktury
    private ComponentLookup<LocalTransform> _transformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // 2. Inicjalizujemy Lookup raz przy starcie systemu
        // true oznacza read-only (szybsze)
        _transformLookup = state.GetComponentLookup<LocalTransform>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>())
            return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 3. KLUCZOWE: Aktualizujemy stan Lookup na pocz¹tku ka¿dej klatki
        _transformLookup.Update(ref state);

        foreach (var (activeWeapon, socket, entity) in
                 SystemAPI.Query<RefRW<ActiveWeapon>, WeaponSocket>()
                 .WithEntityAccess())
        {
            Entity newWeapon = activeWeapon.ValueRO.WeaponEntity;

            if (newWeapon != Entity.Null && newWeapon.Index < 0)
                continue;

            if (newWeapon != activeWeapon.ValueRO.PreviousWeaponEntity)
            {
                // U¿ywamy zaktualizowanego pola _transformLookup
                if (newWeapon != Entity.Null && _transformLookup.HasComponent(newWeapon))
                {
                    // 1. Pobieramy aktualny transform broni
                    var weaponTransform = _transformLookup[newWeapon];

                    // 2. Zerujemy pozycjê (relatywnie do socketu)
                    weaponTransform.Position = float3.zero;

                    // 3. Aplikujemy zmiany
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