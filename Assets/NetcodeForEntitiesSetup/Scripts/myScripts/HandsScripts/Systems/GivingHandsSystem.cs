using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct GivingHandsSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. Sprawdzamy singletony na pocz¹tku - bardzo tanie w Burst
        if (!SystemAPI.HasSingleton<HandsResources>()) return;

        // 2. Pobieramy ECB
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var resources = SystemAPI.GetSingleton<HandsResources>();

        // 3. Lookupy pozwalaj¹ nam bezpiecznie pobieraæ dane wewn¹trz pêtli 
        // bez koniecznoœci robienia Query dla ka¿dej drobnostki
        var ghostOwnerLookup = state.GetComponentLookup<GhostOwner>(true);

        // OPTYMALIZACJA: Zmieniamy RefRW na RefRO. 
        // Dziêki temu nie oznaczamy ActiveHands jako "Dirty" w ka¿dej klatce (brak lagów sieciowych).
        foreach (var (activeHands, playerEntity) in
                 SystemAPI.Query<RefRO<ActiveHands>>()
                 .WithEntityAccess())
        {
            // Sprawdzamy warunek na danych tylko do odczytu
            if (activeHands.ValueRO.LeftHandEntity == Entity.Null)
            {
                // 4. Instantiate prefabów
                Entity leftHandSpawned = ecb.Instantiate(resources.LeftHand);
                Entity rightHandSpawned = ecb.Instantiate(resources.RightHand);

                // 5. PRZYPISANIE - U¿ywamy ECB do aktualizacji komponentu. 
                // To sprawi, ¿e zmiana zostanie wys³ana sieci¹ tylko RAZ (w momencie przypisania).
                ecb.SetComponent(playerEntity, new ActiveHands
                {
                    LeftHandEntity = leftHandSpawned,
                    PrevLeftHand = Entity.Null, // Klient u¿yje tego do detekcji zmiany
                    RightHandEntity = rightHandSpawned,
                    PrevRightHand = Entity.Null
                });

                // 6. Ustawienie w³aœciciela Ghosta (NetworkId)
                if (ghostOwnerLookup.HasComponent(playerEntity))
                {
                    var playerOwner = ghostOwnerLookup[playerEntity];
                    var ownerData = new GhostOwner { NetworkId = playerOwner.NetworkId };

                    ecb.SetComponent(leftHandSpawned, ownerData);
                    ecb.SetComponent(rightHandSpawned, ownerData);
                    ecb.SetComponent(leftHandSpawned, new HandsOwner { Entity = playerEntity });
                    ecb.SetComponent(rightHandSpawned, new HandsOwner { Entity = playerEntity });
                }

                ecb.AppendToBuffer(playerEntity, new LinkedEntityGroup { Value = leftHandSpawned });
                ecb.AppendToBuffer(playerEntity, new LinkedEntityGroup { Value = rightHandSpawned });

                // 7. Dodanie pomocniczych komponentów
                var weaponOwner = new WeaponOwner { Entity = playerEntity };
                ecb.AddComponent(leftHandSpawned, weaponOwner);
                ecb.AddComponent(rightHandSpawned, weaponOwner);
            }
        }
    }
}