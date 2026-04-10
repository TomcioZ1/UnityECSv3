using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

//[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct DropWeaponSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (dropWeapon, health, transform, entity) in
                 SystemAPI.Query<RefRO<DropWeapon>, RefRO<HealthComponent>, RefRO<LocalTransform>>()
                 .WithEntityAccess())
        {
            if (health.ValueRO.HealthPoints <= 0)
            {
                Entity prefab = dropWeapon.ValueRO.DropWeaponPrefab;
                if (prefab == Entity.Null) continue;

                // 1. Spawnowanie
                Entity droppedWeapon = ecb.Instantiate(prefab);

                //5.9f

                // 2. Pozycja - dodaj mały offset w górę (0.5f), żeby broń nie utknęła w podłodze
                float3 spawnPos = transform.ValueRO.Position; // Lekko nad ziemią
                spawnPos.y = 5.9f;

                ecb.SetComponent(droppedWeapon, LocalTransform.FromPositionRotationScale(
                    spawnPos,
                    quaternion.identity,
                    1.0f));

                // 3. KLUCZ DLA NETCODE - Odkomentuj to!
                ecb.AddComponent(droppedWeapon, new GhostOwner { NetworkId = -1 });

                // 4. KLUCZ DLA TWOJEGO SYSTEMU PICKUP
                // Upewniamy się, że nowa broń ma zainicjowany stan
                ecb.SetComponent(droppedWeapon, new GhostState { IsDestroyed = false });

                // 5. WYMUSZENIE AKTUALIZACJI FIZYKI
                // Jeśli prefab ma PhysicsVelocity, warto go zresetować/nadać, 
                // co zmusi system do przeliczenia kolizji w nowym miejscu.
               

                ecb.RemoveComponent<DropWeapon>(entity);
            }
        }
    }
}