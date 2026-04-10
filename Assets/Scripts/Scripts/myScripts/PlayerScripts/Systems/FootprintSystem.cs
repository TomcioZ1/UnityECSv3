using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using MaterialPropertyBaseColor = Unity.Rendering.URPMaterialPropertyBaseColor;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile]
public partial struct FootprintSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<FootprintSpawner>(out var spawnerSettings))
            return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (playerTransform, footprintState) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerFootprintState>>()
                 .WithAll<PlayerTag, Simulate>())
        {
            float3 playerPos = playerTransform.ValueRO.Position;

            if (!footprintState.ValueRO.IsInitialized)
            {
                footprintState.ValueRW.LastSpawnPosition = playerPos;
                footprintState.ValueRW.IsInitialized = true;
                continue;
            }

            // U¿ywamy Twojej zmiennej distanceBetweenSteps
            float dist = math.distance(playerPos, footprintState.ValueRO.LastSpawnPosition);

            if (dist > footprintState.ValueRO.distanceBetweenSteps)
            {
                Entity footprint = ecb.Instantiate(spawnerSettings.FootprintPrefab);

                float3 rightDir = math.mul(playerTransform.ValueRO.Rotation, new float3(1, 0, 0));

                // U¿ywamy Twojej zmiennej distanceBetweenLegs do offsetu na boki
                float sideOffset = footprintState.ValueRO.LeftFoot ?
                    -footprintState.ValueRO.distanceBetweenLegs :
                    footprintState.ValueRO.distanceBetweenLegs;

                float3 spawnPos = playerPos + (rightDir * sideOffset);
                spawnPos.y = playerPos.y - 0.25f;

                // Ustawiamy Transform
                ecb.SetComponent(footprint, LocalTransform.FromPositionRotation(spawnPos, playerTransform.ValueRO.Rotation));

                // Ustawiamy czas ¿ycia (u¿ywamy pola MaxValue do obliczeñ zanikania)
                // Zak³adamy, ¿e czas ¿ycia bierzesz np. z distanceBetweenSteps * 10 lub sta³ej, 
                // jeœli nie masz go w struct, wpisa³em 5.0f jako bazê.


                // Inicjalizujemy kolor (wymagane do zanikania)
                ecb.AddComponent(footprint, new MaterialPropertyBaseColor { Value = new float4(0, 0, 0, 1) });

                // Aktualizacja stanu gracza
                footprintState.ValueRW.LastSpawnPosition = playerPos;
                footprintState.ValueRW.LeftFoot = !footprintState.ValueRO.LeftFoot;
            }
        }
    }
}