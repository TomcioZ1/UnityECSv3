/*using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
// Uruchamiamy na samym koñcu, ¿eby mieæ pewnoœæ, ¿e Netcode skoñczy³ synchronizacjê
[UpdateInGroup(typeof(GhostSimulationSystemGroup), OrderLast = true)]
public partial struct CleanupDisabledGhostsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Pobieramy ECB, aby bezpiecznie usuwaæ encje
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // PÊTLA PO RZECZACH Z DISABLED:
        // 1. Szukamy komponentu BoxComponent (lub innego, który identyfikuje Twoje obiekty)
        // 2. Musi posiadaæ tag Disabled (WithAll)
        // 3. .WithPresent<Disabled>() - wymusza na ECS pokazanie tych encji w zapytaniu
        foreach (var (box, entity) in SystemAPI.Query<RefRO<BoxComponent>>()
                     .WithAll<Disabled>()
                     .WithPresent<Disabled>()
                     .WithEntityAccess())
        {
            // Logika usuwania:
            // Poniewa¿ serwer ju¿ "wy³¹czy³" ten obiekt (doda³ Disabled),
            // klient mo¿e go teraz bezpiecznie usun¹æ ze swojego œwiata.
            ecb.DestroyEntity(entity);

            // UnityEngine.Debug.Log($"[Cleanup] Usuniêto encjê {entity.Index} (by³a Disabled)");
        }
    }
}*/