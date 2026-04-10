using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]

// System prze³adowania broni
public partial struct ReloadWeaponSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        double currentTime = SystemAPI.Time.ElapsedTime;

        // Iterujemy bezpoœrednio po broniach
        foreach (var (wState, weapon) in
                 SystemAPI.Query<RefRW<WeaponWorkState>, RefRW<WeaponData>>()
                 .WithAll<Simulate>())
        {
            if (!wState.ValueRO.IsReloading) continue;

            if (currentTime >= wState.ValueRO.ReloadTimer)
            {
                weapon.ValueRW.currentAmmo = weapon.ValueRO.magSize;
                wState.ValueRW.IsReloading = false;
            }
        }
    }
}