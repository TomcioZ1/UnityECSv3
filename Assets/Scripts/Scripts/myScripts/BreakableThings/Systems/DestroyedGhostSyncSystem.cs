using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;

// System powinien dzia³aæ na kliencie w grupie symulacji predykcyjnej lub po prostu w Simulation
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct DestroyedGhostSyncSystem : ISystem
{
    private ComponentLookup<PhysicsCollider> physicsColliderLookup;
    private BufferLookup<LinkedEntityGroup> linkedEntityLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        physicsColliderLookup = state.GetComponentLookup<PhysicsCollider>(true);
        linkedEntityLookup = state.GetBufferLookup<LinkedEntityGroup>(true);

        // Zapewniamy, ¿e system uruchomi siê tylko gdy s¹ jakieœ Ghosty
        state.RequireForUpdate<NetworkId>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        physicsColliderLookup.Update(ref state);
        linkedEntityLookup.Update(ref state);

        // Szukamy encji, które s¹ Ghostami i maj¹ ustawione IsDestroyed na true,
        // ale jeszcze nie maj¹ dodanego DisableRendering (co oznacza, ¿e nie zosta³y przetworzone)
        // Wykluczamy encje, które ju¿ maj¹ DisableRendering, aby nie spamowaæ ECB.
        new SyncDestroyedGhostsJob
        {
            ECB = ecb,
            LinkedEntityLookup = linkedEntityLookup
        }.Run(); // U¿ywamy Run lub Schedule w zale¿noœci od potrzeb wydajnoœciowych
    }

    [BurstCompile]
    [WithAll(typeof(GhostState))]
    [WithNone(typeof(DisableRendering))] // Kluczowe: przetwarzamy tylko te, które jeszcze "widniej¹"
    partial struct SyncDestroyedGhostsJob : IJobEntity
    {
        public EntityCommandBuffer ECB;
        [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedEntityLookup;

        public void Execute(Entity entity, in GhostState ghostState)
        {
            if (ghostState.IsDestroyed)
            {
                // Jeœli encja ma dzieci (np. osobny Mesh pod rootem), musimy wy³¹czyæ wszystko
                if (LinkedEntityLookup.HasBuffer(entity))
                {
                    var children = LinkedEntityLookup[entity];
                    for (int i = 0; i < children.Length; i++)
                    {
                        DisableGhostVisuals(children[i].Value);
                    }
                }
                else
                {
                    DisableGhostVisuals(entity);
                }
            }
        }

        private void DisableGhostVisuals(Entity e)
        {
            // Dodajemy tagi/komponenty wy³¹czaj¹ce
            ECB.AddComponent<DisableRendering>(e);

            // Usuwamy fizykê, aby postaæ nie blokowa³a siê na "niewidzialnym" itemie
            ECB.RemoveComponent<PhysicsCollider>(e);
        }
    }
}