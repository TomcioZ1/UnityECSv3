using Unity.Burst;
using Unity.Entities;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;

// Pozwala klientowi na zmiane broni

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct WeaponSyncServerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // System dzia³a na serwerze i synchronizuje intencjê gracza z jego stanem Ghost
        foreach (var (input, inventory) in
                 SystemAPI.Query<RefRO<MyPlayerInput>, RefRW<PlayerInventory>>()
                 .WithAll<Simulate>())
        {
            // 1. Sprawdzamy, czy gracz nacisn¹³ klawisz zmiany slotu (1-4)
            // Zak³adamy, ¿e input.choosenWeapon zwraca numer slotu, który gracz chce wybraæ
            byte requestedSlot = input.ValueRO.choosenWeapon;

            if (requestedSlot >= 1 && requestedSlot <= 4)
            {
                // Aktualizujemy ActiveSlotIndex, który jest [GhostField]
                // Dziêki temu wszyscy klienci dowiedz¹ siê, który slot jest teraz aktywny
                inventory.ValueRW.ActiveSlotIndex = requestedSlot;
            }

            // Opcjonalnie: Tutaj mo¿esz dodaæ logikê "u¿ywania" przedmiotu,
            // jeœli np. Slot 4 to granaty zu¿ywalne.
        }
    }
}