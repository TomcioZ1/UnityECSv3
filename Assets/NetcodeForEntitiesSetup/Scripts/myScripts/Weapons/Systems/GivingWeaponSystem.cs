using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Multiplayer.Center.NetcodeForEntitiesSetup;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct WeaponControlSystem : ISystem
{
    private struct WeaponChangeRequest
    {
        public Entity PlayerEntity;
        public Entity OldWeaponEntity;
        public Entity WeaponSocket;
        public byte TargetWeaponId;
        public int NetworkId;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<WeaponResources>(out var resources)) return;

        var changes = new NativeList<WeaponChangeRequest>(Allocator.Temp);

        foreach (var (activeWeapon, input, socket, playerEntity) in
                 SystemAPI.Query<RefRO<ActiveWeapon>, RefRO<MyPlayerInput>, RefRO<WeaponSocket>>()
                 .WithEntityAccess())
        {
            byte currentId = activeWeapon.ValueRO.SelectedWeaponId;
            byte inputId = input.ValueRO.choosenWeapon;
            byte targetId = currentId;

            if (currentId == 0 && inputId == 0) targetId = 1;
            else if (inputId != 0 && inputId != currentId) targetId = inputId;

            if (targetId != currentId)
            {
                int netId = 0;
                if (SystemAPI.HasComponent<GhostOwner>(playerEntity))
                {
                    netId = SystemAPI.GetComponent<GhostOwner>(playerEntity).NetworkId;
                }

                changes.Add(new WeaponChangeRequest
                {
                    PlayerEntity = playerEntity,
                    OldWeaponEntity = activeWeapon.ValueRO.WeaponEntity,
                    WeaponSocket = socket.ValueRO.WeaponSocketEntity,
                    TargetWeaponId = targetId,
                    NetworkId = netId
                });
            }
        }

        if (changes.Length > 0)
        {
            var entityManager = state.EntityManager;

            foreach (var req in changes)
            {
                // A. Usuwanie starej broni
                if (req.OldWeaponEntity != Entity.Null && entityManager.Exists(req.OldWeaponEntity))
                {
                    entityManager.DestroyEntity(req.OldWeaponEntity);
                }

                // B. Wybór prefaba
                Entity prefabToSpawn = req.TargetWeaponId switch
                {
                    1 => resources.Pistol,
                    2 => resources.Shotgun,
                    3 => resources.ak47,
                    4 => resources.Sniper,
                    _ => Entity.Null
                };

                Entity newWeaponEntity = Entity.Null;

                // C. Stwórz now¹ broñ
                if (prefabToSpawn != Entity.Null)
                {
                    newWeaponEntity = entityManager.Instantiate(prefabToSpawn);

                    // 1. Pobierz domyœln¹ rotacjê i skalê z prefaba
                    // To sprawi, ¿e jeœli broñ w prefabie jest obrócona o 90 stopni, to tak zostanie.
                    LocalTransform prefabTransform = entityManager.GetComponentData<LocalTransform>(prefabToSpawn);

                    // GhostOwner
                    entityManager.SetComponentData(newWeaponEntity, new GhostOwner { NetworkId = req.NetworkId });

                    // WeaponOwner
                    entityManager.AddComponentData(newWeaponEntity, new WeaponOwner { Entity = req.PlayerEntity });

                    // --- TRANSFORM I HIERARCHIA ---
                    if (!entityManager.HasComponent<Parent>(newWeaponEntity))
                    {
                        entityManager.AddComponentData(newWeaponEntity, new Parent { Value = req.WeaponSocket });
                    }
                    else
                    {
                        entityManager.SetComponentData(newWeaponEntity, new Parent { Value = req.WeaponSocket });
                    }

                    // 2. Ustawiamy pozycjê na zero, ALE zachowujemy rotacjê i skalê z prefaba
                    entityManager.SetComponentData(newWeaponEntity, LocalTransform.FromPositionRotationScale(
                        float3.zero,
                        prefabTransform.Rotation,
                        prefabTransform.Scale
                    ));
                }

                // D. Aktualizacja ActiveWeapon
                var activeWeapon = entityManager.GetComponentData<ActiveWeapon>(req.PlayerEntity);
                activeWeapon.WeaponEntity = newWeaponEntity;
                activeWeapon.SelectedWeaponId = req.TargetWeaponId;
                entityManager.SetComponentData(req.PlayerEntity, activeWeapon);
            }
        }

        changes.Dispose();
    }
}