/*using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct BoxVisualSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Pobieramy ECB, aby bezpiecznie niszczyæ skrzynki na koniec klatki
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var isServer = state.WorldUnmanaged.IsServer();

        // Query szuka skrzynek z odpowiednimi komponentami
        foreach (var (box, health, transform, entity) in
                 SystemAPI.Query<RefRO<BoxComponent>, RefRO<HealthComponent>, RefRW<LocalTransform>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // 1. OBLICZANIE PROCENTU HP
            float maxHp = 100f; // Za³o¿enie: 100 to HP startowe
            float healthPercent = (float)health.ValueRO.HealthPoints / maxHp;

            // 2. LOGIKA NISZCZENIA (Przy 50% HP lub mniej)
            if (healthPercent <= 0.5f)
            {
                if (isServer)
                {
                    ecb.DestroyEntity(entity);
                }
                else
                {
                    // Na kliencie dodajemy Disabled, ¿eby natychmiast ukryæ obiekt
                    // Zostanie on usuniêty z pamiêci, gdy serwer przeœle Ghost Destroy
                    ecb.AddComponent<Disabled>(entity);
                }
                continue;
            }

            // 3. AKTUALIZACJA WIZUALNA (Skalowanie)
            // Skala maleje liniowo wraz z HP (np. przy 75% HP skala wynosi 0.75)
            float currentScale = box.ValueRO.InitialScale * healthPercent;
            transform.ValueRW.Scale = currentScale;

            // 4. KOREKTA POZYCJI (Aby spód dotyka³ ziemi)
            // Zak³adamy, ¿e Pivot modelu jest w jego œrodku.
            // Obliczamy o ile góra i dó³ siê skurczy³y i obni¿amy obiekt o tê wartoœæ.
            float heightReduction = (box.ValueRO.InitialScale - currentScale) * 0.5f;
            transform.ValueRW.Position.y = box.ValueRO.InitialY - heightReduction;
        }
    }
}*/