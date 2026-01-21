/*using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine; // Potrzebne do Debug.Log

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
// [BurstCompile] // Zakomentuj to, aby widzieæ logi w konsoli!
public partial struct PlayerPunchSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.IsFirstTimeFullyPredictingTick) return;

        float deltaTime = SystemAPI.Time.DeltaTime;
        bool isServer = state.WorldUnmanaged.IsServer();

        // Przygotowanie danych do kolizji
        var healthQuery = SystemAPI.QueryBuilder().WithAll<HealthComponent, LocalTransform>().Build();
        var healthEntities = healthQuery.ToEntityArray(Allocator.Temp);
        var healthTransforms = healthQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        foreach (var (hands, punchingHand, input, playerTransform, playerEntity) in
                 SystemAPI.Query<RefRW<HandsComponent>, RefRO<PunchingHand>, RefRO<PlayerShootInput>, RefRO<LocalTransform>>()
                 .WithAll<Simulate>()
                 .WithEntityAccess())
        {
            // --- 1. WYKRYWANIE KLIKNIÊCIA ---
            if (hands.ValueRO.State == HandState.Idle && input.ValueRO.ShootPrimary > 0)
            {
                hands.ValueRW.State = HandState.LeftPunch;
                hands.ValueRW.AnimationTimer = 0.6f; // Nieco d³u¿szy czas dla widocznoœci
                Debug.Log($"[PUNCH] Start ciosu! State: {hands.ValueRO.State}, Entity: {playerEntity}");
            }

            // --- 2. PROCES ANIMACJI ---
            if (hands.ValueRO.State != HandState.Idle)
            {
                float totalDuration = 0.6f;
                float previousTimer = hands.ValueRO.AnimationTimer;
                hands.ValueRW.AnimationTimer -= deltaTime;

                // Obliczanie postêpu (0 do 1)
                float progress = math.clamp((totalDuration - hands.ValueRO.AnimationTimer) / totalDuration, 0f, 1f);

                // Sinusoida dla p³ynnego ruchu tam i z powrotem
                float punchEffect = math.sin(progress * math.PI);

                // PRZESADZONY ZASIÊG dla testu: u¿ywamy Reach * 2, ¿eby by³o widaæ
                float visibleReach = punchingHand.ValueRO.Reach * 2.0f;
                float3 extension = new float3(0, 0, punchEffect * visibleReach);

                // Wybór rêki
                Entity handEntity = (hands.ValueRO.State == HandState.LeftPunch) ? hands.ValueRO.LeftHand : hands.ValueRO.RightHand;

                if (state.EntityManager.HasComponent<LocalTransform>(handEntity))
                {
                    var handTransform = SystemAPI.GetComponentRW<LocalTransform>(handEntity);

                    // Ustawiamy pozycjê: 
                    // X: -0.5 (lewo), Y: 1.0 (wysokoœæ klatki), Z: extension (wysuniêcie)
                    float sideOffset = (hands.ValueRO.State == HandState.LeftPunch) ? -0.5f : 0.5f;
                    handTransform.ValueRW.Position = new float3(sideOffset, 1.0f, extension.z);
                }

                // --- 3. LOGIKA TRAFIENIA (SERWER) ---
                // Sprawdzamy w szczytowym momencie (progress blisko 0.5)
                if (isServer && previousTimer > 0.3f && hands.ValueRO.AnimationTimer <= 0.3f)
                {
                    Debug.Log("[PUNCH] Serwer sprawdza kolizjê piêœci...");

                    // Pozycja piêœci w œwiecie
                    float3 worldPunchPos = math.transform(playerTransform.ValueRO.ToMatrix(), extension + new float3(0, 1.0f, 0));

                    for (int i = 0; i < healthEntities.Length; i++)
                    {
                        if (healthEntities[i] == playerEntity) continue;

                        float dist = math.distance(worldPunchPos, healthTransforms[i].Position);
                        if (dist < 1.5f) // Doœæ du¿y margines b³êdu dla testów
                        {
                            var targetHealth = SystemAPI.GetComponentRW<HealthComponent>(healthEntities[i]);
                            targetHealth.ValueRW.HealthPoints -= (int)punchingHand.ValueRO.Damage;

                            Debug.Log($"[HIT] Trafiono encjê {healthEntities[i]}! Pozosta³o HP: {targetHealth.ValueRO.HealthPoints}");
                        }
                    }
                }

                // --- 4. KONIEC CIOSU ---
                if (hands.ValueRO.AnimationTimer <= 0)
                {
                    Debug.Log("[PUNCH] Koniec animacji, powrót do Idle.");
                    hands.ValueRW.State = HandState.Idle;
                    hands.ValueRW.AnimationTimer = 0;

                    // Reset pozycji do klatki piersiowej
                    if (state.EntityManager.HasComponent<LocalTransform>(handEntity))
                    {
                        var lt = SystemAPI.GetComponentRW<LocalTransform>(handEntity);
                        float sideOffset = (hands.ValueRO.State == HandState.LeftPunch) ? -0.5f : 0.5f;
                        lt.ValueRW.Position = new float3(sideOffset, 1.0f, 0);
                    }
                }
            }
        }
    }
}*/