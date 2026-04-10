using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct PlayerHealthUISystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. SprawdŸ czy UI istnieje i czy mamy po³¹czenie
        if (PlayerHealthUIController.Instance == null) return;
        if (!SystemAPI.HasSingleton<NetworkId>()) return;

        int localNetId = SystemAPI.GetSingleton<NetworkId>().Value;

        // 2. Szukamy lokalnego gracza
        // Musimy pobraæ HealthComponent oraz GhostOwner (by sprawdziæ czy to my)
        foreach (var (health, ghostOwner) in
                 SystemAPI.Query<RefRO<HealthComponent>, RefRO<GhostOwner>>())
        {
            if (ghostOwner.ValueRO.NetworkId != localNetId) continue;

            // 3. Pobieramy max zdrowie (jeœli nie masz go w HealthComponent, 
            // mo¿esz u¿yæ sta³ej lub dodaæ pole MaxHealth do komponentu)
            int currentHealth = health.ValueRO.HealthPoints;
            int maxHealth = 100; // Domyœlnie z Twojego Authoring

            // 4. Aktualizujemy UI
            PlayerHealthUIController.Instance.UpdateHealth(currentHealth, maxHealth);

            // ZnaleŸliœmy siebie, nie ma sensu mieliæ dalej pêtli
            break;
        }
    }
}