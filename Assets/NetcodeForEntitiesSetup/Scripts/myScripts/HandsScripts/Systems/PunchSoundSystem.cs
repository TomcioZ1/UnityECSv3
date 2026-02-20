using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct PunchSoundSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // BeginSimulationEntityCommandBufferSystem gwarantuje, øe usuniemy tag zaraz po przetworzeniu
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Przeszukujemy encje, ktÛre majπ wys≥any event ataku
        foreach (var (punchEvent, entity) in SystemAPI.Query<PunchFiredEvent>().WithEntityAccess())
        {
            // TWORZYMY Ø•DANIE DèWI KU
            Entity soundReq = ecb.CreateEntity();
            ecb.AddComponent(soundReq, new PlaySoundRequest
            {
                SoundID = 4, // ID düwiÍku uderzenia
                Position = punchEvent.Position,
                IsLoop = false
            });

            // LOGUJEMY (pojawi siÍ tylko raz na uderzenie!)
            //UnityEngine.Debug.Log($"[SoundSystem] Odtwarzam düwiÍk na pozycji {punchEvent.Position}");

            // USUWAMY KOMPONENT, aby nie odtwarzaÊ go w nastÍpnej klatce
            ecb.RemoveComponent<PunchFiredEvent>(entity);
        }
    }
}