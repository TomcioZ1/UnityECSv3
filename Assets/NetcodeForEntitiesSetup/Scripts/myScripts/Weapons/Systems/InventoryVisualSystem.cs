using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = false)]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct InventoryVisualSystem : ISystem
{
    private ComponentLookup<LocalTransform> _transformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _transformLookup = state.GetComponentLookup<LocalTransform>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>())
            return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        _transformLookup.Update(ref state);

        // Zapytanie o graczy z nowym inwentarzem
        foreach (var (inventory, socket, entity) in
                 SystemAPI.Query<RefRW<PlayerInventory>, WeaponSocket>()
                 .WithEntityAccess())
        {
            Entity currentWeapon = inventory.ValueRO.CurrentWeaponEntity;

            // Sprawdzamy, czy encja w d³oni zmieni³a siê od ostatniej klatki
            // Wykorzystujemy CurrentlySpawnedWeaponId jako pomocniczy znacznik zmiany
            if (currentWeapon != inventory.ValueRO.CurrentWeaponEntity ||
                currentWeapon != Entity.Null && !state.EntityManager.HasComponent<Parent>(currentWeapon))
            {
                // Jeœli encja istnieje i ma transform, ustawiamy jej rodzica (Parent)
                if (currentWeapon != Entity.Null && _transformLookup.HasComponent(currentWeapon))
                {
                    var weaponTransform = _transformLookup[currentWeapon];

                    // Zerujemy pozycjê lokaln¹, aby broñ "wskoczy³a" w punkt socketu
                    weaponTransform.Position = float3.zero;

                    // Przypinamy broñ do socketu (np. koœæ d³oni)
                    ecb.AddComponent(currentWeapon, new Parent { Value = socket.WeaponSocketEntity });
                    ecb.SetComponent(currentWeapon, weaponTransform);

                    // Na serwerze dodajemy do LinkedEntityGroup, aby broñ zosta³a usuniêta wraz z graczem
                    if (state.WorldUnmanaged.IsServer())
                    {
                        ecb.AppendToBuffer(entity, new LinkedEntityGroup { Value = currentWeapon });
                    }
                }
            }
        }
    }
}