using Unity.Burst;
using Unity.Entities;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct WeaponSyncServerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (input, activeWeapon) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRW<ActiveWeapon>>())
        {
            // Przepisujemy lokalny input do pola, które widz¹ wszyscy
            activeWeapon.ValueRW.SelectedWeaponId = input.ValueRO.choosenWeapon;
        }
    }
}