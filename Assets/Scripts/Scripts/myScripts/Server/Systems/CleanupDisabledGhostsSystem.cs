using Unity.Entities;
using Unity.NetCode;

// System dzia³a tylko w œwiecie klienta i symulacji
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct BoxClientVisualSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // U¿ywamy EntityCommandBuffer, aby bezpiecznie dodawaæ komponenty podczas iteracji
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // Zapytanie: szukamy BoxComponent, który ma flagê zniszczenia, 
        // ale encja NIE ma jeszcze komponentu Disabled (¿eby nie dodawaæ go co klatkê)
        foreach (var (box, entity) in SystemAPI.Query<RefRO<BoxComponent>>()
                     .WithNone<Disabled>()
                     .WithEntityAccess())
        {
            if (box.ValueRO.isDestoryed)
            {
                // Dodanie Disabled wy³¹czy renderowanie, fizykê i wszystkie inne systemy dla tej encji
                ecb.AddComponent<Disabled>(entity);
            }
        }
    }
}